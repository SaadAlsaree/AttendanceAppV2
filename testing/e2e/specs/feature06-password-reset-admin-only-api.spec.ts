import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';

/**
 * FEATURE 06 — Password reset, admin only (عمل ريستارت للباسورد للادمن فقط).
 *
 * Source of truth (Arabic): only an Admin / SuperAdmin may PERFORM a password
 * reset on another user. This spec proves the backend authorization boundary —
 * the actual security enforcement behind the frontend visibility gate:
 *   - seed.admin  (Admin)      → POST /users/reset-password ⇒ 204 (allowed)
 *   - seed.super  (SuperAdmin) → POST /users/reset-password ⇒ 204 (allowed)
 *   - a Manager (non-admin)    → POST /users/reset-password ⇒ 403 (forbidden)
 *
 * The non-admin is created via the API (POST /users/new as admin) so the suite
 * is self-contained; it is the reset target throughout and is deleted in
 * afterAll. Run isolated from the browser auth.setup with --no-deps.
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

const ROLE_MANAGER = 3; // Domain.Enums.Role.Manager — a non-admin that must be blocked.

async function login(ctx: APIRequestContext, userLogin: string, password: string) {
  const res = await ctx.post('/auth/login', { data: { userLogin, password } });
  expect(res.ok(), `login ${userLogin} should succeed (${res.status()})`).toBeTruthy();
  return (await res.json()).data.token as string;
}

test.describe.serial('feature 06 — password reset is admin-only (API)', () => {
  let ctx: APIRequestContext;
  let adminToken = '';
  let superToken = '';
  let managerToken = '';
  let tempUserId = '';

  const stamp = Date.now();
  // userLogin allows only letters, numbers and underscores (backend validation).
  const tempLogin = `e2e_pwreset_${stamp}`;
  const tempPassword = 'Temp@123456';

  const auth = (token: string) => ({ Authorization: `Bearer ${token}` });

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({
      baseURL: API,
      ignoreHTTPSErrors: true,
      ...(BURP ? { proxy: { server: BURP } } : {})
    });

    adminToken = await login(ctx, 'seed.admin', 'Admin@123456');
    superToken = await login(ctx, 'seed.super', 'Super@123456');

    // Need a real OrganizationalUnitId to create the fixture user.
    const usersRes = await ctx.get('/users?page=1&pageSize=50', { headers: auth(adminToken) });
    expect(usersRes.ok(), 'GET /users should load for admin').toBeTruthy();
    const users = (await usersRes.json()).data as Array<{ organizationalUnitId?: string }>;
    const orgUnitId = users.find((u) => u.organizationalUnitId)?.organizationalUnitId;
    expect(orgUnitId, 'an organizational unit id is required to create the fixture user').toBeTruthy();

    // Create a non-admin (Manager) fixture user via the API.
    const create = await ctx.post('/users/new', {
      headers: auth(adminToken),
      data: {
        username: `E2E PwReset ${stamp}`,
        userLogin: tempLogin,
        password: tempPassword,
        confirmPassword: tempPassword,
        role: ROLE_MANAGER,
        organizationalUnitId: orgUnitId
      }
    });
    expect(create.ok(), `creating fixture user should succeed (${create.status()})`).toBeTruthy();
    tempUserId = (await create.json()).data.userId as string;
    expect(tempUserId, 'fixture user id should be returned').toBeTruthy();

    // Log in AS the fixture user (token stays valid even after its password is reset).
    managerToken = await login(ctx, tempLogin, tempPassword);
  });

  test('Admin can reset another user\'s password ⇒ 204', async () => {
    const res = await ctx.post('/users/reset-password', {
      headers: auth(adminToken),
      data: { userId: tempUserId, newPassword: 'Reset@123456', confirmPassword: 'Reset@123456' }
    });
    expect(res.status(), 'admin reset must be allowed').toBe(204);
  });

  test('SuperAdmin can reset another user\'s password ⇒ 204', async () => {
    const res = await ctx.post('/users/reset-password', {
      headers: auth(superToken),
      data: { userId: tempUserId, newPassword: 'Reset@654321', confirmPassword: 'Reset@654321' }
    });
    expect(res.status(), 'superadmin reset must be allowed').toBe(204);
  });

  test('Non-admin (Manager) is forbidden from resetting a password ⇒ 403', async () => {
    const res = await ctx.post('/users/reset-password', {
      headers: auth(managerToken),
      data: { userId: tempUserId, newPassword: 'Hacker@123456', confirmPassword: 'Hacker@123456' }
    });
    expect(res.status(), 'non-admin reset must be forbidden').toBe(403);
  });

  test.afterAll(async () => {
    if (tempUserId) {
      await ctx.delete(`/users/${tempUserId}`, { headers: auth(adminToken) }).catch(() => {});
    }
    await ctx.dispose();
  });
});
