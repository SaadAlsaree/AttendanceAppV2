import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 05 — انشاء جداول: schedules for newly added employees (admin only).
 * The Arabic «حتى يظهر كل الحضور بشكل كامل» is the RESULT of being able to add a
 * schedule (attendance then computes against it going forward), not a request to
 * retroactively backfill past rows — so this is purely about creation working.
 *
 * Backend coverage:
 *  1) Schedule creation works for a brand-new employee (no existing schedule):
 *     POST /attendance-schedules returns 200 and writes one ScheduleDay per day.
 *     (Before the fix the frontend dropped `scheduleDayDate` and the backend
 *     validator rejected every create.)
 *  2) The backend requires `scheduleDayDate` per day (the contract the frontend
 *     now satisfies): omitting it is rejected.
 *  3) Auth is enforced (unauthenticated create is rejected).
 *
 * Ground truth via Postgres (E2E_PG_*); the action goes through the API. Fixtures
 * are cleaned up in afterAll.
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

const iso = (d: Date) => d.toISOString().slice(0, 10); // YYYY-MM-DD

test.describe.serial('feature 05 — schedules for newly added employees (API)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  let token = '';
  let employeeId = '';
  let shiftId = '';
  let createdScheduleId = '';

  // 7-day window ending today; every date carries a ScheduleDay.
  const today = new Date();
  const days: Date[] = Array.from({ length: 7 }, (_, i) => {
    const d = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate()));
    d.setUTCDate(d.getUTCDate() - (6 - i));
    return d;
  });
  const startDate = iso(days[0]);
  const endDate = iso(days[6]);

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({
      baseURL: API,
      ignoreHTTPSErrors: true,
      ...(BURP ? { proxy: { server: BURP } } : {})
    });

    const login = await ctx.post('/auth/login', {
      data: { userLogin: 'seed.admin', password: 'Admin@123456' }
    });
    expect(login.ok(), 'admin login should succeed').toBeTruthy();
    token = (await login.json()).data.token;

    db = pgClient();
    await db.connect();

    // A "newly added" employee: has an org unit, not deleted, and NO schedule yet.
    const emp = await db.query(
      `SELECT e.id
         FROM "Employees" e
        WHERE e.organizational_unit_id IS NOT NULL
          AND e.is_deleted = false
          AND NOT EXISTS (
            SELECT 1 FROM "AttendanceSchedules" s
             WHERE s.employee_id = e.id AND s.is_deleted = false)
        LIMIT 1`
    );
    expect(emp.rows.length, 'need an employee with an org unit and no schedule').toBe(1);
    employeeId = emp.rows[0].id;

    const shift = await db.query(`SELECT id FROM "Shifts" WHERE is_deleted = false LIMIT 1`);
    expect(shift.rows.length, 'need at least one shift').toBe(1);
    shiftId = shift.rows[0].id;
  });

  test('POST /attendance-schedules creates a schedule for a new employee and writes all ScheduleDays', async () => {
    const scheduleDays = days.map((d) => ({
      shiftId,
      scheduleDayDate: iso(d), // the field the frontend used to drop
      isActive: true,
      notes: ''
    }));

    const res = await ctx.post('/attendance-schedules', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        employeeId,
        startDate,
        endDate,
        scheduleType: 'Regular',
        isActive: true,
        notes: 'E2E feature05',
        scheduleDays,
        excludedDates: []
      }
    });
    expect(res.status(), 'create must be accepted').toBe(200);

    // One AttendanceSchedule for the employee.
    const sched = await db.query(
      `SELECT id, is_active FROM "AttendanceSchedules"
        WHERE employee_id = $1 AND is_deleted = false
        ORDER BY created_at DESC LIMIT 1`,
      [employeeId]
    );
    expect(sched.rows.length, 'schedule row should exist').toBe(1);
    expect(sched.rows[0].is_active).toBe(true);
    createdScheduleId = sched.rows[0].id;

    // One ScheduleDay per day in the range.
    const cnt = await db.query(
      `SELECT count(*)::int AS n FROM "ScheduleDays" WHERE attendance_schedule_id = $1`,
      [createdScheduleId]
    );
    expect(cnt.rows[0].n, 'should write one ScheduleDay per day').toBe(days.length);
  });

  test('backend rejects a schedule day missing scheduleDayDate', async () => {
    const res = await ctx.post('/attendance-schedules', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        employeeId,
        startDate,
        endDate,
        scheduleType: 'Regular',
        isActive: true,
        scheduleDays: [{ shiftId, isActive: true }], // no scheduleDayDate
        excludedDates: []
      }
    });
    expect(res.status(), 'missing scheduleDayDate must be rejected').not.toBe(200);
  });

  test('unauthenticated create is rejected', async () => {
    const res = await ctx.post('/attendance-schedules', {
      data: {
        employeeId,
        startDate,
        endDate,
        scheduleType: 'Regular',
        isActive: true,
        scheduleDays: [{ shiftId, scheduleDayDate: startDate, isActive: true }],
        excludedDates: []
      }
    });
    expect([401, 403], 'no bearer → unauthorized').toContain(res.status());
  });

  test.afterAll(async () => {
    // Hard-clean fixtures so reruns find a pristine employee.
    if (db) {
      if (createdScheduleId) {
        await db
          .query(`DELETE FROM "ScheduleDays" WHERE attendance_schedule_id = $1`, [createdScheduleId])
          .catch(() => {});
        await db
          .query(`DELETE FROM "AttendanceSchedules" WHERE id = $1`, [createdScheduleId])
          .catch(() => {});
      }
      await db.end().catch(() => {});
    }
    await ctx.dispose();
  });
});
