import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 15: احتساب ساعات العمل للمبيت والخفراء بعد الساعة 12 ليلا
 * Work hours for overnight/guard shifts (EndTime < StartTime) must count time
 * after midnight. Real dump data shows the historical bug: خفر rows stored a
 * same-date check-out EARLIER than check-in (e.g. 16:07 → 04:40, working
 * minutes -686). This spec covers both fixed paths against the REAL خفر shift
 * (20:03 → 08:07 local, expected 724 min):
 *
 *  A. Manual POST /attendance/check-out — a post-midnight checkout sent with
 *     the historical broken shape (same-date, earlier than check-in) must be
 *     normalized (+1 day) and yield positive working minutes.
 *  B. Batch pairing (Hangfire create-attendance-records) — a Direct=2 punch
 *     the morning after must be attributed to the PREVIOUS day's overnight
 *     attendance row, not the punch's own calendar day.
 *
 * Fixtures are self-contained test employees (no schedules, unique emp_id) so
 * the 5-minute recurring job cannot collide with real rows; everything is
 * removed in afterAll.
 *
 * Run against the instance under test with E2E_API_URL (feature instance:
 * http://localhost:7090).
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

// Real خفر shift from the dump: 20:03 → 08:07 Asia/Baghdad (UTC+3).
const GUARD_SHIFT_ID = '019a7cb1-094d-7ebb-86b4-d8f889d65c21';
const GUARD_SHIFT_MINUTES = 724; // 20:03 → 08:07 next day

const utcDateOnly = (offsetDays: number) => {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() + offsetDays);
  return d.toISOString().slice(0, 10); // YYYY-MM-DD
};

test.describe.serial('feature 15 — overnight/guard hours after midnight (API)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  let token = '';
  let orgUnitId = '';
  let dayShiftId = ''; // any normal (end > start) shift, for the sad path
  const suffix = `${Date.now() % 100000}`;
  const empA = { id: '', empId: `E2E15A${suffix}` }; // manual check-out path
  const empB = { id: '', empId: `E2E15B${suffix}` }; // batch pairing path
  const attendanceIds: string[] = [];

  const D = utcDateOnly(-1); // overnight work date (yesterday UTC — inside job window)
  const D1 = utcDateOnly(0); // the morning after (today UTC)

  async function insertEmployee(emp: { id: string; empId: string }) {
    const r = await db.query(
      `INSERT INTO "Employees"
         (id, emp_id, rfid, first_name, second_name, third_name, family_name,
          full_name, organizational_unit_id, created_at, is_deleted)
       VALUES (gen_random_uuid(), $1::text, $1::text, 'E2E', 'Feature15', 'Overnight', 'Test',
               'E2E Feature15 ' || $1::text, $2, now(), false)
       RETURNING id`,
      [emp.empId, orgUnitId]
    );
    emp.id = r.rows[0].id;
  }

  async function insertAttendance(
    employeeId: string, dateISO: string, shiftId: string, checkInUtc: string | null
  ): Promise<string> {
    const r = await db.query(
      `INSERT INTO "Attendances"
         (id, employee_id, organization_id, date, shift_id, check_in_time,
          check_in_method, status, created_at, is_deleted)
       VALUES (gen_random_uuid(), $1, $2, $3::timestamptz, $4, $5::timestamptz,
               CASE WHEN $5 IS NULL THEN NULL ELSE 'Biometric' END,
               'Pending', now(), false)
       RETURNING id`,
      [employeeId, orgUnitId, `${dateISO}T00:00:00Z`, shiftId, checkInUtc]
    );
    attendanceIds.push(r.rows[0].id);
    return r.rows[0].id;
  }

  async function insertLog(empId: string, utcISO: string, direct: '1' | '2', dateWork: string) {
    await db.query(
      `INSERT INTO "AttendanceLogs"
         (id, date_time_attend, card_no, device_name, device_no, direct, emp_id,
          date_work, time_attend, created_at, is_deleted)
       VALUES (gen_random_uuid(), $1::timestamptz, $2, 'E2E15', 'E2E15', $3, $2,
               $4::date, ($1::timestamptz AT TIME ZONE 'Asia/Baghdad')::time, now(), false)`,
      [utcISO, empId, direct, dateWork]
    );
  }

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();

    ctx = await pwRequest.newContext({
      baseURL: API,
      ignoreHTTPSErrors: true,
      ...(BURP ? { proxy: { server: BURP } } : {})
    });

    const login = await ctx.post('/auth/login', {
      data: { userLogin: process.env.E2E_ADMIN_LOGIN || 'seed.admin',
              password: process.env.E2E_ADMIN_PASSWORD || 'Admin@123456' }
    });
    expect(login.ok(), 'login should succeed').toBeTruthy();
    token = (await login.json()).data.token;

    // Sanity: the real خفر shift is still the overnight shape this spec assumes.
    const shift = await db.query(
      `SELECT start_time, end_time FROM "Shifts" WHERE id=$1 AND is_deleted=false`,
      [GUARD_SHIFT_ID]
    );
    expect(shift.rows.length, 'خفر shift must exist in dump').toBe(1);
    expect(shift.rows[0].end_time < shift.rows[0].start_time,
      'خفر must be an overnight shift (end < start)').toBeTruthy();

    const dayShift = await db.query(
      `SELECT id FROM "Shifts" WHERE end_time > start_time AND is_deleted=false LIMIT 1`
    );
    dayShiftId = dayShift.rows[0].id;

    const org = await db.query(
      `SELECT organizational_unit_id FROM "Employees"
       WHERE organizational_unit_id IS NOT NULL AND is_deleted=false LIMIT 1`
    );
    orgUnitId = org.rows[0].organizational_unit_id;

    await insertEmployee(empA);
    await insertEmployee(empB);
  });

  test.afterAll(async () => {
    // Handler-created logs carry EmpID = employee GUID; fixture logs carry emp code.
    await db.query(`DELETE FROM "AttendanceLogs" WHERE emp_id = ANY($1)`,
      [[empA.empId, empB.empId, empA.id, empB.id]]).catch(() => {});
    await db.query(`DELETE FROM "Attendances" WHERE employee_id = ANY($1)`,
      [[empA.id, empB.id].filter(Boolean)]).catch(() => {});
    await db.query(`DELETE FROM "Employees" WHERE id = ANY($1)`,
      [[empA.id, empB.id].filter(Boolean)]).catch(() => {});
    await db.end().catch(() => {});
    await ctx.dispose().catch(() => {});
  });

  test('A1: manual check-out after midnight (historical broken shape) yields positive minutes', async () => {
    // Check-in 16:07Z = 19:07 local (before the 20:03 start → late 0).
    const attendanceId = await insertAttendance(empA.id, D, GUARD_SHIFT_ID, `${D}T16:07:00Z`);

    // The historical broken shape: checkout sent on the SAME date, earlier than
    // check-in (04:40Z = 07:40 local, i.e. the next morning in reality).
    const res = await ctx.post('/attendance/check-out', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        employeeId: empA.id,
        attendanceId,
        checkOutTime: `${D}T04:40:00Z`,
        cardNo: 'E2E15',
        name: 'E2E Feature15'
      }
    });
    expect(res.status(), await res.text()).toBe(200);
    const body = await res.json();

    // 16:07Z → next-day 04:40Z = 753 minutes; NEVER negative/zero.
    const row = (await db.query(
      `SELECT check_out_time, working_minutes, early_leave_minutes, late_minutes, status
       FROM "Attendances" WHERE id=$1`, [attendanceId])).rows[0];
    expect(row.working_minutes).toBe(753);
    const expectedOut = new Date(new Date(`${D}T04:40:00Z`).getTime() + 86_400_000).toISOString();
    expect(new Date(row.check_out_time).toISOString()).toBe(expectedOut);
    // 07:40 local vs 08:07 end minus the 10-min grace period (07:57) → 17 early.
    expect(row.early_leave_minutes).toBe(17);
    expect(row.late_minutes).toBe(0);
    // Response mirrors DB.
    expect(body.workingMinutes ?? body.data?.workingMinutes).toBe(753);
  });

  test('A2: non-overnight shift still rejects checkout before check-in', async () => {
    const attendanceId = await insertAttendance(empA.id, D1, dayShiftId, `${D1}T05:00:00Z`);

    const res = await ctx.post('/attendance/check-out', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        employeeId: empA.id,
        attendanceId,
        checkOutTime: `${D1}T04:00:00Z`,
        cardNo: 'E2E15',
        name: 'E2E Feature15'
      }
    });
    expect(res.ok(), 'day-shift checkout before check-in must be rejected').toBeFalsy();

    const row = (await db.query(
      `SELECT check_out_time FROM "Attendances" WHERE id=$1`, [attendanceId])).rows[0];
    expect(row.check_out_time).toBeNull();
  });

  test('B: batch pairing attributes the morning punch to the previous overnight day', async () => {
    test.setTimeout(180_000);

    const dayBefore = await insertAttendance(empB.id, D, GUARD_SHIFT_ID, null);
    const dayAfter = await insertAttendance(empB.id, D1, GUARD_SHIFT_ID, null);

    // On-time punches for the خفر shift: in 20:03 local (17:03Z) on D,
    // out 08:07 local (05:07Z) the morning after — date_work = punch's own day,
    // exactly how the devices record it.
    await insertLog(empB.empId, `${D}T17:03:00Z`, '1', D);
    await insertLog(empB.empId, `${D1}T05:07:00Z`, '2', D1);

    // Trigger the recurring job that runs CreateAttendanceRecordsAsyncIfNotExists
    // + UpdateAttendancesCheckInAndCheckOutAsync (with admin Bearer; the dashboard requires Admin).
    const trig = await ctx.post('/hangfire/recurring/trigger', {
      headers: { Authorization: `Bearer ${token}` }, // dashboard now requires an authenticated Admin/SuperAdmin
      form: { 'jobs[]': 'create-attendance-records' }
    });
    expect(trig.status(), 'hangfire trigger should be accepted').toBeLessThan(400);

    // Poll until the pairing lands (job runs async; also fires every 5 min).
    let row: any = null;
    await expect.poll(async () => {
      row = (await db.query(
        `SELECT check_in_time, check_out_time, working_minutes, late_minutes,
                early_leave_minutes, overtime_minutes, status
         FROM "Attendances" WHERE id=$1`, [dayBefore])).rows[0];
      return row.check_out_time !== null;
    }, { timeout: 120_000, intervals: [3_000] }).toBeTruthy();

    expect(new Date(row.check_in_time).toISOString()).toBe(`${D}T17:03:00.000Z`);
    expect(new Date(row.check_out_time).toISOString()).toBe(`${D1}T05:07:00.000Z`);
    expect(row.working_minutes).toBe(GUARD_SHIFT_MINUTES); // full 724, incl. post-midnight
    expect(row.late_minutes).toBe(0);
    expect(row.early_leave_minutes).toBe(0);
    expect(row.overtime_minutes).toBe(0);
    expect(row.status).toBe('Present');

    // The morning punch must NOT have been claimed by its own calendar day.
    const after = (await db.query(
      `SELECT check_in_time, check_out_time FROM "Attendances" WHERE id=$1`, [dayAfter])).rows[0];
    expect(after.check_in_time).toBeNull();
    expect(after.check_out_time).toBeNull();
  });
});
