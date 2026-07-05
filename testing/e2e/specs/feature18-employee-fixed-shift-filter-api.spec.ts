import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';
import { uniqueSuffix } from '../fixtures/test-data';

/**
 * FEATURE 18 — «تثبيت الدوام» indicator + filter on the employee list — API.
 *
 * GET /employees now returns a `hasFixedShift` flag per row (true ⇔ the employee
 * has ≥1 EmployeeWeeklyShifts row) and accepts a `HasFixedShift` (bool) query
 * filter. This lets the employee table show which employees still lack a fixed
 * weekly pattern and filter to just those. Enforcement mirrors the existing
 * `IsManager` filter; reads stay scoped to the caller's unit tree.
 *
 * Covered here: the flag is present + correct, the filter partitions the list
 * (true + false == all), searchTerm narrows to the subject on each side, and
 * assigning / clearing a pattern flips the subject's membership.
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

test.describe.serial('feature 18 — employee fixed-shift filter (API)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  let token = '';
  let employeeId = '';
  let empCode = '';
  let shiftId = '';
  const shiftName = `E2E F18 ${uniqueSuffix()}`;

  const auth = () => ({ Authorization: `Bearer ${token}` });

  /** totalCount for GET /employees with an arbitrary query-string suffix. */
  const listCount = async (qs: string) => {
    const res = await ctx.get(`/employees?page=1&pageSize=1${qs}`, { headers: auth() });
    expect(res.ok(), `GET /employees${qs} should load`).toBeTruthy();
    return (await res.json()).totalCount as number;
  };

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({
      baseURL: API,
      ignoreHTTPSErrors: true,
      ...(BURP ? { proxy: { server: BURP } } : {})
    });
    db = pgClient();
    await db.connect();

    const login = await ctx.post('/auth/login', {
      data: { userLogin: 'seed.admin', password: 'Admin@123456' }
    });
    expect(login.ok(), 'login should succeed').toBeTruthy();
    token = (await login.json()).data.token;

    // A clean subject: active employee with an org unit and NO weekly pattern yet,
    // so this spec controls whether it has a fixed shift.
    const row = await db.query(`
      SELECT e.id
      FROM public."Employees" e
      WHERE e.is_deleted = false AND e.organizational_unit_id IS NOT NULL
        AND NOT EXISTS (SELECT 1 FROM public."EmployeeWeeklyShifts" w WHERE w.employee_id = e.id)
      ORDER BY e.created_at DESC LIMIT 1`);
    expect(row.rowCount, 'a pattern-less employee must exist').toBe(1);
    employeeId = row.rows[0].id;

    const detail = await ctx.get(`/employees/${employeeId}`, { headers: auth() });
    expect(detail.ok()).toBeTruthy();
    empCode = (await detail.json()).data.empId;

    const mk = await ctx.post('/shifts', {
      headers: auth(),
      data: {
        name: shiftName,
        startTime: '09:00:00',
        endTime: '16:00:00',
        shiftType: 'Morning',
        isActive: true,
        gracePeriodMinutes: 10,
        allowEarlyCheckIn: false,
        allowLateCheckOut: false
      }
    });
    expect(mk.ok(), 'create E2E shift').toBeTruthy();
    shiftId = String(await mk.json()).replace(/"/g, '');
  });

  test('every list row carries a boolean hasFixedShift flag', async () => {
    const res = await ctx.get('/employees?page=1&pageSize=25', { headers: auth() });
    expect(res.ok()).toBeTruthy();
    const rows = (await res.json()).data as Array<{ hasFixedShift: unknown }>;
    expect(rows.length).toBeGreaterThan(0);
    for (const r of rows) {
      expect(typeof r.hasFixedShift, 'hasFixedShift is a boolean').toBe('boolean');
    }
  });

  test('HasFixedShift filter partitions the list (true + false == all)', async () => {
    const all = await listCount('');
    const withShift = await listCount('&HasFixedShift=true');
    const without = await listCount('&HasFixedShift=false');
    expect(withShift + without, 'the two filters partition the full list').toBe(all);
    expect(withShift, 'at least the seeded patterned employees exist').toBeGreaterThan(0);

    // Ground truth: `true` count equals distinct employees with a pattern row.
    const pg = await db.query(
      'SELECT count(DISTINCT employee_id)::int AS n FROM public."EmployeeWeeklyShifts"'
    );
    expect(withShift, 'HasFixedShift=true matches the DB').toBe(pg.rows[0].n);
  });

  test('assigning a pattern flips the subject into HasFixedShift=true', async () => {
    // Before: the subject has no pattern → appears under false, not true.
    const q = `&searchTerm=${encodeURIComponent(empCode)}`;
    expect(await listCount(`${q}&HasFixedShift=false`), 'subject starts unassigned').toBe(1);
    expect(await listCount(`${q}&HasFixedShift=true`), 'subject not yet assigned').toBe(0);

    const put = await ctx.put(`/employees/${employeeId}/weekly-shifts`, {
      headers: auth(),
      data: { days: [0, 1, 2, 3, 4].map((dayOfWeek) => ({ dayOfWeek, shiftId })) }
    });
    expect(put.status(), 'assign pattern').toBe(204);

    // After: the subject moves to true, and its row flag reads true.
    expect(await listCount(`${q}&HasFixedShift=true`), 'subject now assigned').toBe(1);
    expect(await listCount(`${q}&HasFixedShift=false`), 'subject no longer unassigned').toBe(0);

    const res = await ctx.get(`/employees?page=1&pageSize=1${q}`, { headers: auth() });
    const rowFlag = (await res.json()).data[0].hasFixedShift;
    expect(rowFlag, 'row flag reflects the assignment').toBe(true);
  });

  test('clearing the pattern flips the subject back to HasFixedShift=false', async () => {
    const put = await ctx.put(`/employees/${employeeId}/weekly-shifts`, {
      headers: auth(),
      data: { days: [] }
    });
    expect(put.status(), 'clear pattern').toBe(204);

    const q = `&searchTerm=${encodeURIComponent(empCode)}`;
    expect(await listCount(`${q}&HasFixedShift=false`), 'subject back to unassigned').toBe(1);
    expect(await listCount(`${q}&HasFixedShift=true`), 'subject no longer assigned').toBe(0);
  });

  test.afterAll(async () => {
    // Idempotent cleanup: clear the pattern, then delete the shift.
    await ctx
      .put(`/employees/${employeeId}/weekly-shifts`, { headers: auth(), data: { days: [] } })
      .catch(() => {});
    await db
      .query(`DELETE FROM public."Attendances" WHERE shift_id = $1`, [shiftId])
      .catch(() => {});
    await ctx.delete(`/shifts/${shiftId}`, { headers: auth() }).catch(() => {});
    await db?.end().catch(() => {});
    await ctx.dispose();
  });
});
