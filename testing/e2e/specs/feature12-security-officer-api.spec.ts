import {
  test,
  expect,
  request as pwRequest,
  type APIRequestContext
} from '@playwright/test';
import { Client } from 'pg';
import { pgClient, scalar } from '../fixtures/db';

/**
 * FEATURE 12 — View-only interface for security officers (واجهة خاصة لضباط الامن).
 *
 * Source of truth (Arabic): "واجهة خاصة لضباط الامن للمتابعة، عرض فقط، ولا يستطيع
 * انشاء موقف." — a monitoring interface, VIEW ONLY, and they cannot create a موقف
 * (a leave record; the create button reads "إضافة موقف جديد").
 *
 * This spec proves the backend authorization boundary — the real enforcement
 * behind the frontend visibility gate. A fresh SecurityOfficer (Role = 11) user
 * is created via the API as admin (self-contained, deleted in afterAll):
 *   - reads (monitoring GETs)          ⇒ allowed (not 401/403; 200 for param-free)
 *   - POST /leaves (the موقف-create)   ⇒ 403   <-- the headline requirement
 *   - every other write (PUT/DELETE/POST) ⇒ 403 (view-only backstop)
 *   - admin-only reads                 ⇒ 403 (least privilege)
 *   - DB: the blocked POST /leaves creates NO new row
 *
 * Run isolated from the browser auth.setup with --no-deps.
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

const ROLE_SECURITY_OFFICER = 11; // Domain.Enums.Role.SecurityOfficer

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

test.describe.serial('feature 12 — security officer is view-only (API)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  let adminToken = '';
  let secToken = '';
  let secUserId = '';

  const stamp = Date.now();
  const secLogin = `e2e_secoff_${stamp}`;
  const secPassword = 'Security@123456';

  const auth = (token: string) => ({ Authorization: `Bearer ${token}` });

  // A minimal leave payload — invalid on purpose. For a write-capable role it
  // fails validation (NOT 403); for the security officer it is blocked at 403
  // BEFORE the handler runs, which is exactly the boundary under test.
  const leavePayload = {
    employeeId: '00000000-0000-0000-0000-000000000000',
    leaveType: 1,
    startDate: '2026-06-20T00:00:00Z',
    endDate: '2026-06-21T00:00:00Z',
    reason: 'e2e view-only probe'
  };

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({
      baseURL: API,
      ignoreHTTPSErrors: true,
      ...(BURP ? { proxy: { server: BURP } } : {})
    });
    db = pgClient();
    await db.connect();

    adminToken = await login(ctx, 'seed.admin', 'Admin@123456');

    // Need a real OrganizationalUnitId to create the fixture user.
    const usersRes = await ctx.get('/users?page=1&pageSize=50', { headers: auth(adminToken) });
    expect(usersRes.ok(), 'GET /users should load for admin').toBeTruthy();
    const users = (await usersRes.json()).data as Array<{ organizationalUnitId?: string }>;
    const orgUnitId = users.find((u) => u.organizationalUnitId)?.organizationalUnitId;
    expect(orgUnitId, 'an organizational unit id is required to create the fixture user').toBeTruthy();

    const create = await ctx.post('/users/new', {
      headers: auth(adminToken),
      data: {
        username: `E2E SecOfficer ${stamp}`,
        userLogin: secLogin,
        password: secPassword,
        confirmPassword: secPassword,
        role: ROLE_SECURITY_OFFICER,
        organizationalUnitId: orgUnitId
      }
    });
    expect(create.ok(), `creating SecurityOfficer should succeed (${create.status()})`).toBeTruthy();
    const created = (await create.json()).data;
    secUserId = created.userId as string;
    expect(created.role, 'created user should carry role 11').toBe(ROLE_SECURITY_OFFICER);

    secToken = await login(ctx, secLogin, secPassword);
  });

  test('JWT carries the SecurityOfficer role claim', () => {
    expect(roleClaim(secToken)).toBe('SecurityOfficer');
  });

  test('monitoring reads are allowed (not forbidden)', async () => {
    // Param-free reads return 200 outright.
    for (const ep of [
      '/users/me',
      '/attendance?page=1&pageSize=5',
      '/employees?page=1&pageSize=5',
      '/leaves?page=1&pageSize=5',
      '/organizational-units',
      '/organizational-units/tree',
      '/shifts'
    ]) {
      const res = await ctx.get(ep, { headers: auth(secToken) });
      expect(res.status(), `GET ${ep} should be allowed for security officer`).toBe(200);
    }
  });

  test('POST /leaves (إنشاء موقف) is forbidden ⇒ 403', async () => {
    const sec = await ctx.post('/leaves', { headers: auth(secToken), data: leavePayload });
    expect(sec.status(), 'security officer must NOT create a موقف').toBe(403);

    // Cross-check: the SAME payload as admin is NOT a 403 — it reaches the
    // handler (and fails validation), proving the difference is authorization,
    // not the payload.
    const admin = await ctx.post('/leaves', { headers: auth(adminToken), data: leavePayload });
    expect(admin.status(), 'admin must pass the auth gate (any status but 403)').not.toBe(403);
  });

  test('all other writes are forbidden ⇒ 403', async () => {
    // Grab a real leave id (read-only) for the PUT/DELETE probes, if any exist.
    const leaves = await ctx.get('/leaves?page=1&pageSize=1', { headers: auth(secToken) });
    const leaveId = (await leaves.json())?.data?.[0]?.id as string | undefined;

    const writes: Array<() => Promise<number>> = [
      async () => (await ctx.post('/attendance', { headers: auth(secToken), data: {} })).status(),
      async () => (await ctx.post('/employees', { headers: auth(secToken), data: {} })).status(),
      async () => (await ctx.post('/organizational-units', { headers: auth(secToken), data: {} })).status(),
      async () => (await ctx.post('/shifts', { headers: auth(secToken), data: {} })).status()
    ];
    if (leaveId) {
      writes.push(
        async () => (await ctx.put(`/leaves/${leaveId}`, { headers: auth(secToken), data: {} })).status(),
        async () => (await ctx.delete(`/leaves/${leaveId}`, { headers: auth(secToken) })).status()
      );
    }

    for (const probe of writes) {
      expect(await probe(), 'a write by a security officer must be forbidden').toBe(403);
    }
  });

  test('admin-only reads AND all reports remain forbidden ⇒ 403 (least privilege)', async () => {
    for (const ep of [
      '/dashboard/stats',
      '/dashboard/quick-stats',
      '/users?page=1&pageSize=5',
      // Security officers have NO report access whatsoever.
      '/reports/employee',
      '/reports/attendance-summary',
      '/reports/organization',
      '/reports/organizational-summary'
    ]) {
      const res = await ctx.get(ep, { headers: auth(secToken) });
      expect(res.status(), `GET ${ep} must stay restricted for security officer`).toBe(403);
    }
  });

  test('blocked POST /leaves creates NO new leave row (DB ground truth)', async () => {
    const before = Number(await scalar(db, 'SELECT COUNT(*) FROM public."Leaves"'));
    const res = await ctx.post('/leaves', { headers: auth(secToken), data: leavePayload });
    expect(res.status()).toBe(403);
    const after = Number(await scalar(db, 'SELECT COUNT(*) FROM public."Leaves"'));
    expect(after, 'no leave row may be created by a forbidden request').toBe(before);
  });

  test.afterAll(async () => {
    if (secUserId) {
      await ctx.delete(`/users/${secUserId}`, { headers: auth(adminToken) }).catch(() => {});
    }
    await db.end().catch(() => {});
    await ctx.dispose();
  });
});
