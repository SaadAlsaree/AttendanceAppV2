import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';
import { pgClient, scalar } from '../fixtures/db';
import type { Client } from 'pg';
import { randomUUID } from 'node:crypto';

/**
 * FEATURE 02: Directorate report (تقرير الجهات) — each directorate's report must
 * surface the REAL figures, broken out:
 *   • totalEmployees      — real headcount of the directorate (إجمالي الموظفين)
 *   • totalAttendances    — present / clocked-in (حاضرين)
 *   • totalNotAttendances — non-fingerprinted: scheduled but didn't clock in,
 *                           excluding those on leave (غير مبصمين)
 *   • totalLeaves         — per-unit leaves on the date
 *
 * GET /reports/organization scopes the report to the LOGGED-IN user's own org
 * unit, so this spec: (1) picks a real directorate + date with data and computes
 * ground truth straight from Postgres (mirroring the handler's queries + EF
 * soft-delete filters), (2) temporarily points seed.admin at that directorate,
 * (3) calls the endpoint via Burp, (4) asserts the returned per-unit AND
 * org-level figures equal the DB truth, plus regression guards for the two bugs
 * this feature fixed (whole-DB headcount; constant-zero non-fingerprinted), and
 * (5) restores seed.admin in afterAll (runs even on failure). Cross-checks the
 * canonical /reports/organizational-summary as an independent second source.
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;
const LOGIN = process.env.E2E_ADMIN_LOGIN || 'seed.admin';
const PASSWORD = process.env.E2E_ADMIN_PASSWORD || 'Admin@123456';

type Truth = {
  unitId: string;
  unitName: string;
  date: string; // YYYY-MM-DD
  employees: number;
  present: number;
  notAttendances: number; // non-fingerprinted (scheduled, not clocked, not on leave)
  leaves: number;
  searchTerm: string;
  nonFingerprintedEmployeeId: string;
};

test.describe.serial('Feature 02 — directorate report figures (API, via Burp)', () => {
  let db: Client;
  let ctx: APIRequestContext;
  let token = '';
  let origUnit: string | null = null;
  let truth: Truth;
  let dbTotalEmployees = 0;
  let fixtureLeaveId: string | null = null;

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();

    // 1) Pick the (unit, date) with the most non-fingerprinted rows, requiring
    //    some present employees too, restricted to report-eligible levels (1-3).
    const pick = await db.query(`
      SELECT e.organizational_unit_id AS unit_id,
             to_char(a.date::date, 'YYYY-MM-DD') AS d
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id = a.employee_id AND e.is_deleted = false
      JOIN public."OrganizationalUnits" ou ON ou.id = e.organizational_unit_id
      WHERE a.is_deleted = false AND ou.is_deleted = false
            AND ou.unit_level BETWEEN 1 AND 3
      GROUP BY e.organizational_unit_id, a.date::date
      HAVING COUNT(*) FILTER (WHERE a.check_in_time IS NOT NULL OR a.check_out_time IS NOT NULL) > 0
         AND COUNT(*) FILTER (WHERE a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL) > 0
      ORDER BY COUNT(*) FILTER (WHERE a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL) DESC
      LIMIT 1;`);
    expect(pick.rows.length, 'a directorate+date with data must exist').toBeGreaterThan(0);
    const unitId: string = pick.rows[0].unit_id;
    const date: string = pick.rows[0].d;

    // 2) Compute ground truth (mirrors GetOrganizationReportHandler queries).
    const unitName = (await scalar<string>(
      db, `SELECT unit_name FROM public."OrganizationalUnits" WHERE id=$1`, [unitId]
    )) as string;
    const employees = Number(await scalar(
      db, `SELECT COUNT(*) FROM public."Employees" WHERE organizational_unit_id=$1 AND is_deleted=false`, [unitId]
    ));
    const present = Number(await scalar(db, `
      SELECT COUNT(*) FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE e.organizational_unit_id=$1 AND a.is_deleted=false AND a.date::date=$2
            AND (a.check_in_time IS NOT NULL OR a.check_out_time IS NOT NULL)`, [unitId, date]));
    const leaves = Number(await scalar(db, `
      SELECT COUNT(*) FROM public."Leaves" l
      JOIN public."Employees" e ON e.id=l.employee_id AND e.is_deleted=false
      WHERE e.organizational_unit_id=$1 AND l.is_deleted=false
            AND l.status='Approved'
            AND l.start_date::date<=$2 AND l.end_date::date>=$2`, [unitId, date]));
    const notAttendances = Number(await scalar(db, `
      SELECT COUNT(*) FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE e.organizational_unit_id=$1 AND a.is_deleted=false AND a.date::date=$2
            AND a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL
            AND a.employee_id NOT IN (
              SELECT l.employee_id FROM public."Leaves" l
              JOIN public."Employees" e2 ON e2.id=l.employee_id AND e2.is_deleted=false
              WHERE e2.organizational_unit_id=$1 AND l.is_deleted=false
                    AND l.status='Approved'
                    AND l.start_date::date<=$2 AND l.end_date::date>=$2)`, [unitId, date]));

    const searchTerm = (await scalar<string>(db, `
      SELECT full_name FROM public."Employees"
      WHERE organizational_unit_id=$1 AND is_deleted=false
      ORDER BY full_name
      LIMIT 1`, [unitId])) as string;
    const nonFingerprintedEmployeeId = (await scalar<string>(db, `
      SELECT a.employee_id
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE e.organizational_unit_id=$1 AND a.is_deleted=false AND a.date::date=$2
            AND a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL
            AND NOT EXISTS (
              SELECT 1 FROM public."Leaves" l
              WHERE l.employee_id=a.employee_id AND l.is_deleted=false
                    AND l.status='Approved'
                    AND l.start_date::date<=$2 AND l.end_date::date>=$2)
      LIMIT 1`, [unitId, date])) as string;

    truth = {
      unitId,
      unitName,
      date,
      employees,
      present,
      notAttendances,
      leaves,
      searchTerm,
      nonFingerprintedEmployeeId
    };
    dbTotalEmployees = Number(await scalar(
      db, `SELECT COUNT(*) FROM public."Employees" WHERE is_deleted=false`
    ));
    // eslint-disable-next-line no-console
    console.log(`[feature-02] target=${unitName} date=${date} ` +
      `expected employees=${employees} present=${present} non-fingerprinted=${notAttendances} leaves=${leaves}`);

    // 3) Setup: point seed.admin at the target directorate (saved for teardown).
    origUnit = await scalar<string>(
      db, `SELECT organizational_unit_id FROM public."Users" WHERE user_login=$1`, [LOGIN]
    );
    await db.query(
      `UPDATE public."Users" SET organizational_unit_id=$1 WHERE user_login=$2`, [unitId, LOGIN]
    );

    // 4) API context (Burp-routed) + login (org unit is read from DB, not the JWT).
    ctx = await pwRequest.newContext({
      baseURL: API,
      ignoreHTTPSErrors: true,
      ...(BURP ? { proxy: { server: BURP } } : {})
    });
    const login = await ctx.post('/auth/login', { data: { userLogin: LOGIN, password: PASSWORD } });
    expect(login.ok(), 'login should succeed').toBeTruthy();
    token = (await login.json()).data.token;
  });

  test.afterAll(async () => {
    // Teardown: always restore seed.admin's original org unit.
    if (db) {
      if (fixtureLeaveId) {
        await db.query(`DELETE FROM public."Leaves" WHERE id=$1`, [fixtureLeaveId]).catch(() => {});
      }
      await db.query(
        `UPDATE public."Users" SET organizational_unit_id=$1 WHERE user_login=$2`, [origUnit, LOGIN]
      ).catch(() => {});
      await db.end().catch(() => {});
    }
    await ctx?.dispose();
  });

  test('GET /reports/organization returns correct per-directorate figures', async () => {
    const res = await ctx.get(
      `/reports/organization?OrganizationalUnitId=${truth.unitId}` +
      `&Date=${truth.date}&IncludeSubUnits=false&PageNumber=1&PageSize=10`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    expect(res.status(), 'report should return 200').toBe(200);
    const data = (await res.json()).data;
    const unit = (data.units || []).find((u: { unitId: string }) => u.unitId === truth.unitId);
    expect(unit, 'target directorate must appear in units[]').toBeTruthy();

    // Per-directorate figures — the three the feature requires + leaves.
    expect(unit.totalEmployees, 'real headcount (إجمالي الموظفين)').toBe(truth.employees);
    expect(unit.totalAttendances, 'present (حاضرين)').toBe(truth.present);
    expect(unit.totalNotAttendances, 'non-fingerprinted (غير مبصمين)').toBe(truth.notAttendances);
    expect(unit.totalLeaves, 'per-unit leaves').toBe(truth.leaves);

    // With IncludeSubUnits=false the org-level totals equal the single unit's.
    expect(data.totalEmployees).toBe(truth.employees);
    expect(data.totalAttendances).toBe(truth.present);
    expect(data.totalNotAttendances).toBe(truth.notAttendances);

    // Regression guards for the bugs this feature fixed:
    expect(unit.totalEmployees, 'headcount must be unit-scoped, not whole-DB')
      .not.toBe(dbTotalEmployees);
    expect(unit.totalNotAttendances, 'non-fingerprinted must not be a constant 0')
      .toBeGreaterThan(0);
  });

  test('cross-check: /reports/organizational-summary agrees on the same directorate', async () => {
    const res = await ctx.get(
      `/reports/organizational-summary?OrganizationalUnitId=${truth.unitId}` +
      `&Date=${truth.date}&IncludeSubUnits=false`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    expect(res.status(), 'summary should return 200').toBe(200);
    const data = (await res.json()).data;
    const unit = (data.units || []).find((u: { unitId: string }) => u.unitId === truth.unitId);
    expect(unit, 'target directorate must appear in summary units[]').toBeTruthy();

    // Canonical handler uses the "tottal" (sic) field spelling — see types.
    expect(unit.totalEmployees).toBe(truth.employees);
    expect(unit.tottalAttendances).toBe(truth.present);
    expect(unit.tottalNotAttendances).toBe(truth.notAttendances);
  });

  test('search does not change the real directorate headcount', async () => {
    const res = await ctx.get(
      `/reports/organization?OrganizationalUnitId=${truth.unitId}` +
      `&Date=${truth.date}&IncludeSubUnits=false&PageNumber=1&PageSize=10` +
      `&SearchTerm=${encodeURIComponent(truth.searchTerm)}`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    expect(res.status()).toBe(200);
    const data = (await res.json()).data;
    const unit = (data.units || []).find((u: { unitId: string }) => u.unitId === truth.unitId);

    expect(data.totalEmployees, 'organization headcount must ignore detail search').toBe(truth.employees);
    expect(unit.totalEmployees, 'directorate headcount must ignore detail search').toBe(truth.employees);
  });

  test('only approved leave excludes an employee from non-fingerprinted', async () => {
    fixtureLeaveId = randomUUID();
    await db.query(`
      INSERT INTO public."Leaves"
        (id, employee_id, leave_type, start_date, end_date, reason, status, created_at, is_deleted)
      VALUES ($1, $2, 'Ordinary', $3::date, $3::date, 'E2E leave-status regression', 'Pending', NOW(), false)`,
      [fixtureLeaveId, truth.nonFingerprintedEmployeeId, truth.date]
    );

    const getReport = async () => {
      const res = await ctx.get(
        `/reports/organization?OrganizationalUnitId=${truth.unitId}` +
        `&Date=${truth.date}&IncludeSubUnits=false&PageNumber=1&PageSize=10`,
        { headers: { Authorization: `Bearer ${token}` } }
      );
      expect(res.status()).toBe(200);
      const data = (await res.json()).data;
      return (data.units || []).find((u: { unitId: string }) => u.unitId === truth.unitId);
    };

    for (const status of ['Pending', 'Rejected', 'Cancelled']) {
      await db.query(`UPDATE public."Leaves" SET status=$1 WHERE id=$2`, [status, fixtureLeaveId]);
      const unit = await getReport();
      expect(unit.totalNotAttendances, `${status} leave must not exempt the employee`)
        .toBe(truth.notAttendances);
    }

    await db.query(`UPDATE public."Leaves" SET status='Approved' WHERE id=$1`, [fixtureLeaveId]);
    const unit = await getReport();
    expect(unit.totalNotAttendances, 'approved leave must exempt the employee')
      .toBe(truth.notAttendances - 1);
    expect(unit.totalLeaves, 'approved leave must be included in leave totals')
      .toBe(truth.leaves + 1);
  });
});
