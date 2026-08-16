import { test, expect } from '../fixtures/test-base';
import { request as pwRequest } from '@playwright/test';
import { ROUTES, STORAGE_STATE, API_URL, CREDENTIALS } from '../fixtures/test-data';

/**
 * FEATURE 18 — «تثبيت الدوام» indicator + filter on the employee list — UI.
 *
 * On /employee, the table now shows a «الدوام الثابت» column (مثبت / غير مثبت
 * badge) and a toolbar faceted filter that drives the server-side
 * `?hasFixedShift=` param. Driven in a real browser as seed.admin.
 */

test.describe.serial('feature 18 — employee fixed-shift column + filter (UI)', () => {
  test.use({ storageState: STORAGE_STATE.admin });

  test('employee table shows the الدوام الثابت column with status badges', async ({ page }) => {
    await page.goto(ROUTES.employees, { waitUntil: 'domcontentloaded' });
    await expect(page).toHaveURL(/\/employee/);

    // The new column header.
    await expect(
      page.getByRole('columnheader', { name: 'الدوام الثابت' }),
      'column header present'
    ).toBeVisible({ timeout: 30_000 });

    // At least one row renders a status badge (مثبت or غير مثبت).
    await expect(
      page.getByText(/^(مثبت|غير مثبت)$/).first(),
      'a fixed-shift status badge is rendered'
    ).toBeVisible({ timeout: 30_000 });
  });

  test('the toolbar filter narrows to «غير مثبت» via ?hasFixedShift=false', async ({ page }) => {
    await page.goto(ROUTES.employees, { waitUntil: 'domcontentloaded' });

    // Wait for the table to hydrate (a status badge rendered) before driving the toolbar.
    await expect(page.getByText(/^(مثبت|غير مثبت)$/).first()).toBeVisible({ timeout: 30_000 });

    // Open the faceted filter (the toolbar button carries the column label).
    const filterButton = page.getByRole('button', { name: /الدوام الثابت/ });
    await expect(filterButton, 'toolbar filter button present').toBeVisible({ timeout: 30_000 });
    await filterButton.click();

    // Confirm the popover actually opened before touching options — its search box
    // uses the column label as its placeholder. (Deterministic wait to avoid a
    // cold-compile race where the click lands before the trigger is interactive.)
    const popoverSearch = page.getByPlaceholder('الدوام الثابت');
    await expect(popoverSearch, 'filter popover opened').toBeVisible({ timeout: 15_000 });

    // Choose «غير مثبت» from the popover.
    const option = page.getByRole('option', { name: 'غير مثبت' });
    await expect(option, 'filter option visible').toBeVisible({ timeout: 15_000 });
    await option.click();
    await page.keyboard.press('Escape');

    // The selection is pushed to the URL and drives the server round-trip.
    await expect(page, 'filter reflected in the URL').toHaveURL(/hasFixedShift=false/, {
      timeout: 30_000
    });
  });

  test('the «تثبيت الدوام» deep-link pre-selects the target employee', async ({ page }) => {
    // Resolve a real employee code the way the row action encodes it (?searchTerm=<empId>).
    const ctx = await pwRequest.newContext({ baseURL: API_URL, ignoreHTTPSErrors: true });
    const login = await ctx.post('/auth/login', {
      data: { userLogin: CREDENTIALS.admin.login, password: CREDENTIALS.admin.password }
    });
    const token = (await login.json()).data.token;
    const list = await ctx.get('/employees?page=1&pageSize=1', {
      headers: { Authorization: `Bearer ${token}` }
    });
    const emp = (await list.json()).data[0] as { empId: string; fullName: string };
    await ctx.dispose();

    // Deep-link exactly like the employee-table action does.
    await page.goto(`/schedule/assign-shifts?searchTerm=${encodeURIComponent(emp.empId)}`, {
      waitUntil: 'domcontentloaded'
    });

    // The employee picker must open pre-selected on that employee — the trigger shows
    // their name, NOT the empty «اختر موظف» placeholder (the bug this guards against).
    await expect(
      page.getByText(emp.fullName).first(),
      'target employee is pre-selected on the assign screen'
    ).toBeVisible({ timeout: 30_000 });
    await expect(
      page.getByText('اختر موظف'),
      'the empty picker placeholder must be gone'
    ).toHaveCount(0);
  });
});
