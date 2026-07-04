import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';
import { pgClient, scalar } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 09: Overtime report with a from–to filter spanning previous months
 *   (احتساب ساعة العمل الاضافي فلترة من الى للاشهر السابقة).
 *
 * The Application-layer query/handler/validator already existed but were dead code —
 * no HTTP endpoint exposed them. This feature adds GET /reports/overtime. The Arabic
 * asks ONLY for a from–to report over (possibly previous) months with cumulative
 * totals; the per-shift overtime formula is unchanged.
 *
 * This spec logs in as seed.admin (Admin → handler applies no org-unit scoping, so it
 * sees the whole DB), computes ground-truth cumulative overtime straight from Postgres
 * for a CROSS-MONTH range, and asserts the endpoint's statistics + per-employee
 * summaries match. It also guards: missing required params → 400 (NOT the 500 that bare
 * required binding would give), reversed range rejected, and final-day inclusivity
 * (the end-of-day boundary fix). Requests are routed through Burp when configured.
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;
const LOGIN = process.env.E2E_ADMIN_LOGIN || 'seed.admin';
const PASSWORD = process.env.E2E_ADMIN_PASSWORD || 'Admin@123456';

// Cross-month window (Dec 2025 → Feb 2026) — the data set spans 2025-11 .. 2026-06.
const RANGE_START = '2025-12-01';
const RANGE_END = '2026-02-28';

type Truth = {
  totalMinutes: number;
  employeesWithOt: number;
  topEmployeeId: string;
  topEmployeeMinutes: number;
  topEmployeeDays: number;
  singleDay: string;
  singleDayMinutes: number;
};

const auth = (token: string) => ({ headers: { Authorization: `Bearer ${token}` } });

test.describe.serial('Feature 09 — overtime report from–to (API, via Burp)', () => {
  let db: Client;
  let ctx: APIRequestContext;
  let token = '';
  let truth: Truth;

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();

    // Ground truth mirrors the handler: a.is_deleted=false, overtime>0, date in range,
    // grouped per (non-deleted) employee. Admin role => no unit filter.
    const totalMinutes = Number(await scalar(db, `
      SELECT COALESCE(SUM(a.overtime_minutes),0)
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE a.is_deleted=false AND a.overtime_minutes>0
            AND a.date::date BETWEEN $1 AND $2`, [RANGE_START, RANGE_END]));

    const employeesWithOt = Number(await scalar(db, `
      SELECT COUNT(DISTINCT a.employee_id)
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE a.is_deleted=false AND a.overtime_minutes>0
            AND a.date::date BETWEEN $1 AND $2`, [RANGE_START, RANGE_END]));

    const top = await db.query(`
      SELECT a.employee_id AS id,
             SUM(a.overtime_minutes) AS minutes,
             COUNT(*) AS days
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE a.is_deleted=false AND a.overtime_minutes>0
            AND a.date::date BETWEEN $1 AND $2
      GROUP BY a.employee_id
      ORDER BY SUM(a.overtime_minutes) DESC
      LIMIT 1`, [RANGE_START, RANGE_END]);
    expect(top.rows.length, 'a top overtime employee must exist').toBeGreaterThan(0);

    // A specific day with overtime — used to assert the inclusive end-of-day boundary.
    const day = await db.query(`
      SELECT to_char(a.date::date,'YYYY-MM-DD') AS d, SUM(a.overtime_minutes) AS minutes
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE a.is_deleted=false AND a.overtime_minutes>0
            AND a.date::date BETWEEN $1 AND $2
      GROUP BY a.date::date
      ORDER BY SUM(a.overtime_minutes) DESC
      LIMIT 1`, [RANGE_START, RANGE_END]);

    truth = {
      totalMinutes,
      employeesWithOt,
      topEmployeeId: top.rows[0].id,
      topEmployeeMinutes: Number(top.rows[0].minutes),
      topEmployeeDays: Number(top.rows[0].days),
      singleDay: day.rows[0].d,
      singleDayMinutes: Number(day.rows[0].minutes)
    };
    // eslint-disable-next-line no-console
    console.log(`[feature-09] range=${RANGE_START}..${RANGE_END} totalMinutes=${totalMinutes} ` +
      `employeesWithOt=${employeesWithOt} topEmp=${truth.topEmployeeId} topMin=${truth.topEmployeeMinutes} ` +
      `singleDay=${truth.singleDay} singleDayMin=${truth.singleDayMinutes}`);

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
    await db?.end().catch(() => {});
    await ctx?.dispose();
  });

  test('cross-month range totals match DB ground truth', async () => {
    const res = await ctx.get(
      `/reports/overtime?StartDate=${RANGE_START}&EndDate=${RANGE_END}`, auth(token));
    expect(res.status(), 'overtime report should return 200').toBe(200);
    const data = await res.json();

    // statistics.totalOvertimeHours == DB minutes / 60 (compare in minutes to dodge fp noise).
    expect(Math.round(data.statistics.totalOvertimeHours * 60),
      'total overtime minutes must equal DB truth').toBe(truth.totalMinutes);
    expect(data.statistics.employeesWithOvertime,
      'employees-with-overtime count must match DB').toBe(truth.employeesWithOt);

    // Per-employee cumulative summary for the heaviest-overtime employee.
    const emp = (data.employeeSummaries || []).find(
      (e: { employeeId: string }) => e.employeeId === truth.topEmployeeId);
    expect(emp, 'top overtime employee must appear in employeeSummaries').toBeTruthy();
    expect(Math.round(emp.totalOvertimeHours * 60),
      'employee cumulative overtime minutes must match DB').toBe(truth.topEmployeeMinutes);
    expect(emp.overtimeDays, 'overtimeDays = count of OT attendance rows').toBe(truth.topEmployeeDays);
  });

  test('missing required from/to params return 400 (not 500)', async () => {
    const none = await ctx.get(`/reports/overtime`, auth(token));
    expect(none.status(), 'no dates → 400').toBe(400);

    const onlyStart = await ctx.get(`/reports/overtime?StartDate=${RANGE_START}`, auth(token));
    expect(onlyStart.status(), 'only StartDate → 400').toBe(400);

    const onlyEnd = await ctx.get(`/reports/overtime?EndDate=${RANGE_END}`, auth(token));
    expect(onlyEnd.status(), 'only EndDate → 400').toBe(400);
  });

  test('reversed range (from > to) is rejected', async () => {
    const res = await ctx.get(
      `/reports/overtime?StartDate=${RANGE_END}&EndDate=${RANGE_START}`, auth(token));
    expect(res.status(), 'from > to must not return 200').not.toBe(200);
    expect(res.status()).toBeGreaterThanOrEqual(400);
  });

  test('single-day range is inclusive of its full day (end-of-day boundary)', async () => {
    const res = await ctx.get(
      `/reports/overtime?StartDate=${truth.singleDay}&EndDate=${truth.singleDay}`, auth(token));
    expect(res.status(), 'single-day range should return 200').toBe(200);
    const data = await res.json();
    expect(Math.round(data.statistics.totalOvertimeHours * 60),
      'that day\'s overtime must be included in full').toBe(truth.singleDayMinutes);
  });

  test('unauthenticated request is rejected', async () => {
    const res = await ctx.get(`/reports/overtime?StartDate=${RANGE_START}&EndDate=${RANGE_END}`);
    expect([401, 403], 'no token → 401/403').toContain(res.status());
  });
});
