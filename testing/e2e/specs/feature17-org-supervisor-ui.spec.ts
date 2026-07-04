import {
  test,
  expect,
  request as pwRequest,
  type APIRequestContext
} from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { ShellPage } from '../pages/shell.page';
import { ROUTES, API_URL, CREDENTIALS } from '../fixtures/test-data';

/**
 * FEATURE 17 — OrgSupervisor role (مشرف جهة) — UI gating.
 *
 * A freshly-created OrgSupervisor (Role = 12), pinned to a real unit with
 * employees, logs in through a real browser. The spec asserts the scoped-admin
 * UX behind the client gates:
 *   - sidebar shows the granted sections (الحضور، الموظفين، الجداول، المواقف،
 *     التقارير) and HIDES System Settings (إعدادات النظام).
 *   - the scoped WRITE screens open (no «غير مصرح»): /schedule/assign-shifts and
 *     /schedule/create-schedule render their employee picker.
 *   - monitoring screens open without redirect: attendance, employees, reports.
 *   - the admin-only /system/users-permissions deep-link redirects to '/'.
 *
 * The authoritative security boundary (403 on writes, unit scoping) lives in the
 * API spec; this guards against the client gate hiding a screen the role should
 * have, or exposing one it should not. Starts logged out; logs in via the UI.
 */

const SYSTEM_SETTINGS = 'إعدادات النظام';
const ATTENDANCE_MGMT = 'إدارة الحضور';
const EMPLOYEE_MGMT = 'إدارة الموظفين';
const SCHEDULE_MGMT = 'إدارة الجداول';
const LEAVE_MGMT = 'إدارة المواقف';
const REPORTS = 'التقارير';
const REPORTS_ANALYTICS = 'التقارير والتحليلات';
const UNAUTHORIZED = 'غير مصرح';
const EMPLOYEE_PICKER = 'اختر موظف';

const ASSIGN_SHIFTS = '/schedule/assign-shifts';
const CREATE_SCHEDULE = '/schedule/create-schedule';

test.describe.serial('feature 17 — org supervisor scoped UI', () => {
  test.use({ storageState: { cookies: [], origins: [] } }); // start logged out

  let ctx: APIRequestContext;
  let adminToken = '';
  let supUserId = '';

  const stamp = Date.now();
  const supLogin = `e2e_orgsup_ui_${stamp}`;
  const supPassword = 'OrgSup@123456';

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({ baseURL: API_URL, ignoreHTTPSErrors: true });
    const login = await ctx.post('/auth/login', {
      data: { userLogin: CREDENTIALS.admin.login, password: CREDENTIALS.admin.password }
    });
    adminToken = (await login.json()).data.token;
    const auth = { Authorization: `Bearer ${adminToken}` };

    // Pin the supervisor to a unit that actually has employees, so the
    // assign/create pickers are populated (scoped to this unit's tree).
    const emps = await ctx.get('/employees?page=1&pageSize=50', { headers: auth });
    const list = (await emps.json()).data as Array<{ organizationalUnitId?: string }>;
    const orgUnitId = list.find((e) => e.organizationalUnitId)?.organizationalUnitId;
    expect(orgUnitId, 'a unit with employees is required for the fixture').toBeTruthy();

    const create = await ctx.post('/users/new', {
      headers: auth,
      data: {
        username: `E2E OrgSup UI ${stamp}`,
        userLogin: supLogin,
        password: supPassword,
        confirmPassword: supPassword,
        role: 12, // OrgSupervisor
        organizationalUnitId: orgUnitId
      }
    });
    expect(create.ok(), `creating OrgSupervisor should succeed (${create.status()})`).toBeTruthy();
    supUserId = (await create.json()).data.userId as string;
  });

  test('sidebar shows the granted sections and hides System Settings', async ({ page }) => {
    await new LoginPage(page).goto();
    await new LoginPage(page).loginAndWait(supLogin, supPassword);

    // Operations sections the supervisor runs.
    for (const section of [ATTENDANCE_MGMT, EMPLOYEE_MGMT, SCHEDULE_MGMT, LEAVE_MGMT]) {
      await expect(page.getByText(section).first(), `sidebar should show ${section}`).toBeVisible();
    }
    // Administration AND reports are closed: none of these may appear.
    await expect(page.getByText(SYSTEM_SETTINGS)).toHaveCount(0);
    await expect(page.getByText(REPORTS_ANALYTICS)).toHaveCount(0);
    await expect(page.getByText(REPORTS, { exact: true })).toHaveCount(0);
  });

  test('scoped write screens open for the supervisor (no «غير مصرح»)', async ({ page }) => {
    await new LoginPage(page).goto();
    await new LoginPage(page).loginAndWait(supLogin, supPassword);

    // Fixed-pattern assignment (تثبيت الدوام) — feature 14 write screen, now scoped.
    await page.goto(ASSIGN_SHIFTS, { waitUntil: 'domcontentloaded' });
    await expect(page).toHaveURL(/\/schedule\/assign-shifts/);
    await expect(page.getByText(UNAUTHORIZED)).toHaveCount(0);
    await expect(
      page.getByRole('combobox').filter({ hasText: EMPLOYEE_PICKER }),
      'assign-shifts employee picker is usable'
    ).toBeVisible({ timeout: 30_000 });

    // Schedule creation — also scoped to the supervisor's tree.
    await page.goto(CREATE_SCHEDULE, { waitUntil: 'domcontentloaded' });
    await expect(page).toHaveURL(/\/schedule\/create-schedule/);
    await expect(page.getByText(UNAUTHORIZED)).toHaveCount(0);
    await expect(
      page.getByRole('combobox').filter({ hasText: EMPLOYEE_PICKER }),
      'create-schedule employee picker is usable'
    ).toBeVisible({ timeout: 30_000 });
  });

  test('monitoring/operations screens open without redirect', async ({ page }) => {
    await new LoginPage(page).goto();
    await new LoginPage(page).loginAndWait(supLogin, supPassword);

    for (const route of [ROUTES.attendanceViewAll, ROUTES.employees, ROUTES.schedule]) {
      await page.goto(route, { waitUntil: 'domcontentloaded' });
      await expect(page, `${route} should open for the supervisor`).toHaveURL(
        new RegExp(route.replace(/\//g, '\\/'))
      );
      await expect(page.getByText(UNAUTHORIZED), `${route} not blocked`).toHaveCount(0);
    }
  });

  test('report deep-links are blocked (reports are admin/manager-only)', async ({ page }) => {
    await new LoginPage(page).goto();
    await new LoginPage(page).loginAndWait(supLogin, supPassword);

    // Each report page guard redirects the supervisor away from the report route.
    for (const route of [ROUTES.reportsOrganizational, ROUTES.reportsSummary]) {
      await page.goto(route, { waitUntil: 'domcontentloaded' });
      await expect(page, `report ${route} must not open for the supervisor`).not.toHaveURL(
        new RegExp(route.replace(/\//g, '\\/'))
      );
    }
  });

  test('admin-only System settings deep-link redirects the supervisor to /', async ({ page }) => {
    await new LoginPage(page).goto();
    await new LoginPage(page).loginAndWait(supLogin, supPassword);

    await page.goto(ROUTES.usersPermissions, { waitUntil: 'domcontentloaded' });
    await expect(page, 'users-permissions must redirect the supervisor away').not.toHaveURL(
      /\/system\/users-permissions/
    );
  });

  test.afterAll(async () => {
    if (supUserId) {
      await ctx
        .delete(`/users/${supUserId}`, { headers: { Authorization: `Bearer ${adminToken}` } })
        .catch(() => {});
    }
    await ctx.dispose();
  });
});
