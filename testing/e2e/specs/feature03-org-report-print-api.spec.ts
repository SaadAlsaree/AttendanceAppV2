import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';
import { pgClient, scalar } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 03 (التقارير) — part A: the organizational report's PRINT must list the
 * names of the non-fingerprinted (غير مبصمين) and of the action/status employees
 * (الاجراءات), not just the present (حاضرين). The print renders server-fetched
 * data, so the new fields live on GET /reports/organization:
 *   • unit.nonFingerprintedEmployees — full, un-paginated [{employeeId, employeeName}]
 *   • unit.actionEmployees           — [{employeeId, employeeName, actionName}]
 *
 * This spec points seed.admin at a real directorate (restored in afterAll) and
 * asserts the non-fingerprinted NAME list is complete (length === the count, which
 * pagination must not shrink) and matches the DB, and that actionEmployees is a
 * well-formed array carrying Arabic action labels.
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
  notAttendances: number;
  nonFingerprintedNames: string[];
};

test.describe.serial('Feature 03 — org report print lists (API, via Burp)', () => {
  let db: Client;
  let ctx: APIRequestContext;
  let token = '';
  let origUnit: string | null = null;
  let truth: Truth;

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();

    // 1) Pick the (unit, date) with the most non-fingerprinted rows (report levels 1-3).
    const pick = await db.query(`
      SELECT e.organizational_unit_id AS unit_id,
             to_char(a.date::date, 'YYYY-MM-DD') AS d
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id = a.employee_id AND e.is_deleted = false
      JOIN public."OrganizationalUnits" ou ON ou.id = e.organizational_unit_id
      WHERE a.is_deleted = false AND ou.is_deleted = false
            AND ou.unit_level BETWEEN 1 AND 3
      GROUP BY e.organizational_unit_id, a.date::date
      HAVING COUNT(*) FILTER (WHERE a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL) > 0
      ORDER BY COUNT(*) FILTER (WHERE a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL) DESC
      LIMIT 1;`);
    expect(pick.rows.length, 'a unit+date with non-fingerprinted data must exist').toBeGreaterThan(0);
    const unitId: string = pick.rows[0].unit_id;
    const date: string = pick.rows[0].d;

    const unitName = (await scalar<string>(
      db, `SELECT unit_name FROM public."OrganizationalUnits" WHERE id=$1`, [unitId]
    )) as string;

    // Ground-truth non-fingerprinted names (scheduled, no punch, not on approved leave).
    const names = await db.query(
      `
      SELECT e.full_name AS name
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE e.organizational_unit_id=$1 AND a.is_deleted=false AND a.date::date=$2
            AND a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL
            AND NOT EXISTS (
              SELECT 1 FROM public."Leaves" l
              WHERE l.employee_id=a.employee_id AND l.is_deleted=false
                    AND l.status='Approved'
                    AND l.start_date::date<=$2 AND l.end_date::date>=$2)
      ORDER BY e.full_name`,
      [unitId, date]
    );
    const nonFingerprintedNames = names.rows.map((x) => x.name as string);

    truth = {
      unitId,
      unitName,
      date,
      notAttendances: nonFingerprintedNames.length,
      nonFingerprintedNames
    };
    // eslint-disable-next-line no-console
    console.log(`[feature-03A] unit=${unitName} date=${date} non-fingerprinted=${truth.notAttendances}`);

    // Point seed.admin at the target directorate (org unit is read from DB, not the JWT).
    origUnit = await scalar<string>(
      db, `SELECT organizational_unit_id FROM public."Users" WHERE user_login=$1`, [LOGIN]
    );
    await db.query(
      `UPDATE public."Users" SET organizational_unit_id=$1 WHERE user_login=$2`, [unitId, LOGIN]
    );

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
    if (db) {
      await db.query(
        `UPDATE public."Users" SET organizational_unit_id=$1 WHERE user_login=$2`, [origUnit, LOGIN]
      ).catch(() => {});
      await db.end().catch(() => {});
    }
    await ctx?.dispose();
  });

  // A small PageSize proves the name lists are NOT clipped by the detail pagination.
  const fetchUnit = async (pageSize: number) => {
    const res = await ctx.get(
      `/reports/organization?OrganizationalUnitId=${truth.unitId}` +
      `&Date=${truth.date}&IncludeSubUnits=false&PageNumber=1&PageSize=${pageSize}`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    expect(res.status(), 'report should return 200').toBe(200);
    const data = (await res.json()).data;
    const unit = (data.units || []).find((u: { unitId: string }) => u.unitId === truth.unitId);
    expect(unit, 'target directorate must appear in units[]').toBeTruthy();
    return unit;
  };

  test('non-fingerprinted NAME list is complete and matches the DB', async () => {
    const unit = await fetchUnit(5);

    expect(Array.isArray(unit.nonFingerprintedEmployees)).toBeTruthy();
    // Names list length must equal the count badge...
    expect(unit.nonFingerprintedEmployees.length, 'names length == count')
      .toBe(unit.totalNotAttendances);
    // ...and equal the DB ground truth (proves it ignores the small PageSize).
    expect(unit.nonFingerprintedEmployees.length, 'names length == DB truth')
      .toBe(truth.notAttendances);

    const got = unit.nonFingerprintedEmployees
      .map((e: { employeeName: string }) => e.employeeName)
      .sort();
    expect(got).toEqual([...truth.nonFingerprintedNames].sort());
  });

  test('non-fingerprinted list is independent of detail pagination', async () => {
    const small = await fetchUnit(1);
    const large = await fetchUnit(500);
    expect(small.nonFingerprintedEmployees.length).toBe(truth.notAttendances);
    expect(large.nonFingerprintedEmployees.length).toBe(truth.notAttendances);
    // The paginated present-detail list, by contrast, IS bounded by PageSize.
    expect(small.employeeDetails.length).toBeLessThanOrEqual(large.employeeDetails.length);
  });

  test('actionEmployees is a well-formed list (name + Arabic action label)', async () => {
    const unit = await fetchUnit(10);
    expect(Array.isArray(unit.actionEmployees), 'actionEmployees must be an array').toBeTruthy();
    for (const a of unit.actionEmployees) {
      expect(typeof a.employeeName).toBe('string');
      expect(typeof a.actionName).toBe('string');
      expect(a.actionName.length, 'action must carry a label').toBeGreaterThan(0);
    }
    // Actions must NOT just mirror the non-fingerprinted bucket (Pending is excluded).
    const nfIds = new Set(
      unit.nonFingerprintedEmployees.map((e: { employeeId: string }) => e.employeeId)
    );
    for (const a of unit.actionEmployees) {
      expect(nfIds.has(a.employeeId), 'an action employee must not be a non-fingerprinted one').toBeFalsy();
    }
  });
});
