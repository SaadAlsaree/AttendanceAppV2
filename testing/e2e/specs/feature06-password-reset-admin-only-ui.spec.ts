import {
  test,
  expect,
  request as pwRequest,
  type APIRequestContext,
  type Page
} from '@playwright/test';
import { ShellPage } from '../pages/shell.page';
import { LoginPage } from '../pages/login.page';
import { ROUTES, STORAGE_STATE, API_URL, CREDENTIALS } from '../fixtures/test-data';

/**
 * FEATURE 06 — Password reset, admin only (UI gating).
 *
 * The reset action in the user-management table dropdown is now gated to
 * Admin / SuperAdmin via the client-side `useCurrentUser()` role check
 * (row-actions.tsx). These specs assert the *positive* paths in a real browser:
 *   - Admin  → the page loads and a non-self row's menu shows "إعادة تعيين كلمة المرور".
 *   - SuperAdmin → the page loads (NOT redirected — SuperAdmin was previously
 *     excluded from the page gate) and the reset action is likewise visible.
 *
 * The negative path (a non-admin is forbidden) is enforced/verified at the API
 * layer in feature06-password-reset-admin-only-api.spec.ts (Manager ⇒ 403),
 * which is the real security boundary; the UI check here guards against the
 * client gate accidentally hiding the action from admins.
 */

const RESET = 'إعادة تعيين كلمة المرور';
const CHANGE = 'تغيير كلمة المرور';

/** Open the actions dropdown for the first table row and return the open menu. */
async function openFirstRowMenu(page: Page) {
  const firstRow = page.locator('table tbody tr').first();
  await expect(firstRow).toBeVisible({ timeout: 30_000 });
  await firstRow.getByRole('button', { name: /فتح القائمة/ }).click();
  return page.getByRole('menu');
}

test.describe('feature 06 — reset action is gated to admins (UI)', () => {
  test('Admin: users page loads and a non-self row exposes the reset action', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.usersPermissions);
    await expect(page).toHaveURL(/users-permissions/);
    await shell.expectTableOrEmptyState();

    // Row 1 is another user (not the logged-in admin), so it must offer reset.
    const menu = await openFirstRowMenu(page);
    await expect(menu.getByRole('menuitem', { name: RESET })).toBeVisible();
  });

  test.describe('as SuperAdmin', () => {
    test.use({ storageState: STORAGE_STATE.super });

    test('SuperAdmin: user-management page is reachable (no longer redirected)', async ({ page }) => {
      const shell = new ShellPage(page);
      await shell.goto(ROUTES.usersPermissions);
      // Feature 06 added SuperAdmin to the page gate; previously [Admin, Manager]
      // bounced SuperAdmins to '/'. Staying on the route proves the gate fix.
      await expect(page).toHaveURL(/users-permissions/);
      await shell.expectTableOrEmptyState();
      // NOTE: GET /users is scoped to the caller's organizational unit, and
      // seed.super matches no unit, so the table is empty for this user — a
      // pre-existing data-scoping quirk, unrelated to feature 06. SuperAdmin's
      // reset privilege itself is proven in the API spec (super ⇒ 204).
    });
  });

  // Negative path in a REAL browser: a Manager (non-admin) must NOT see the
  // reset action. Defense is layered — (1) GET /users is itself Admin/SuperAdmin
  // only, so the table is empty for a Manager; (2) the row-actions `canReset`
  // gate hides the item regardless; (3) the API returns 403 (api spec). This
  // test logs in through the UI as a freshly-created Manager and asserts the
  // reset label appears nowhere on the user-management page.
  test.describe('as a non-admin (Manager)', () => {
    test.use({ storageState: { cookies: [], origins: [] } }); // start logged out

    let ctx: APIRequestContext;
    let adminToken = '';
    let managerId = '';
    const stamp = Date.now();
    const managerLogin = `e2e_neg_mgr_${stamp}`;
    const managerPassword = 'Temp@123456';

    test.beforeAll(async () => {
      ctx = await pwRequest.newContext({ baseURL: API_URL, ignoreHTTPSErrors: true });
      const login = await ctx.post('/auth/login', {
        data: { userLogin: CREDENTIALS.admin.login, password: CREDENTIALS.admin.password }
      });
      adminToken = (await login.json()).data.token;
      const auth = { Authorization: `Bearer ${adminToken}` };

      const usersRes = await ctx.get('/users?page=1&pageSize=50', { headers: auth });
      const users = (await usersRes.json()).data as Array<{ organizationalUnitId?: string }>;
      const orgUnitId = users.find((u) => u.organizationalUnitId)?.organizationalUnitId;

      const create = await ctx.post('/users/new', {
        headers: auth,
        data: {
          username: `E2E Neg Manager ${stamp}`,
          userLogin: managerLogin,
          password: managerPassword,
          confirmPassword: managerPassword,
          role: 3, // Manager
          organizationalUnitId: orgUnitId
        }
      });
      managerId = (await create.json()).data.userId as string;
    });

    test('Manager sees NO reset action on the user-management page', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAndWait(managerLogin, managerPassword);

      const shell = new ShellPage(page);
      await shell.goto(ROUTES.usersPermissions);
      await expect(page).toHaveURL(/users-permissions/); // Manager is allowed on the page

      // The reset action label must not appear anywhere (menu items or buttons).
      await expect(page.getByText(RESET)).toHaveCount(0);
    });

    test.afterAll(async () => {
      if (managerId) {
        await ctx
          .delete(`/users/${managerId}`, { headers: { Authorization: `Bearer ${adminToken}` } })
          .catch(() => {});
      }
      await ctx.dispose();
    });
  });

  // END-TO-END: actually drive the reset dialog (open → fill → submit) as an
  // admin and confirm the password really changes. This exercises the submit
  // path onSubmit → resetPasswordClient that the visibility checks above never
  // touched — it is the test that catches "wrong service method" / dialog wiring
  // regressions, not just whether the button is shown.
  test.describe('admin completes a reset through the dialog (e2e)', () => {
    let ctx: APIRequestContext;
    let adminToken = '';
    let targetId = '';
    const stamp = Date.now();
    const targetLogin = `e2e_reset_target_${stamp}`;
    const oldPassword = 'Temp@123456';
    const newPassword = 'NewPass@789012';

    test.beforeAll(async () => {
      ctx = await pwRequest.newContext({ baseURL: API_URL, ignoreHTTPSErrors: true });
      const login = await ctx.post('/auth/login', {
        data: { userLogin: CREDENTIALS.admin.login, password: CREDENTIALS.admin.password }
      });
      adminToken = (await login.json()).data.token;
      const auth = { Authorization: `Bearer ${adminToken}` };

      const usersRes = await ctx.get('/users?page=1&pageSize=50', { headers: auth });
      const users = (await usersRes.json()).data as Array<{ organizationalUnitId?: string }>;
      const orgUnitId = users.find((u) => u.organizationalUnitId)?.organizationalUnitId;

      const create = await ctx.post('/users/new', {
        headers: auth,
        data: {
          username: `E2E Reset Target ${stamp}`,
          userLogin: targetLogin,
          password: oldPassword,
          confirmPassword: oldPassword,
          role: 4, // Employee — an ordinary reset target
          organizationalUnitId: orgUnitId
        }
      });
      targetId = (await create.json()).data.userId as string;
    });

    test('reset dialog submits and the new password actually works', async ({ page }) => {
      // Deep-link to the target user's detail view (admin storageState by default).
      await new ShellPage(page).goto(`${ROUTES.usersPermissions}/${targetId}`);

      // Open the reset dialog from the detail-view action button.
      await page.getByRole('button', { name: RESET }).click();
      const dialog = page.getByRole('dialog');
      await expect(dialog).toBeVisible();

      // Fill both fields and submit — this is the path that hits resetPasswordClient.
      await dialog.getByPlaceholder('أدخل كلمة المرور الجديدة').fill(newPassword);
      await dialog.getByPlaceholder('أعد إدخال كلمة المرور الجديدة').fill(newPassword);
      await dialog.getByRole('button', { name: RESET }).click();

      // Success toast confirms the client call succeeded (no "headers outside
      // request scope" — that bug would surface the error toast instead).
      await expect(page.getByText('تم إعادة تعيين كلمة المرور بنجاح')).toBeVisible({ timeout: 15_000 });

      // Ground truth: the NEW password now logs in, the OLD one no longer does.
      const withNew = await ctx.post('/auth/login', {
        data: { userLogin: targetLogin, password: newPassword }
      });
      expect(withNew.ok(), 'new password should authenticate').toBeTruthy();
      const withOld = await ctx.post('/auth/login', {
        data: { userLogin: targetLogin, password: oldPassword }
      });
      expect(withOld.ok(), 'old password should no longer authenticate').toBeFalsy();
    });

    test.afterAll(async () => {
      if (targetId) {
        await ctx
          .delete(`/users/${targetId}`, { headers: { Authorization: `Bearer ${adminToken}` } })
          .catch(() => {});
      }
      await ctx.dispose();
    });
  });
});
