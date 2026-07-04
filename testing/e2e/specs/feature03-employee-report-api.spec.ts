import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';
import { pgClient, scalar } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 03 (التقارير) — part B: admin-only per-employee report (from–to).
 *
 * GET /reports/employee?EmployeeId&FromDate&ToDate returns one employee's
 * day-by-day attendance plus period totals, and is restricted to Admin/SuperAdmin.
 *
 * This spec: (1) picks a real employee + date range with data and computes ground
 * truth straight from Postgres (mirroring the handler), (2) asserts the endpoint's
 * per-day rows and totals equal the DB truth, and (3) asserts the authorization
 * gate — 401 without a token, 400 on an invalid range, and 403 for a non-admin
 * (seed.admin is temporarily demoted to Employee, re-logged-in, then restored).
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;
const LOGIN = process.env.E2E_ADMIN_LOGIN || 'seed.admin';
const PASSWORD = process.env.E2E_ADMIN_PASSWORD || 'Admin@123456';

type Truth = {
  employeeId: string;
  employeeName: string;
  from: string; // YYYY-MM-DD
  to: string; // YYYY-MM-DD
  totalDays: number;
  presentDays: number;
  absentDays: number;
  lateDays: number;
  leaveDays: number;
  overtimeHours: number;
};

const newContext = () =>
  pwRequest.newContext({
    baseURL: API,
    ignoreHTTPSErrors: true,
    ...(BURP ? { proxy: { server: BURP } } : {})
  });

test.describe.serial('Feature 03 — employee report (API, via Burp)', () => {
  let db: Client;
  let ctx: APIRequestContext;
  let token = '';
  let origRole: string | null = null;
  let truth: Truth;

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();

    // 1) Pick the employee with the most attendance rows, and a tight window
    //    inside their data so totals are non-trivial.
    const pick = await db.query(`
      SELECT a.employee_id AS id, e.full_name AS name,
             to_char(MIN(a.date)::date, 'YYYY-MM-DD') AS dmin,
             to_char(MAX(a.date)::date, 'YYYY-MM-DD') AS dmax
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id = a.employee_id AND e.is_deleted = false
      WHERE a.is_deleted = false
      GROUP BY a.employee_id, e.full_name
      ORDER BY COUNT(*) DESC
      LIMIT 1;`);
    expect(pick.rows.length, 'an employee with attendance data must exist').toBeGreaterThan(0);
    const employeeId: string = pick.rows[0].id;
    const employeeName: string = pick.rows[0].name;
    const from: string = pick.rows[0].dmin;
    const to: string = pick.rows[0].dmax;

    // 2) Ground truth over [from, to] — mirrors GetEmployeeReportHandler.
    const totals = await db.query(
      `
      SELECT COUNT(*) AS total,
             COUNT(*) FILTER (WHERE status='Present')  AS present,
             COUNT(*) FILTER (WHERE status='Absent')   AS absent,
             COUNT(*) FILTER (WHERE late_minutes > 0)  AS late,
             COUNT(*) FILTER (WHERE status='Vacation') AS leave,
             COALESCE(SUM(overtime_minutes), 0)        AS ot_minutes
      FROM public."Attendances"
      WHERE employee_id=$1 AND is_deleted=false
            AND date::date >= $2::date AND date::date <= $3::date`,
      [employeeId, from, to]
    );
    const r = totals.rows[0];

    truth = {
      employeeId,
      employeeName,
      from,
      to,
      totalDays: Number(r.total),
      presentDays: Number(r.present),
      absentDays: Number(r.absent),
      lateDays: Number(r.late),
      leaveDays: Number(r.leave),
      overtimeHours: Math.round((Number(r.ot_minutes) / 60) * 100) / 100
    };
    // eslint-disable-next-line no-console
    console.log(`[feature-03] employee=${employeeName} ${from}..${to} ` +
      `days=${truth.totalDays} present=${truth.presentDays} late=${truth.lateDays}`);

    // 3) API context + admin login.
    ctx = await newContext();
    const login = await ctx.post('/auth/login', { data: { userLogin: LOGIN, password: PASSWORD } });
    expect(login.ok(), 'admin login should succeed').toBeTruthy();
    token = (await login.json()).data.token;
  });

  test.afterAll(async () => {
    if (db) {
      // Safety net: always restore seed.admin's role if a test left it demoted.
      if (origRole) {
        await db.query(
          `UPDATE public."Users" SET role=$1 WHERE user_login=$2`, [origRole, LOGIN]
        ).catch(() => {});
      }
      await db.end().catch(() => {});
    }
    await ctx?.dispose();
  });

  test('GET /reports/employee returns correct per-day rows and totals', async () => {
    const res = await ctx.get(
      `/reports/employee?EmployeeId=${truth.employeeId}` +
      `&FromDate=${truth.from}&ToDate=${truth.to}`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    expect(res.status(), 'report should return 200').toBe(200);
    const data = (await res.json()).data;

    expect(data.employeeId).toBe(truth.employeeId);
    expect(data.employeeName).toBe(truth.employeeName);

    // Period totals equal the DB truth.
    expect(data.totalDays, 'total days').toBe(truth.totalDays);
    expect(data.presentDays, 'present days').toBe(truth.presentDays);
    expect(data.absentDays, 'absent days').toBe(truth.absentDays);
    expect(data.lateDays, 'late days').toBe(truth.lateDays);
    expect(data.leaveDays, 'leave days').toBe(truth.leaveDays);
    expect(data.totalOvertimeHours, 'overtime hours').toBeCloseTo(truth.overtimeHours, 2);

    // The per-day list is complete and labeled.
    expect(Array.isArray(data.days)).toBeTruthy();
    expect(data.days.length, 'one row per attendance day').toBe(truth.totalDays);
    for (const day of data.days) {
      expect(typeof day.statusName).toBe('string');
      expect(day.statusName.length, 'status must carry an Arabic label').toBeGreaterThan(0);
      // غير مبصم flag is consistent with the punches.
      expect(day.isNonFingerprinted).toBe(!day.checkInTime && !day.checkOutTime);
    }
  });

  test('requires authentication — 401 without a token', async () => {
    const res = await ctx.get(
      `/reports/employee?EmployeeId=${truth.employeeId}&FromDate=${truth.from}&ToDate=${truth.to}`
    );
    expect(res.status(), 'unauthenticated request must be rejected').toBe(401);
  });

  test('rejects an invalid date range — 400 when from > to', async () => {
    const res = await ctx.get(
      `/reports/employee?EmployeeId=${truth.employeeId}&FromDate=${truth.to}&ToDate=${truth.from}`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    expect(res.status(), 'from > to must be a 400').toBe(400);
  });

  test('admin-only — a non-admin (Employee) is forbidden (403)', async () => {
    // Demote seed.admin in the DB, then log in fresh so the JWT carries the
    // non-admin role claim the endpoint reads. Restored immediately + in afterAll.
    origRole = await scalar<string>(
      db, `SELECT role FROM public."Users" WHERE user_login=$1`, [LOGIN]
    );
    await db.query(`UPDATE public."Users" SET role='Employee' WHERE user_login=$1`, [LOGIN]);

    const demoted = await newContext();
    try {
      const login = await demoted.post('/auth/login', {
        data: { userLogin: LOGIN, password: PASSWORD }
      });
      expect(login.ok(), 'demoted login should still succeed').toBeTruthy();
      const demotedToken = (await login.json()).data.token;

      const res = await demoted.get(
        `/reports/employee?EmployeeId=${truth.employeeId}&FromDate=${truth.from}&ToDate=${truth.to}`,
        { headers: { Authorization: `Bearer ${demotedToken}` } }
      );
      expect(res.status(), 'non-admin must be forbidden').toBe(403);
    } finally {
      await db.query(`UPDATE public."Users" SET role=$1 WHERE user_login=$2`, [origRole, LOGIN]);
      origRole = null;
      await demoted.dispose();
    }
  });
});
