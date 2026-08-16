import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';
import { uniqueSuffix } from '../fixtures/test-data';

/**
 * FEATURE 14 — تثبيت دوام الموظف بشكل مباشر (weekly fixed-shift pattern) — API.
 *
 * The employee gets a per-day-of-week fixed shift (EmployeeWeeklyShifts table,
 * dayOfWeek 0=Sunday…6=Saturday) via PUT /employees/{id}/weekly-shifts with
 * FULL-REPLACE semantics (empty days clears the pattern). Attendance resolution
 * falls back to the pattern when no ScheduleDay covers the date, so check-in
 * works without any schedule. Schedules (when present) always win.
 *
 * Covered here: assign + round-trip, validation negatives, delete/deactivate
 * guards, check-in resolution against today's weekday, and clearing.
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

/** The backend's business date is Asia/Baghdad (UTC+3, no DST). */
const baghdadNow = () => new Date(Date.now() + 3 * 3600 * 1000);
const baghdadWeekday = () => baghdadNow().getUTCDay(); // 0=Sunday, matches .NET

test.describe.serial('feature 14 — weekly fixed shift (API)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  let token = '';
  let employeeId = '';
  let organizationId = '';
  let shiftId = '';
  let inactiveShiftId = '';
  const shiftName = `E2E W14 ${uniqueSuffix()}`;
  const inactiveShiftName = `E2E W14 inactive ${uniqueSuffix()}`;

  const auth = () => ({ Authorization: `Bearer ${token}` });

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

    // A clean subject: active employee with an org unit, NO active schedule
    // covering today and NO weekly pattern yet — resolution must come from the
    // pattern this spec assigns.
    const row = await db.query(`
      SELECT e.id, e.organizational_unit_id
      FROM public."Employees" e
      WHERE e.is_deleted = false AND e.organizational_unit_id IS NOT NULL
        AND NOT EXISTS (SELECT 1 FROM public."AttendanceSchedules" s
                        WHERE s.employee_id = e.id AND s.is_active AND NOT s.is_deleted
                          AND s.start_date <= CURRENT_DATE
                          AND (s.end_date IS NULL OR s.end_date >= CURRENT_DATE))
        AND NOT EXISTS (SELECT 1 FROM public."EmployeeWeeklyShifts" w WHERE w.employee_id = e.id)
      ORDER BY e.created_at DESC LIMIT 1`);
    expect(row.rowCount, 'a schedule-less employee must exist').toBe(1);
    employeeId = row.rows[0].id;
    organizationId = row.rows[0].organizational_unit_id;

    // Fresh shifts for this run (unique names — the dump has E2E leftovers).
    const mkShift = async (name: string, isActive: boolean) => {
      const res = await ctx.post('/shifts', {
        headers: auth(),
        data: {
          name,
          startTime: '09:00:00',
          endTime: '16:00:00',
          shiftType: 'Morning',
          isActive,
          gracePeriodMinutes: 10,
          allowEarlyCheckIn: false,
          allowLateCheckOut: false
        }
      });
      expect(res.ok(), `create shift ${name}`).toBeTruthy();
      return String(await res.json()).replace(/"/g, '');
    };
    shiftId = await mkShift(shiftName, true);
    inactiveShiftId = await mkShift(inactiveShiftName, false);
  });

  test('PUT weekly-shifts assigns a full pattern → 204, GET round-trips it', async () => {
    const days = [0, 1, 2, 3, 4, 5, 6].map((dayOfWeek) => ({ dayOfWeek, shiftId }));
    const put = await ctx.put(`/employees/${employeeId}/weekly-shifts`, {
      headers: auth(),
      data: { days }
    });
    expect(put.status(), 'assign pattern').toBe(204);

    const get = await ctx.get(`/employees/${employeeId}`, { headers: auth() });
    expect(get.ok()).toBeTruthy();
    const weekly = (await get.json()).data.weeklyShifts;
    expect(weekly, 'weeklyShifts present in employee VM').toHaveLength(7);
    expect(weekly[0]).toMatchObject({ dayOfWeek: 0, shiftId, shiftName });
    expect(weekly.map((w: { dayOfWeek: number }) => w.dayOfWeek)).toEqual([0, 1, 2, 3, 4, 5, 6]);
  });

  test('GET employees/weekly-shifts lists the employee with grouped days (feature 14b)', async () => {
    // The subject's empId (code) — needed for the searchTerm assertions.
    const detail = await ctx.get(`/employees/${employeeId}`, { headers: auth() });
    const empCode: string = (await detail.json()).data.empId;

    // Grouped listing contains the subject with all 7 days on the E2E shift.
    const list = await ctx.get('/employees/weekly-shifts?page=1&pageSize=100', {
      headers: auth()
    });
    expect(list.ok(), 'weekly-shifts list should load').toBeTruthy();
    const body = await list.json();
    expect(body.totalCount, 'counts employees, not pattern rows').toBeGreaterThanOrEqual(1);
    const row = body.data.find(
      (r: { employeeId: string }) => r.employeeId === employeeId
    );
    expect(row, 'subject employee grouped row present').toBeTruthy();
    expect(row.empId).toBe(empCode);
    expect(row.days).toHaveLength(7);
    expect(row.days[0]).toMatchObject({ dayOfWeek: 0, shiftId, shiftName });

    // Ground truth: grouped days match the raw table rows.
    const pg = await db.query(
      'SELECT count(*)::int AS n FROM public."EmployeeWeeklyShifts" WHERE employee_id = $1',
      [employeeId]
    );
    expect(row.days).toHaveLength(pg.rows[0].n);

    // searchTerm matches by employee code and narrows the result.
    const byCode = await ctx.get(
      `/employees/weekly-shifts?page=1&pageSize=10&searchTerm=${encodeURIComponent(empCode)}`,
      { headers: auth() }
    );
    const byCodeBody = await byCode.json();
    expect(byCodeBody.totalCount).toBe(1);
    expect(byCodeBody.data[0].employeeId).toBe(employeeId);

    // pagination metadata is per employee
    const paged = await ctx.get('/employees/weekly-shifts?page=1&pageSize=1', {
      headers: auth()
    });
    const pagedBody = await paged.json();
    expect(pagedBody.data).toHaveLength(1);
    expect(pagedBody.totalPages).toBe(pagedBody.totalCount);
  });

  test('duplicate weekday in one payload → 400 validation', async () => {
    const res = await ctx.put(`/employees/${employeeId}/weekly-shifts`, {
      headers: auth(),
      data: { days: [{ dayOfWeek: 0, shiftId }, { dayOfWeek: 0, shiftId }] }
    });
    expect(res.status()).toBe(400);
  });

  test('unknown shift id → 404 Shift.NotFound', async () => {
    const res = await ctx.put(`/employees/${employeeId}/weekly-shifts`, {
      headers: auth(),
      data: { days: [{ dayOfWeek: 0, shiftId: '11111111-1111-1111-1111-111111111111' }] }
    });
    expect(res.status()).toBe(404);
    expect(JSON.stringify(await res.json())).toContain('Shift.NotFound');
  });

  test('inactive shift → 400 Shift.Inactive', async () => {
    const res = await ctx.put(`/employees/${employeeId}/weekly-shifts`, {
      headers: auth(),
      data: { days: [{ dayOfWeek: 0, shiftId: inactiveShiftId }] }
    });
    expect(res.status()).toBe(400);
    expect(JSON.stringify(await res.json())).toContain('Shift.Inactive');
  });

  test('unknown employee → 404 Employee not found', async () => {
    const res = await ctx.put(
      '/employees/11111111-1111-1111-1111-111111111111/weekly-shifts',
      { headers: auth(), data: { days: [] } }
    );
    expect(res.status()).toBe(404);
  });

  test('an assigned shift cannot be deleted (Shift.CannotDeleteInUse)', async () => {
    // The 5-min background job may already have stamped this shift into today's
    // auto-created attendance row (that's the feature working); remove such rows
    // so the WEEKLY-PATTERN guard is what blocks the delete, not the pre-existing
    // attendance-records guard.
    await db.query(`DELETE FROM public."Attendances" WHERE shift_id = $1`, [shiftId]);

    const res = await ctx.delete(`/shifts/${shiftId}`, { headers: auth() });
    expect(res.status()).toBe(400);
    expect(JSON.stringify(await res.json())).toContain('Shift.CannotDeleteInUse');
  });

  test('an assigned shift cannot be deactivated (Shift.CannotUpdateInUse)', async () => {
    const res = await ctx.put(`/shifts/${shiftId}`, {
      headers: auth(),
      data: {
        name: shiftName,
        startTime: '09:00:00',
        endTime: '16:00:00',
        shiftType: 'Morning',
        isActive: false
      }
    });
    expect(res.status()).toBe(400);
    expect(JSON.stringify(await res.json())).toContain('Shift.CannotUpdateInUse');
  });

  test('check-in resolves the shift from the weekly pattern (no schedule needed)', async () => {
    // Make sure no attendance row exists for the backend's business date yet.
    await db.query(
      `DELETE FROM public."Attendances"
       WHERE employee_id = $1 AND date::date >= CURRENT_DATE`,
      [employeeId]
    );

    const nowIso = new Date().toISOString().slice(0, 19) + 'Z';
    const res = await ctx.post('/attendance/check-in', {
      headers: auth(),
      data: {
        employeeId,
        organizationId,
        dateTimeAttend: nowIso,
        dateWork: nowIso.slice(0, 10),
        checkInMethod: 1,
        checkOutMethod: 1,
        cardNo: 'E2E14',
        empID: 'E2E14',
        timeAttend: '09:15:00',
        direct: 1,
        deviceName: 'e2e-feature14',
        deviceNo: '0',
        empName: 'e2e'
      }
    });
    expect(res.status(), 'check-in must succeed via the weekly pattern').toBe(200);
    const body = await res.json();
    expect(body.shiftId, 'shift resolved from the pattern').toBe(shiftId);
    expect(body.attendanceScheduleId, 'weekly-pattern provenance (no schedule)').toBeNull();
    expect(typeof body.lateMinutes, 'lateness computed against the pattern shift').toBe('number');
  });

  test('PUT with empty days clears the whole pattern', async () => {
    // Remove any attendance rows carrying the shift so it can be deleted in afterAll.
    await db.query(`DELETE FROM public."Attendances" WHERE shift_id = $1`, [shiftId]);

    const put = await ctx.put(`/employees/${employeeId}/weekly-shifts`, {
      headers: auth(),
      data: { days: [] }
    });
    expect(put.status()).toBe(204);

    const get = await ctx.get(`/employees/${employeeId}`, { headers: auth() });
    expect((await get.json()).data.weeklyShifts).toHaveLength(0);
  });

  test.afterAll(async () => {
    // Idempotent cleanup: pattern, any attendance we created, then the shifts.
    await ctx
      .put(`/employees/${employeeId}/weekly-shifts`, { headers: auth(), data: { days: [] } })
      .catch(() => {});
    await db
      .query(`DELETE FROM public."Attendances" WHERE shift_id = $1`, [shiftId])
      .catch(() => {});
    await ctx.delete(`/shifts/${shiftId}`, { headers: auth() }).catch(() => {});
    await ctx.delete(`/shifts/${inactiveShiftId}`, { headers: auth() }).catch(() => {});
    await db?.end().catch(() => {});
    await ctx.dispose();
  });
});
