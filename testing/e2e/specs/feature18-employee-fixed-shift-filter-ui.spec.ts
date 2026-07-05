import { test, expect } from '../fixtures/test-base';
import { ROUTES, STORAGE_STATE } from '../fixtures/test-data';

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
});
