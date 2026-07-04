import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { ROUTES } from '../fixtures/test-data';

test.describe('system / users & permissions', () => {
  test('users & permissions screen loads (admin-only)', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.usersPermissions);
    await shell.expectTableOrEmptyState();
  });

  // Server vs client variants hit /users* vs /users-permissions* (see
  // docs/frontend/system-admin.md) — a good Burp-review target for sequence.
  test.fixme('create a user persists to "user"', async () => {
    // TODO
  });
});
