import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { ROUTES } from '../fixtures/test-data';

test.describe('dashboard', () => {
  test('loads authenticated', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.dashboard);

    await expect(page).toHaveURL(/\/dashboard/);
    await expect(page.locator('main').first()).toBeVisible();
    // Dashboard stats require ?OrganizationId on the backend; any failing
    // :7080 call is reported as an annotation by the diagnostics fixture and
    // scrutinised in the Burp review (see e2e/README.md).
  });
});
