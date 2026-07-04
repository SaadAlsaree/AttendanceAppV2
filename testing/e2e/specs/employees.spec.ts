import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { ROUTES } from '../fixtures/test-data';

test.describe('employees', () => {
  test('employee list loads with data', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.employees);
    await shell.expectTableOrEmptyState();
    // The seed dump has 5,217 employees; expect at least one row.
    expect(await shell.rowCount()).toBeGreaterThan(0);
  });

  test('add/edit employees screen loads', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.employeesAddEdit);
    await expect(page.locator('main').first()).toBeVisible();
  });

  // Full create requires the employee form's required fields (names, org unit).
  // Implement, then verify the row in "Employees" + linked "user" via Postgres MCP.
  test.fixme('create a new employee persists to "Employees"', async () => {
    // TODO
  });
});
