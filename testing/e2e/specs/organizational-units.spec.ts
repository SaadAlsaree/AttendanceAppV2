import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { ROUTES } from '../fixtures/test-data';

test.describe('organizational-units', () => {
  test('organizational units screen loads', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.organizationalUnit);
    await expect(page.locator('main').first()).toBeVisible();
  });

  // CRUD + tree view. After create, verify "OrganizationalUnits" row and the
  // parent_unit_id self-reference (docs/backend/organizations.md).
  test.fixme('create a unit persists to "OrganizationalUnits"', async () => {
    // TODO
  });
});
