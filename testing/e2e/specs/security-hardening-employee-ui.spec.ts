import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';
import { pgClient } from '../fixtures/db';
import { LoginPage } from '../pages/login.page';
import { ShellPage } from '../pages/shell.page';
import { API_URL, CREDENTIALS } from '../fixtures/test-data';

/**
 * SECURITY HARDENING — frontend regression for the EMPLOYEE role.
 *
 * The backend hardening (scoped /employees/search, guarded /files, Employee removed from
 * POST /attendance-logs and POST /holidays) must not change what an Employee-role user sees.
 * A fresh Employee user logs in through the real UI and visits every screen the sidebar
 * grants that role. We record every API response >= 400 and assert:
 *   - no screen bounced to /login or crashed (no Next.js error boundary)
 *   - none of the CHANGED endpoints ever returned 403/4xx/5xx to the page
 *   - the employee list + detail still render (the picker/list path uses GET /employees)
 *
 * Pre-existing, unrelated 401s from /organizational-units and /shifts (filter widgets — see
 * CLAUDE.md) are tolerated but reported in the test annotations.
 */

const ROLE_EMPLOYEE = 4;
const CHANGED = [/\/employees\/search/, /\/files/, /\/attendance-logs$/, /\/holidays/];

const EMPLOYEE_ROUTES = [
  '/dashboard',
  '/attendance/view-all-attendance',
  '/attendance/not-attendance',
  '/attendance/attendance-logs',
  '/employee',
  '/employee/addedit-employees',
  '/leave',
  '/leave/new',
  '/reports/organizational-report',
  '/reports/organizational-summary',
  '/profile/view-profile'
];

test.use({ storageState: { cookies: [], origins: [] } });

test.describe.serial('security hardening — Employee role UI unaffected', () => {
  let ctx: APIRequestContext;
  let adminToken = '';
  let empUserId = '';
  let ownEmployeeId = '';
  const stamp = Date.now().toString().slice(-7);
  const empLogin = `e2e_sec_ui_emp_${stamp}`;
  const empPassword = 'E2eSec@123456';

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({ baseURL: API_URL, ignoreHTTPSErrors: true });
    const login = await ctx.post('/auth/login', {
      data: { userLogin: CREDENTIALS.admin.login, password: CREDENTIALS.admin.password }
    });
    adminToken = (await login.json()).data.token;

    const db = pgClient();
    await db.connect();
    const row = await db.query(`
      SELECT e.id, e.organizational_unit_id
      FROM public."Employees" e
      WHERE e.is_deleted = false AND e.organizational_unit_id IS NOT NULL
      ORDER BY e.created_at DESC LIMIT 1`);
    await db.end();
    expect(row.rowCount).toBe(1);
    ownEmployeeId = row.rows[0].id;

    const create = await ctx.post('/users/new', {
      headers: { Authorization: `Bearer ${adminToken}` },
      data: {
        username: `E2E Sec UI Emp ${stamp}`,
        userLogin: empLogin,
        password: empPassword,
        confirmPassword: empPassword,
        role: ROLE_EMPLOYEE,
        organizationalUnitId: row.rows[0].organizational_unit_id
      }
    });
    expect(create.ok(), `create employee user (${create.status()})`).toBeTruthy();
    empUserId = (await create.json()).data.userId;
  });

  test('every Employee-visible screen renders; changed endpoints never fail', async ({ page }, testInfo) => {
    const failures: string[] = [];
    page.on('response', (res) => {
      const url = res.url();
      if (url.startsWith(API_URL) && res.status() >= 400) {
        failures.push(`${res.request().method()} ${url.replace(API_URL, '')} -> ${res.status()}`);
      }
    });

    await new LoginPage(page).goto();
    await new LoginPage(page).loginAndWait(empLogin, empPassword);
    const shell = new ShellPage(page);

    for (const route of [...EMPLOYEE_ROUTES, `/employee/${ownEmployeeId}`]) {
      await test.step(route, async () => {
        await shell.goto(route);
        await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {});
        await expect(page).not.toHaveURL(/\/login/);
        await expect(page.getByText(/Application error|Unhandled Runtime Error/)).toHaveCount(0);
      });
    }

    // Frontend hardening: security headers present, X-Powered-By gone (next.config.js).
    const head = await page.request.get('http://localhost:3003/login');
    expect(head.headers()['x-frame-options']).toBe('DENY');
    expect(head.headers()['content-security-policy']).toContain("frame-ancestors 'none'");
    expect(head.headers()['x-content-type-options']).toBe('nosniff');
    expect(head.headers()['x-powered-by']).toBeUndefined();

    testInfo.annotations.push({ type: 'api-4xx-5xx', description: failures.join(' | ') || 'none' });

    // NOTE: /employee redirects the Employee role home by a pre-existing frontend gate
    // (employee/page.tsx canView = Admin/Manager/SecurityOfficer/OrgSupervisor), so the
    // "list still renders" check uses a table screen Employee can open.
    await shell.goto('/attendance/attendance-logs');
    await shell.expectTableOrEmptyState();

    const onChanged = failures.filter((f) => CHANGED.some((re) => re.test(f.split(' ')[1])));
    expect(onChanged, 'no failing calls to the hardened endpoints').toEqual([]);

    const serverErrors = failures.filter((f) => / -> 5\d\d$/.test(f));
    expect(serverErrors, 'no 5xx from any endpoint').toEqual([]);
  });

  test.afterAll(async () => {
    if (empUserId) await ctx.delete(`/users/${empUserId}`, { headers: { Authorization: `Bearer ${adminToken}` } }).catch(() => {});
    await ctx.dispose();
  });
});
