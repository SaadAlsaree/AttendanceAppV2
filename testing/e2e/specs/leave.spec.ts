import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { ROUTES, uniqueSuffix } from '../fixtures/test-data';

/**
 * FEATURE UNDER TEST: 5 new leave types added to LeaveType
 * (frontend src/features/leave/types/leaves.ts + backend LeaveTypeEnum.cs):
 *   FiveYearLeave=16 (إجازة 5 سنوات), Assignment=17 (تكليف),
 *   GuardDescent=18 (نزول خفر), Exempted=19 (معفي), PeriodicLeave=20 (إجازة دورية)
 *
 * Form: src/features/leave/components/leave-form.tsx
 *   employee combobox "اختر موظف", leave-type select "اختر نوع الموقف",
 *   datetime-local start/end, reason textarea "ملاحظات", submit "حفظ".
 *   On create success → toast + router.push('/leave').
 */

const NEW_LEAVE_TYPES = [
  'إجازة 5 سنوات',
  'تكليف',
  'نزول خفر',
  'معفي',
  'إجازة دورية'
];

test.describe('leave — new leave types feature', () => {
  test('leave list loads', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.leave);
    await shell.expectTableOrEmptyState();
  });

  test('new-leave form exposes the 5 new leave types', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.leaveNew);

    const trigger = page
      .getByRole('combobox')
      .filter({ hasText: 'اختر نوع الموقف' });
    const listbox = page.getByRole('listbox');

    // Open the Radix Select; retry the click until the listbox actually opens
    // (the trigger can swallow the first pointer event right after hydration).
    await expect(async () => {
      await trigger.click();
      await expect(listbox).toBeVisible({ timeout: 2000 });
    }).toPass({ timeout: 20_000 });

    // The 5 new types are at the bottom of a 20-item scroll list, so assert they
    // are PRESENT in the rendered listbox (toBeAttached), not scrolled-into-view.
    for (const label of NEW_LEAVE_TYPES) {
      await expect(
        listbox.getByRole('option', { name: label, exact: true })
      ).toBeAttached();
    }
  });

  // BLOCKED by a separate bug: the new-leave form's employee dropdown is EMPTY
  // ("لا يوجد موظفين") — the browser-side GET /employees returns 401 (no bearer),
  // so no employee can be selected and the form can't be submitted via the UI.
  // The backend half of THIS feature (accepting the new leave types) is instead
  // verified directly against POST /leaves with leaveType=18 (GuardDescent),
  // routed through Burp, asserting "Leaves".leave_type='GuardDescent' in Postgres.
  // Re-enable this UI flow once the employees-load 401 is fixed.
  test.fixme('create a leave with a new type (نزول خفر) persists', async ({
    page
  }) => {
    const shell = new ShellPage(page);
    const reason = `E2E leave ${uniqueSuffix()}`;
    await shell.goto(ROUTES.leaveNew);

    // 1) pick an employee from the combobox (role=combobox, not button).
    // NOTE: typing in its search box triggers a router.push that reloads the
    // form, so we do NOT type — just open and pick the first employee.
    await page
      .getByRole('combobox')
      .filter({ hasText: 'اختر موظف' })
      .click();
    await page.getByRole('option').first().click();
    // The popover only sets the value (doesn't self-close) — close it.
    await page.keyboard.press('Escape');

    // 2) pick the NEW leave type "نزول خفر" (GuardDescent = 18)
    await page
      .getByRole('combobox')
      .filter({ hasText: 'اختر نوع الموقف' })
      .click();
    await page.getByRole('option', { name: 'نزول خفر', exact: true }).click();

    // 3) dates + reason
    const dts = page.locator('input[type="datetime-local"]');
    await dts.nth(0).fill('2026-07-01T09:00');
    await dts.nth(1).fill('2026-07-03T17:00');
    await page.getByLabel('ملاحظات').fill(reason);

    // 4) submit → success redirects to /leave
    await page.getByRole('button', { name: 'حفظ' }).click();
    await page.waitForURL('**/leave', { waitUntil: 'commit', timeout: 45_000 });

    // Authoritative existence is verified in Postgres ("Leaves".leave_type =
    // 'GuardDescent', reason = '<reason>'). The reason is logged so the DB
    // check can target this exact row.
    // eslint-disable-next-line no-console
    console.log(`[leave] created reason="${reason}" type=GuardDescent(18)`);
  });
});
