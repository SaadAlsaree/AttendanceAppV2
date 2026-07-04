import {
  test,
  expect,
  request as pwRequest,
  type APIRequestContext
} from '@playwright/test';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * SECURITY — object-level authorization on PUT /leaves/{id} (IDOR fix).
 *
 * Before the fix, the update handler never checked ownership, so any account
 * allowed on the endpoint (incl. the non-privileged `Employee` role) could edit
 * ANY employee's leave by id — a horizontal IDOR. The handler now requires that
 * non-privileged callers own the leave (Employee.UserId == caller); Admin /
 * SuperAdmin remain able to edit any leave by design.
 *
 * Asserts:
 *   - Employee editing ANOTHER employee's leave  → 403 Leave.UnauthorizedUpdate
 *   - Employee editing THEIR OWN leave           → 200 (and persists)
 *   - Admin editing the same foreign leave       → 200 (privileged bypass intact)
 *
 * Fixtures (no API path to backdate/link) are arranged directly in Postgres via
 * the E2E_PG_* connection in fixtures/db.ts.
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const UNAUTH_CODE = 'Leave.UnauthorizedUpdate';

test.describe.serial('feature 08 — leave update IDOR (object-level authz)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  let adminToken = '';
  let empToken = '';
  let empUserId = '';
  let ownerEmpId = '';
  let foreignEmpId = '';
  let ownedLeaveId = '';
  let foreignLeaveId = '';
  let originalUserId: string | null = null;
  let runBase = 0;

  const empLogin = `e2e_idor_emp_${Date.now()}`;
  const empPassword = 'Temp@123456';

  const dayISO = (offsetDays: number) => {
    const d = new Date(Date.UTC(2034, 0, 1));
    d.setUTCDate(d.getUTCDate() + offsetDays);
    return d.toISOString().slice(0, 19);
  };
  const bearer = (t: string) => ({ Authorization: `Bearer ${t}` });

  async function createLeave(employeeId: string, startOffset: number, reason: string) {
    const res = await ctx.post('/leaves', {
      headers: bearer(adminToken),
      data: {
        employeeId,
        leaveType: 1,
        startDate: dayISO(startOffset),
        endDate: dayISO(startOffset + 1),
        reason
      }
    });
    expect(res.status(), `create leave for ${employeeId}`).toBe(200);
    return String(await res.json()).replace(/"/g, '');
  }

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({ baseURL: API, ignoreHTTPSErrors: true });
    db = pgClient();
    await db.connect();
    runBase = Math.floor(Date.now() / 1000) % 9000;

    const login = await ctx.post('/auth/login', {
      data: { userLogin: 'seed.admin', password: 'Admin@123456' }
    });
    adminToken = (await login.json()).data.token;

    // Two distinct employees: one we'll link to the Employee user, one "foreign".
    const emps = await ctx.get('/employees?page=1&pageSize=2', { headers: bearer(adminToken) });
    const empList = (await emps.json()).data as Array<{ id: string }>;
    ownerEmpId = empList[0].id;
    foreignEmpId = empList[1].id;

    // Create a real Employee-role (role=4) user, in some org unit.
    const usersRes = await ctx.get('/users?page=1&pageSize=50', { headers: bearer(adminToken) });
    const users = (await usersRes.json()).data as Array<{ organizationalUnitId?: string }>;
    const orgUnitId = users.find((u) => u.organizationalUnitId)?.organizationalUnitId;
    const create = await ctx.post('/users/new', {
      headers: bearer(adminToken),
      data: {
        username: `E2E IDOR Emp ${runBase}`,
        userLogin: empLogin,
        password: empPassword,
        confirmPassword: empPassword,
        role: 4, // Employee
        organizationalUnitId: orgUnitId
      }
    });
    empUserId = (await create.json()).data.userId as string;

    // Link the owner employee to that user (capture original to restore later).
    const before = await db.query('SELECT user_id FROM public."Employees" WHERE id = $1', [ownerEmpId]);
    originalUserId = before.rows[0]?.user_id ?? null;
    const upd = await db.query('UPDATE public."Employees" SET user_id = $1 WHERE id = $2', [empUserId, ownerEmpId]);
    expect(upd.rowCount).toBe(1);

    // One leave owned by the linked employee, one owned by a different employee.
    ownedLeaveId = await createLeave(ownerEmpId, runBase + 10, 'idor-owned');
    foreignLeaveId = await createLeave(foreignEmpId, runBase + 20, 'idor-foreign');

    // Log in as the Employee user.
    const empLoginRes = await ctx.post('/auth/login', {
      data: { userLogin: empLogin, password: empPassword }
    });
    expect(empLoginRes.ok(), 'employee login should succeed').toBeTruthy();
    empToken = (await empLoginRes.json()).data.token;
  });

  test('IDOR blocked: Employee cannot edit another employee\'s leave (403)', async () => {
    const res = await ctx.put(`/leaves/${foreignLeaveId}`, {
      headers: bearer(empToken),
      data: { reason: 'idor attempt' }
    });
    expect(res.status(), 'foreign edit must be forbidden').toBe(403);
    const body = await res.json();
    expect(body.title || body.detail).toContain(UNAUTH_CODE);

    // And nothing changed on the foreign leave.
    const get = await ctx.get(`/leaves/${foreignLeaveId}`, { headers: bearer(adminToken) });
    expect((await get.json()).reason).toBe('idor-foreign');
  });

  test('owner allowed: Employee can edit their OWN leave (200, persists)', async () => {
    const res = await ctx.put(`/leaves/${ownedLeaveId}`, {
      headers: bearer(empToken),
      data: { reason: 'owner edit ok' }
    });
    expect(res.status(), 'own edit should be allowed').toBe(200);
    const get = await ctx.get(`/leaves/${ownedLeaveId}`, { headers: bearer(adminToken) });
    expect((await get.json()).reason).toBe('owner edit ok');
  });

  test('privileged bypass intact: Admin can edit the foreign leave (200)', async () => {
    const res = await ctx.put(`/leaves/${foreignLeaveId}`, {
      headers: bearer(adminToken),
      data: { reason: 'admin edit ok' }
    });
    expect(res.status(), 'admin should bypass ownership').toBe(200);
    const get = await ctx.get(`/leaves/${foreignLeaveId}`, { headers: bearer(adminToken) });
    expect((await get.json()).reason).toBe('admin edit ok');
  });

  test.afterAll(async () => {
    for (const id of [ownedLeaveId, foreignLeaveId]) {
      if (id) await ctx.delete(`/leaves/${id}`, { headers: bearer(adminToken) }).catch(() => {});
    }
    // Restore the employee's original user link and remove the test user.
    if (ownerEmpId) {
      await db
        .query('UPDATE public."Employees" SET user_id = $1 WHERE id = $2', [originalUserId, ownerEmpId])
        .catch(() => {});
    }
    if (empUserId) {
      await ctx.delete(`/users/${empUserId}`, { headers: bearer(adminToken) }).catch(() => {});
    }
    await db?.end().catch(() => {});
    await ctx.dispose();
  });
});
