import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';
import { pgClient, scalar } from '../fixtures/db';
import type { Client } from 'pg';
import { randomUUID } from 'node:crypto';

/**
 * NOT-ATTENDANCE ALIGNMENT: GET /attendance/not-attendance must use the SAME core
 * "غير مبصم" definition as the daily organizational report's GetNonFingerprintedAsync:
 *
 *   1. SHIFT REQUIRED   — only employees scheduled that day (shift_id IS NOT NULL)
 *                         count; a no-punch row WITHOUT a shift must NOT appear.
 *   2. APPROVED-LEAVE   — only an APPROVED leave exempts an employee; a Pending /
 *      ONLY               Rejected / Cancelled leave must NOT remove them.
 *
 * This spec picks a real date that has BOTH no-punch-with-shift and
 * no-punch-without-shift rows, computes ground truth from Postgres, then drives
 * the live API (via Burp) to assert both rules. Mirrors the patterns in
 * directorate-report-api.spec.ts. All fixture rows are cleaned up in afterAll.
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;
const LOGIN = process.env.E2E_ADMIN_LOGIN || 'seed.admin';
const PASSWORD = process.env.E2E_ADMIN_PASSWORD || 'Admin@123456';

type NotAttendanceItem = { employeeId: string; shiftId: string | null };
type Body = { data: NotAttendanceItem[]; totalCount: number };

test.describe.serial('Not-attendance alignment with report غير مبصم logic (API, via Burp)', () => {
  let db: Client;
  let ctx: APIRequestContext;
  let token = '';
  let date = ''; // YYYY-MM-DD
  let dbNoPunchWithShift = 0;
  let dbNoPunchAll = 0;
  let shiftlessEmployeeId = ''; // no-punch + NO shift on `date` → must be excluded
  let withShiftEmployeeId = ''; // no-punch + shift on `date`, not on leave → must appear
  let fixtureLeaveId: string | null = null;

  const get = (qs: string) =>
    ctx.get(`/attendance/not-attendance?page=1&${qs}`, {
      headers: { Authorization: `Bearer ${token}` }
    });

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();

    // 1) A date with BOTH no-punch-with-shift (report counts these) AND
    //    no-punch-without-shift (report excludes these) → both rules testable.
    date = (await scalar<string>(db, `
      SELECT to_char(a.date::date, 'YYYY-MM-DD')
      FROM public."Attendances" a
      WHERE a.is_deleted = false
      GROUP BY a.date::date
      HAVING COUNT(*) FILTER (WHERE a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL) > 0
         AND COUNT(*) FILTER (WHERE a.shift_id IS NULL     AND a.check_in_time IS NULL AND a.check_out_time IS NULL) > 0
      ORDER BY COUNT(*) FILTER (WHERE a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL) DESC
      LIMIT 1`)) as string;
    expect(date, 'a date with both shifted & unshifted no-punch rows must exist').toBeTruthy();

    dbNoPunchWithShift = Number(await scalar(db, `
      SELECT COUNT(*) FROM public."Attendances" a
      WHERE a.is_deleted=false AND a.date::date=$1
            AND a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL`, [date]));
    dbNoPunchAll = Number(await scalar(db, `
      SELECT COUNT(*) FROM public."Attendances" a
      WHERE a.is_deleted=false AND a.date::date=$1
            AND a.check_in_time IS NULL AND a.check_out_time IS NULL`, [date]));

    // A no-punch employee WITHOUT a shift that day — the new rule must exclude them.
    shiftlessEmployeeId = (await scalar<string>(db, `
      SELECT a.employee_id FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE a.is_deleted=false AND a.date::date=$1
            AND a.shift_id IS NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL
      LIMIT 1`, [date])) as string;

    // eslint-disable-next-line no-console
    console.log(`[not-attendance] date=${date} dbNoPunchWithShift=${dbNoPunchWithShift} ` +
      `dbNoPunchAll=${dbNoPunchAll} shiftlessEmployee=${shiftlessEmployeeId}`);

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
      if (fixtureLeaveId) {
        await db.query(`DELETE FROM public."Leaves" WHERE id=$1`, [fixtureLeaveId]).catch(() => {});
      }
      await db.end().catch(() => {});
    }
    await ctx?.dispose();
  });

  test('RULE 1: every returned row is scheduled (shift_id != null) and shiftless rows are excluded', async () => {
    const res = await get(`pageSize=1000&date=${date}`);
    expect(res.status(), 'should return 200').toBe(200);
    const body = (await res.json()) as Body;

    expect(body.totalCount, 'must return some غير مبصمين for this date').toBeGreaterThan(0);
    // Every returned record MUST have a shift (the report's a.ShiftId != null rule).
    const withoutShift = body.data.filter((r) => !r.shiftId);
    expect(withoutShift.length, 'no returned row may have a null shift').toBe(0);
    // The page must have excluded the shiftless no-punch rows → strictly fewer than
    // all no-punch rows (regression guard: the old handler had no shift filter).
    expect(body.totalCount, 'shiftless no-punch rows must be excluded')
      .toBeLessThan(dbNoPunchAll);

    withShiftEmployeeId = body.data[0].employeeId;
    expect(withShiftEmployeeId).toBeTruthy();
  });

  test('RULE 1 (negative): a no-punch employee WITHOUT a shift does NOT appear', async () => {
    const res = await get(`pageSize=10&date=${date}&employeeId=${shiftlessEmployeeId}`);
    expect(res.status()).toBe(200);
    const body = (await res.json()) as Body;
    expect(body.totalCount, 'unshifted no-punch employee must be excluded (was over-counted before)')
      .toBe(0);
  });

  test('RULE 2: only an APPROVED leave exempts an employee', async () => {
    // Target a currently-returned (no-punch + shift) employee.
    const before = await get(`pageSize=10&date=${date}&employeeId=${withShiftEmployeeId}`);
    expect(((await before.json()) as Body).totalCount, 'target appears before any leave')
      .toBeGreaterThan(0);

    fixtureLeaveId = randomUUID();
    await db.query(`
      INSERT INTO public."Leaves"
        (id, employee_id, leave_type, start_date, end_date, reason, status, created_at, is_deleted)
      VALUES ($1, $2, 'Ordinary', $3::date, $3::date, 'E2E not-attendance leave-status', 'Pending', NOW(), false)`,
      [fixtureLeaveId, withShiftEmployeeId, date]
    );

    const totalFor = async () => ((await (await get(`pageSize=10&date=${date}&employeeId=${withShiftEmployeeId}`)).json()) as Body).totalCount;

    // Non-approved leave statuses must NOT remove the employee.
    for (const status of ['Pending', 'Rejected', 'Cancelled']) {
      await db.query(`UPDATE public."Leaves" SET status=$1 WHERE id=$2`, [status, fixtureLeaveId]);
      expect(await totalFor(), `${status} leave must NOT exempt the employee`).toBeGreaterThan(0);
    }

    // Approved leave MUST remove them.
    await db.query(`UPDATE public."Leaves" SET status='Approved' WHERE id=$1`, [fixtureLeaveId]);
    expect(await totalFor(), 'Approved leave must exempt the employee').toBe(0);
  });
});
