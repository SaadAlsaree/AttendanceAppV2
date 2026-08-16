import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { ROUTES } from '../fixtures/test-data';

test.describe('schedules', () => {
  test('schedules screen loads', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.schedule);
    await expect(page.locator('main').first()).toBeVisible();
  });

  // Schedule creation + "all ScheduleDays" coverage now lives in the feature05
  // specs (run: npm run test:e2e:feature05): the API spec creates a schedule for a
  // brand-new employee, asserts one ScheduleDay per day, and verifies retroactive
  // attendance reconciliation; the UI spec drives the real create-schedule screen
  // (admin gating, employee selectability, full create flow + DB check).
});
