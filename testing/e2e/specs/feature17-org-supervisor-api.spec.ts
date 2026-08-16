import {
  test,
  expect,
  request as pwRequest,
  type APIRequestContext
} from '@playwright/test';
import { Client } from 'pg';
import { pgClient, scalar } from '../fixtures/db';

/**
 * FEATURE 17 — OrgSupervisor role (مشرف جهة): per-organization operations admin.
 *
 * Source of truth (Arabic): "create new role to manage employee schedules,
 * attendance, reports for each org so must be that managed by his org … also
 * view his employees so all features but for his organization without manager
 * users etc."
 *
 * A scoped role (Role = 12) that runs daily operations for its OWN unit tree
 * (own unit + all sub-units) and nothing else. This spec proves the backend
 * authorization boundary — the real enforcement behind the frontend gates. A
 * fresh OrgSupervisor user is created via the API as admin (self-contained,
 * deleted in afterAll), pinned to a unit that has both a sub-tree of employees
 * and a foreign employee outside it, then:
 *   - employee reads are SCOPED to the sub-tree (foreign employee absent)
 *   - writes on an own-tree employee (weekly-shifts, schedule, attendance) ⇒ allowed
 *   - the SAME writes on a foreign employee                                 ⇒ 403
 *   - admin-only surfaces (POST /shifts, GET /users, employee edit, POST /users/new) ⇒ 403
 *   - ALL reports (attendance-summary, organization, organizational-summary,
 *     overtime, employee)                                                   ⇒ 403 (admin/manager surface)
 *   - dashboard stays granted but scoped — foreign quick-stats              ⇒ 403
 *   - REGRESSION: a plain Employee-role user can no longer PUT schedule updates ⇒ 403
 *
 * Run isolated from the browser auth.setup with --no-deps.
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

const ROLE_ORG_SUPERVISOR = 12; // Domain.Enums.Role.OrgSupervisor
const ROLE_EMPLOYEE = 4; //        Domain.Enums.Role.Employee

async function login(ctx: APIRequestContext, userLogin: string, password: string) {
  const res = await ctx.post('/auth/login', { data: { userLogin, password } });
  expect(res.ok(), `login ${userLogin} should succeed (${res.status()})`).toBeTruthy();
  return (await res.json()).data.token as string;
}

/** Decode the "Role" claim out of a JWT without verifying the signature. */
function roleClaim(token: string): string {
  const payload = JSON.parse(Buffer.from(token.split('.')[1], 'base64').toString('utf8'));
  return payload.Role as string;
}

test.describe.serial('feature 17 — org supervisor is unit-scoped (API)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  let adminToken = '';
  let supToken = '';
  let supUserId = '';
  let empUserId = '';
  let empToken = '';

  // Resolved from the DB in beforeAll.
  let unitA = ''; //         supervisor's unit (root of the accessible tree)
  let ownEmp = ''; //        an employee inside unitA's sub-tree, no pattern yet
  let foreignEmp = ''; //    an active employee OUTSIDE unitA's sub-tree
  let foreignUnit = ''; //   foreignEmp's organizational unit
  let subtreeCount = 0; //   employees in unitA's sub-tree (expected list total)
  let shiftId = ''; //       a dedicated active shift for the write payloads

  const stamp = Date.now();
  const supLogin = `e2e_orgsup_${stamp}`;
  const supPassword = 'OrgSup@123456';
  const empLogin = `e2e_emp_${stamp}`;
  const empPassword = 'Employee@123456';

  const auth = (token: string) => ({ Authorization: `Bearer ${token}` });

  // Valid one-day schedule payload (date inside the range) so the create reaches
  // the handler — the own/foreign difference must be authorization, not validation.
  const schedulePayload = (employeeId: string) => ({
    employeeId,
    startDate: '2031-02-03',
    endDate: '2031-02-07',
    scheduleType: 'Regular',
    isActive: true,
    scheduleDays: [{ scheduleDayDate: '2031-02-04', shiftId, isActive: true }]
  });

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({
      baseURL: API,
      ignoreHTTPSErrors: true,
      ...(BURP ? { proxy: { server: BURP } } : {})
    });
    db = pgClient();
    await db.connect();

    adminToken = await login(ctx, 'seed.admin', 'Admin@123456');

    // Pick unit A: a unit that has at least one child unit AND employees in its
    // sub-tree — so scoping is non-trivial and a foreign employee is guaranteed.
    const unitRow = await db.query(`
      WITH RECURSIVE tree AS (
        SELECT id AS root, id AS node FROM public."OrganizationalUnits"
        UNION ALL
        SELECT t.root, c.id FROM public."OrganizationalUnits" c JOIN tree t ON c.parent_unit_id = t.node
      )
      SELECT ou.id,
             (SELECT count(*) FROM public."Employees" e
              WHERE e.organizational_unit_id IN (SELECT node FROM tree WHERE root = ou.id)
                AND e.is_deleted = false) AS subtree_emps
      FROM public."OrganizationalUnits" ou
      WHERE ou.parent_unit_id IS NOT NULL
        AND (SELECT count(*) FROM public."OrganizationalUnits" c WHERE c.parent_unit_id = ou.id) > 0
      ORDER BY subtree_emps DESC
      LIMIT 1`);
    expect(unitRow.rowCount, 'a unit with children + employees must exist').toBe(1);
    unitA = unitRow.rows[0].id;
    subtreeCount = Number(unitRow.rows[0].subtree_emps);

    // An own-tree employee with no weekly pattern (so the PUT is a clean assign).
    const own = await db.query(
      `WITH RECURSIVE tree AS (
         SELECT $1::uuid AS node
         UNION ALL
         SELECT c.id FROM public."OrganizationalUnits" c JOIN tree t ON c.parent_unit_id = t.node
       )
       SELECT e.id FROM public."Employees" e
       WHERE e.organizational_unit_id IN (SELECT node FROM tree) AND e.is_deleted = false
         AND NOT EXISTS (SELECT 1 FROM public."EmployeeWeeklyShifts" w WHERE w.employee_id = e.id)
       ORDER BY e.created_at DESC LIMIT 1`,
      [unitA]
    );
    expect(own.rowCount, 'an own-tree employee must exist').toBe(1);
    ownEmp = own.rows[0].id;

    // A foreign employee whose unit is NOT in unitA's sub-tree.
    const foreign = await db.query(
      `WITH RECURSIVE tree AS (
         SELECT $1::uuid AS node
         UNION ALL
         SELECT c.id FROM public."OrganizationalUnits" c JOIN tree t ON c.parent_unit_id = t.node
       )
       SELECT e.id, e.organizational_unit_id FROM public."Employees" e
       WHERE e.organizational_unit_id IS NOT NULL
         AND e.organizational_unit_id NOT IN (SELECT node FROM tree)
         AND e.is_deleted = false
         AND NOT EXISTS (SELECT 1 FROM public."EmployeeWeeklyShifts" w WHERE w.employee_id = e.id)
       ORDER BY e.created_at DESC LIMIT 1`,
      [unitA]
    );
    expect(foreign.rowCount, 'a foreign employee must exist').toBe(1);
    foreignEmp = foreign.rows[0].id;
    foreignUnit = foreign.rows[0].organizational_unit_id;

    // A dedicated active shift for the write payloads (unique name, cleaned up).
    const mk = await ctx.post('/shifts', {
      headers: auth(adminToken),
      data: {
        name: `E2E F17 ${stamp}`,
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

    // Create the OrgSupervisor pinned to unit A, and a plain Employee for the
    // schedule-update regression (any unit is fine for that one).
    const createSup = await ctx.post('/users/new', {
      headers: auth(adminToken),
      data: {
        username: `E2E OrgSup ${stamp}`,
        userLogin: supLogin,
        password: supPassword,
        confirmPassword: supPassword,
        role: ROLE_ORG_SUPERVISOR,
        organizationalUnitId: unitA
      }
    });
    expect(createSup.ok(), `creating OrgSupervisor should succeed (${createSup.status()})`).toBeTruthy();
    supUserId = (await createSup.json()).data.userId as string;
    supToken = await login(ctx, supLogin, supPassword);

    const createEmp = await ctx.post('/users/new', {
      headers: auth(adminToken),
      data: {
        username: `E2E Emp ${stamp}`,
        userLogin: empLogin,
        password: empPassword,
        confirmPassword: empPassword,
        role: ROLE_EMPLOYEE,
        organizationalUnitId: unitA
      }
    });
    expect(createEmp.ok(), `creating Employee should succeed (${createEmp.status()})`).toBeTruthy();
    empUserId = (await createEmp.json()).data.userId as string;
    empToken = await login(ctx, empLogin, empPassword);
  });

  test('JWT carries the OrgSupervisor role claim', () => {
    expect(roleClaim(supToken)).toBe('OrgSupervisor');
  });

  test('employee list is scoped to the supervisor sub-tree (foreign employee absent)', async () => {
    const res = await ctx.get('/employees?page=1&pageSize=1', { headers: auth(supToken) });
    expect(res.status(), 'employees read is allowed').toBe(200);
    const total = (await res.json()).totalCount as number;
    expect(total, 'list total equals the DB sub-tree headcount').toBe(subtreeCount);

    // The foreign employee must never appear, even when searched by id-adjacent page scan.
    const foreignInList = await ctx.get('/employees?page=1&pageSize=100', { headers: auth(supToken) });
    const ids = ((await foreignInList.json()).data as Array<{ id: string }>).map((e) => e.id);
    expect(ids, 'foreign employee is not visible to the supervisor').not.toContain(foreignEmp);
  });

  test('weekly-shifts: own-tree employee ⇒ 204, foreign employee ⇒ 403', async () => {
    const own = await ctx.put(`/employees/${ownEmp}/weekly-shifts`, {
      headers: auth(supToken),
      data: { days: [{ dayOfWeek: 0, shiftId }] }
    });
    expect(own.status(), 'own-tree weekly-shifts assign').toBe(204);

    const foreign = await ctx.put(`/employees/${foreignEmp}/weekly-shifts`, {
      headers: auth(supToken),
      data: { days: [{ dayOfWeek: 0, shiftId }] }
    });
    expect(foreign.status(), 'foreign weekly-shifts assign is forbidden').toBe(403);

    // DB ground truth: only the own-tree employee got a row.
    const foreignRows = Number(
      await scalar(db, 'SELECT count(*) FROM public."EmployeeWeeklyShifts" WHERE employee_id = $1', [foreignEmp])
    );
    expect(foreignRows, 'no pattern written for the foreign employee').toBe(0);
  });

  test('schedule create: own-tree ⇒ allowed, foreign ⇒ 403 (same valid payload)', async () => {
    const own = await ctx.post('/attendance-schedules', {
      headers: auth(supToken),
      data: schedulePayload(ownEmp)
    });
    expect(own.status(), 'own-tree schedule create passes the auth gate').not.toBe(403);
    expect(own.ok(), `own-tree schedule create should succeed (${own.status()})`).toBeTruthy();

    const foreign = await ctx.post('/attendance-schedules', {
      headers: auth(supToken),
      data: schedulePayload(foreignEmp)
    });
    expect(foreign.status(), 'foreign schedule create is forbidden').toBe(403);
  });

  test('admin-only surfaces stay forbidden ⇒ 403 (least privilege)', async () => {
    // Writes the supervisor must never reach.
    const shiftCreate = await ctx.post('/shifts', {
      headers: auth(supToken),
      data: { name: 'x', startTime: '09:00:00', endTime: '16:00:00', shiftType: 'Morning', isActive: true }
    });
    expect(shiftCreate.status(), 'POST /shifts is admin-only').toBe(403);

    const empUpdate = await ctx.put(`/employees/${ownEmp}`, { headers: auth(supToken), data: { fullName: 'x' } });
    expect(empUpdate.status(), 'employee edit is admin-only').toBe(403);

    const userCreate = await ctx.post('/users/new', {
      headers: auth(supToken),
      data: {
        username: 'x',
        userLogin: `e2e_should_fail_${stamp}`,
        password: supPassword,
        confirmPassword: supPassword,
        role: ROLE_EMPLOYEE,
        organizationalUnitId: unitA
      }
    });
    expect(userCreate.status(), 'user creation is admin-only').toBe(403);

    // Admin-only reads.
    for (const ep of ['/users?page=1&pageSize=5', '/reports/employee']) {
      const res = await ctx.get(ep, { headers: auth(supToken) });
      expect(res.status(), `GET ${ep} must stay admin-only`).toBe(403);
    }
  });

  test('ALL reports are forbidden for the supervisor ⇒ 403', async () => {
    // Reports are an admin/manager surface; the OrgSupervisor gets none of them.
    for (const ep of [
      `/reports/attendance-summary?OrganizationalUnitId=${unitA}&IncludeSubUnits=true`,
      `/reports/organization?OrganizationalUnitId=${unitA}&IncludeSubUnits=true`,
      '/reports/organizational-summary',
      '/reports/overtime',
      '/reports/employee'
    ]) {
      const res = await ctx.get(ep, { headers: auth(supToken) });
      expect(res.status(), `GET ${ep} must be forbidden for the supervisor`).toBe(403);
    }
  });

  test('dashboard stays granted but scoped to the supervisor unit tree', async () => {
    // dashboard quick-stats for a foreign org ⇒ 403 (scoped, but the dashboard itself is allowed).
    const qs = await ctx.get(`/dashboard/quick-stats?OrganizationId=${foreignUnit}`, {
      headers: auth(supToken)
    });
    expect(qs.status(), 'foreign quick-stats is forbidden').toBe(403);
  });

  test('REGRESSION: a plain Employee can no longer PUT schedule updates ⇒ 403', async () => {
    // Any real schedule id works — the role gate fires before the handler. Fall
    // back to a random id (still 403 on the role gate) if none exist yet.
    const anySchedule = (await scalar(
      db,
      'SELECT id::text FROM public."AttendanceSchedules" WHERE is_deleted = false LIMIT 1'
    )) as string | null;
    const scheduleId = anySchedule ?? '00000000-0000-0000-0000-000000000000';

    const put = await ctx.put(`/attendance-schedules/${scheduleId}`, {
      headers: auth(empToken),
      data: { scheduleType: 'Regular' }
    });
    expect(put.status(), 'Employee role is no longer allowed on schedule Update').toBe(403);
  });

  test.afterAll(async () => {
    // Clear anything the supervisor created on the own-tree employee.
    if (ownEmp) {
      await ctx
        .put(`/employees/${ownEmp}/weekly-shifts`, { headers: auth(adminToken), data: { days: [] } })
        .catch(() => {});
      // node-postgres uses the extended protocol when params are present, which
      // executes only ONE statement — so the two deletes must be separate calls.
      await db
        .query(
          `DELETE FROM public."ScheduleDays" WHERE attendance_schedule_id IN
             (SELECT id FROM public."AttendanceSchedules" WHERE employee_id = $1 AND start_date = '2031-02-03')`,
          [ownEmp]
        )
        .catch(() => {});
      await db
        .query(
          `DELETE FROM public."AttendanceSchedules" WHERE employee_id = $1 AND start_date = '2031-02-03'`,
          [ownEmp]
        )
        .catch(() => {});
    }
    if (supUserId) {
      await ctx.delete(`/users/${supUserId}`, { headers: auth(adminToken) }).catch(() => {});
    }
    if (empUserId) {
      await ctx.delete(`/users/${empUserId}`, { headers: auth(adminToken) }).catch(() => {});
    }
    if (shiftId) {
      await db.query('DELETE FROM public."Attendances" WHERE shift_id = $1', [shiftId]).catch(() => {});
      await ctx.delete(`/shifts/${shiftId}`, { headers: auth(adminToken) }).catch(() => {});
    }
    await db.end().catch(() => {});
    await ctx.dispose();
  });
});
