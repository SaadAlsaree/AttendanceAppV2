import {
  test,
  expect,
  request as pwRequest,
  type APIRequestContext
} from '@playwright/test';
import { ShellPage } from '../pages/shell.page';
import { LoginPage } from '../pages/login.page';
import { ROUTES, API_URL, CREDENTIALS } from '../fixtures/test-data';

/**
 * FEATURE 12 — security officer view-only interface (UI gating).
 *
 * A freshly-created SecurityOfficer (Role = 11) logs in through a real browser
 * and the spec asserts the view-only UX:
 *   - sidebar shows monitoring sections (إدارة المواقف) and HIDES System Settings
 *     (إعدادات النظام).
 *   - /leave renders the table but the "إضافة موقف جديد" create button is absent.
 *   - a leave row's action menu shows "عرض التفاصيل" but NOT "تعديل".
 *   - deep-links to /leave/new and /leave/[id]/edit are redirected to /leave.
 *   - /employee renders but the "إضافة موظف جديد" button is absent.
 *
 * The authoritative security boundary (403 on writes) lives in the API spec;
 * this guards against the client gate accidentally showing write affordances.
 * Starts logged out and logs in via the UI (no stored auth for this role).
 */

const ADD_LEAVE = 'إضافة موقف جديد';
const ADD_EMPLOYEE = 'إضافة موظف جديد';
const SYSTEM_SETTINGS = 'إعدادات النظام';
const REPORTS = 'التقارير';
const REPORTS_ANALYTICS = 'التقارير والتحليلات';
const LEAVE_MGMT = 'إدارة المواقف';
const VIEW_DETAILS = 'عرض التفاصيل';
const EDIT = 'تعديل';

test.describe.serial('feature 12 — security officer view-only (UI)', () => {
  test.use({ storageState: { cookies: [], origins: [] } }); // start logged out

  let ctx: APIRequestContext;
  let adminToken = '';
  let secUserId = '';
  let anyLeaveId: string | undefined;

  const stamp = Date.now();
  const secLogin = `e2e_secoff_ui_${stamp}`;
  const secPassword = 'Security@123456';

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
        username: `E2E SecOfficer UI ${stamp}`,
        userLogin: secLogin,
        password: secPassword,
        confirmPassword: secPassword,
        role: 11, // SecurityOfficer
        organizationalUnitId: orgUnitId
      }
    });
    secUserId = (await create.json()).data.userId as string;

    const leaves = await ctx.get('/leaves?page=1&pageSize=1', { headers: auth });
    anyLeaveId = (await leaves.json())?.data?.[0]?.id as string | undefined;
  });

  test('sidebar, leave page and employee page hide all write affordances', async ({ page }) => {
    await new LoginPage(page).goto();
    await new LoginPage(page).loginAndWait(secLogin, secPassword);

    const shell = new ShellPage(page);

    // Sidebar: monitoring visible; System Settings AND all Reports hidden.
    await expect(page.getByText(LEAVE_MGMT).first()).toBeVisible();
    await expect(page.getByText(SYSTEM_SETTINGS)).toHaveCount(0);
    await expect(page.getByText(REPORTS_ANALYTICS)).toHaveCount(0);
    await expect(page.getByText(REPORTS, { exact: true })).toHaveCount(0);

    // Leave listing: table renders, but no "إضافة موقف جديد" create control.
    await shell.goto(ROUTES.leave);
    await shell.expectTableOrEmptyState();
    await expect(page.getByRole('link', { name: ADD_LEAVE })).toHaveCount(0);
    await expect(page.getByText(ADD_LEAVE)).toHaveCount(0);

    // Row action menu (if any rows): "عرض التفاصيل" present, "تعديل" hidden.
    if ((await shell.rowCount()) > 0) {
      const firstRow = page.locator('table tbody tr').first();
      const trigger = firstRow.getByRole('button', { name: /فتح القائمة/ });
      // The leave table streams in via Suspense; rows can shift right after first
      // paint and dismiss the dropdown. Settle on a stable row, then open the
      // menu, retrying until the portal's menu items are actually present.
      await expect(trigger).toBeVisible();
      const menu = page.getByRole('menu');
      const viewItem = menu.getByRole('menuitem', { name: VIEW_DETAILS });
      await expect(async () => {
        await trigger.click();
        await expect(viewItem).toBeVisible({ timeout: 2000 });
      }).toPass({ timeout: 20000 });
      await expect(menu.getByRole('menuitem', { name: EDIT })).toHaveCount(0);

      // Leave DETAIL page must expose no write buttons (edit/approve/reject/cancel).
      await viewItem.click();
      await expect(page).toHaveURL(/\/leave\/[^/]+$/);
      await expect(
        page.getByRole('heading', { name: 'تفاصيل الموقف' })
      ).toBeVisible();
      for (const label of [EDIT, 'موافقة', 'رفض', 'إلغاء']) {
        await expect(page.getByRole('button', { name: label })).toHaveCount(0);
      }
    }

    // Employee listing: reachable for viewing, but no add button.
    await shell.goto(ROUTES.employees);
    await shell.expectTableOrEmptyState();
    await expect(page.getByRole('link', { name: ADD_EMPLOYEE })).toHaveCount(0);

    // Employee table row menu: "عرض" present, "تعديل" hidden.
    if ((await shell.rowCount()) > 0) {
      const trigger = page
        .locator('table tbody tr')
        .first()
        .getByRole('button', { name: /فتح القائمة/ });
      const menu = page.getByRole('menu');
      await expect(async () => {
        await trigger.click();
        await expect(menu.getByRole('menuitem', { name: 'عرض' })).toBeVisible({
          timeout: 2000
        });
      }).toPass({ timeout: 20000 });
      await expect(menu.getByRole('menuitem', { name: EDIT })).toHaveCount(0);
    }
  });

  test('monitoring screens shown in the sidebar actually open (no redirect to /)', async ({ page }) => {
    await new LoginPage(page).goto();
    await new LoginPage(page).loginAndWait(secLogin, secPassword);

    // These are in the officer's sidebar; their page guard must allow viewing.
    for (const route of [
      ROUTES.attendanceViewAll,
      ROUTES.attendanceNot,
      ROUTES.attendanceLogs
    ]) {
      await page.goto(route, { waitUntil: 'domcontentloaded' });
      await expect(page).toHaveURL(new RegExp(route.replace(/\//g, '\\/')));
    }

    // Reports are NOT for security officers: the page guards must redirect away.
    for (const route of [ROUTES.reportsOrganizational, ROUTES.reportsSummary]) {
      await page.goto(route, { waitUntil: 'domcontentloaded' });
      await expect(page, `report ${route} must redirect the officer away`).not.toHaveURL(
        new RegExp(route.replace(/\//g, '\\/'))
      );
    }
  });

  test('deep-links to create/edit leave routes are redirected to /leave', async ({ page }) => {
    await new LoginPage(page).goto();
    await new LoginPage(page).loginAndWait(secLogin, secPassword);

    // /leave/new → server guard redirects view-only users to /leave.
    await page.goto(ROUTES.leaveNew, { waitUntil: 'domcontentloaded' });
    await expect(page).toHaveURL(/\/leave(\?|$)/);
    await expect(page).not.toHaveURL(/\/leave\/new/);

    // /leave/[id]/edit → same guard.
    if (anyLeaveId) {
      await page.goto(`/leave/${anyLeaveId}/edit`, { waitUntil: 'domcontentloaded' });
      await expect(page).toHaveURL(/\/leave(\?|$)/);
      await expect(page).not.toHaveURL(/\/edit/);
    }
  });

  test.afterAll(async () => {
    if (secUserId) {
      await ctx
        .delete(`/users/${secUserId}`, { headers: { Authorization: `Bearer ${adminToken}` } })
        .catch(() => {});
    }
    await ctx.dispose();
  });
});
