import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { ROUTES } from '../fixtures/test-data';

test.describe('reports', () => {
  // Report endpoints require OrganizationalUnitId; a missing param surfaces as a
  // 500 (docs/backend/dashboard-and-reports.md) — caught in the Burp review via
  // the diagnostics annotation, not a render failure here.
  test('organizational report screen loads', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.reportsOrganizational);
    await expect(page.locator('main').first()).toBeVisible();
  });

  test('organizational summary screen loads', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.reportsSummary);
    await expect(page.locator('main').first()).toBeVisible();
  });
});
