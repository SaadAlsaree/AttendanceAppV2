import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 10: ساعات العمل تؤشر 0 بوجود وقت دخول ووقت خروج
 * Working minutes were only computed inside a `shift is not null` guard, so
 * NO-SHIFT attendance rows kept working_minutes NULL and the UI showed
 * "0 ساعات". The fix decouples elapsed working minutes from the shift in all
 * three write paths (check-out, update, biometric batch job); reversed /
 * duplicate scan pairs store NULL (never 0 or negative).
 *
 * All fixture employees here deliberately have NO shift, NO schedule and NO
 * weekly pattern. Everything is removed in afterAll.
 *
 * Run against the instance under test with E2E_API_URL (feature instance:
 * http://localhost:7100). For the batch test keep only ONE API instance
 * running (shared Hangfire schema).
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

const utcDateOnly = (offsetDays: number) => {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() + offsetDays);
  return d.toISOString().slice(0, 10); // YYYY-MM-DD
};

test.describe.serial('feature 10 — no-shift working minutes (API)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  let token = '';
  let orgUnitId = '';
  const suffix = `${Date.now() % 100000}`;
  const empU = { id: '', empId: `E2E10U${suffix}` }; // update path
  const empC = { id: '', empId: `E2E10C${suffix}` }; // check-out path
  const empB = { id: '', empId: `E2E10B${suffix}` }; // batch pairing path
  const attendanceIds: string[] = [];

  const Y = utcDateOnly(-1); // yesterday UTC (update-path fixture)
  const T = utcDateOnly(0); // today UTC (check-out + batch fixtures)

  async function insertEmployee(emp: { id: string; empId: string }) {
    const r = await db.query(
      `INSERT INTO "Employees"
         (id, emp_id, rfid, first_name, second_name, third_name, family_name,
          full_name, organizational_unit_id, created_at, is_deleted)
       VALUES (gen_random_uuid(), $1::text, $1::text, 'E2E', 'Feature10', 'WorkHours', 'Test',
               'E2E Feature10 ' || $1::text, $2, now(), false)
       RETURNING id`,
      [emp.empId, orgUnitId]
    );
    emp.id = r.rows[0].id;
  }

  // NO shift_id on purpose — that is the whole point of feature 10.
  async function insertNoShiftAttendance(
    employeeId: string, dateISO: string, checkInUtc: string | null
  ): Promise<string> {
    const r = await db.query(
      `INSERT INTO "Attendances"
         (id, employee_id, organization_id, date, shift_id, check_in_time,
          check_in_method, status, created_at, is_deleted)
       VALUES (gen_random_uuid(), $1, $2, $3::timestamptz, NULL, $4::timestamptz,
               CASE WHEN $4 IS NULL THEN NULL ELSE 'Biometric' END,
               'Pending', now(), false)
       RETURNING id`,
      [employeeId, orgUnitId, `${dateISO}T00:00:00Z`, checkInUtc]
    );
    attendanceIds.push(r.rows[0].id);
    return r.rows[0].id;
  }

  async function insertLog(empId: string, utcISO: string, direct: '1' | '2', dateWork: string) {
    await db.query(
      `INSERT INTO "AttendanceLogs"
         (id, date_time_attend, card_no, device_name, device_no, direct, emp_id,
          date_work, time_attend, created_at, is_deleted)
       VALUES (gen_random_uuid(), $1::timestamptz, $2, 'E2E10', 'E2E10', $3, $2,
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

    const org = await db.query(
      `SELECT organizational_unit_id FROM "Employees"
       WHERE organizational_unit_id IS NOT NULL AND is_deleted=false LIMIT 1`
    );
    orgUnitId = org.rows[0].organizational_unit_id;

    await insertEmployee(empU);
    await insertEmployee(empC);
    await insertEmployee(empB);
  });

  test.afterAll(async () => {
    // Handler-created logs carry EmpID = employee GUID; fixture logs carry emp code.
    await db.query(`DELETE FROM "AttendanceLogs" WHERE emp_id = ANY($1)`,
      [[empU.empId, empC.empId, empB.empId, empU.id, empC.id, empB.id]]).catch(() => {});
    await db.query(`DELETE FROM "Attendances" WHERE employee_id = ANY($1)`,
      [[empU.id, empC.id, empB.id].filter(Boolean)]).catch(() => {});
    await db.query(`DELETE FROM "Employees" WHERE id = ANY($1)`,
      [[empU.id, empC.id, empB.id].filter(Boolean)]).catch(() => {});
    await db.end().catch(() => {});
    await ctx.dispose().catch(() => {});
  });

  test('U1: PUT /attendance/{id} on a NO-SHIFT record computes working minutes', async () => {
    const attendanceId = await insertNoShiftAttendance(empU.id, Y, null);

    const res = await ctx.put(`/attendance/${attendanceId}`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        checkInTime: `${Y}T05:00:00Z`,
        checkOutTime: `${Y}T13:30:00Z`
      }
    });
    expect(res.status(), await res.text()).toBe(200);
    const body = await res.json();
    expect(body.workingMinutes ?? body.data?.workingMinutes).toBe(510);

    const row = (await db.query(
      `SELECT working_minutes, late_minutes, early_leave_minutes, overtime_minutes
       FROM "Attendances" WHERE id=$1`, [attendanceId])).rows[0];
    expect(row.working_minutes).toBe(510); // 05:00 → 13:30 = 8.5h — NOT NULL, NOT 0
    // Shift-dependent metrics stay unset without a shift.
    expect(row.late_minutes).toBeNull();
    expect(row.early_leave_minutes).toBeNull();
    expect(row.overtime_minutes).toBeNull();
  });

  test('U2: reversed pair is rejected and working_minutes stays NULL (not corrupted)', async () => {
    const attendanceId = await insertNoShiftAttendance(empU.id, utcDateOnly(-2), null);

    const res = await ctx.put(`/attendance/${attendanceId}`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        checkInTime: `${Y}T10:00:00Z`,
        checkOutTime: `${Y}T09:00:00Z`
      }
    });
    expect(res.ok(), 'reversed check-in/check-out must be rejected').toBeFalsy();

    const row = (await db.query(
      `SELECT check_in_time, check_out_time, working_minutes
       FROM "Attendances" WHERE id=$1`, [attendanceId])).rows[0];
    expect(row.working_minutes).toBeNull();
    expect(row.check_out_time).toBeNull();
  });

  test('C1: POST /attendance/check-out on a NO-SHIFT record computes working minutes', async () => {
    // Check-out handler resolves the attendance by employee + TODAY.
    const now = Date.now();
    const checkIn = new Date(Math.max(new Date(`${T}T00:00:30Z`).getTime(), now - 4 * 3_600_000));
    const checkOut = new Date(now - 60_000);
    const expected = Math.floor((checkOut.getTime() - checkIn.getTime()) / 60_000);
    expect(expected).toBeGreaterThan(0);

    const attendanceId = await insertNoShiftAttendance(empC.id, T, checkIn.toISOString());

    const res = await ctx.post('/attendance/check-out', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        employeeId: empC.id,
        attendanceId,
        checkOutTime: checkOut.toISOString(),
        cardNo: 'E2E10',
        name: 'E2E Feature10'
      }
    });
    expect(res.status(), await res.text()).toBe(200);
    const body = await res.json();
    expect(body.workingMinutes ?? body.data?.workingMinutes).toBe(expected);

    const row = (await db.query(
      `SELECT working_minutes FROM "Attendances" WHERE id=$1`, [attendanceId])).rows[0];
    expect(row.working_minutes).toBe(expected);
  });

  test('B1: biometric batch job fills working minutes for a NO-SHIFT employee', async () => {
    test.setTimeout(480_000); // recurring job fires every 5 min if the manual trigger is refused

    const attendanceId = await insertNoShiftAttendance(empB.id, T, null);

    // Same-UTC-day punches in the past (the job only reads today's logs).
    const now = Date.now();
    const outAt = new Date(now - 120_000);
    const inAt = new Date(Math.max(new Date(`${T}T00:00:30Z`).getTime(), outAt.getTime() - 3 * 3_600_000));
    const expected = Math.floor((outAt.getTime() - inAt.getTime()) / 60_000);
    expect(expected).toBeGreaterThan(0);

    await insertLog(empB.empId, inAt.toISOString(), '1', T);
    await insertLog(empB.empId, outAt.toISOString(), '2', T);

    // Best effort — on this branch the Hangfire dashboard may refuse the POST
    // (antiforgery); the recurring job fires every 5 minutes regardless.
    await ctx.post('/hangfire/recurring/trigger', {
      form: { 'jobs[]': 'create-attendance-records' }
    }).catch(() => {});

    let row: any = null;
    await expect.poll(async () => {
      row = (await db.query(
        `SELECT check_in_time, check_out_time, working_minutes, status
         FROM "Attendances" WHERE id=$1`, [attendanceId])).rows[0];
      return row.working_minutes !== null;
    }, { timeout: 420_000, intervals: [5_000] }).toBeTruthy();

    expect(new Date(row.check_in_time).toISOString()).toBe(inAt.toISOString());
    expect(new Date(row.check_out_time).toISOString()).toBe(outAt.toISOString());
    expect(row.working_minutes).toBe(expected); // no shift, yet real elapsed minutes
    expect(row.status).toBe('Present');
  });
});
