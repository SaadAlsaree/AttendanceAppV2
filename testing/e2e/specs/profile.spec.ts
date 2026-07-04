import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { ROUTES } from '../fixtures/test-data';

test.describe('profile', () => {
  test('view-profile screen loads for the logged-in user', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.profile);
    await expect(page.locator('main').first()).toBeVisible();
  });

  // change-password posts to the users endpoint; verify the password_hash in
  // "user" changes (and login still works with the new password) — do this with
  // a throwaway seeded user, never the shared seed.admin.
  test.fixme('change password updates the user hash', async () => {
    // TODO
  });
});
