import { test, expect } from '../fixtures/test-base';
import { LoginPage } from '../pages/login.page';
import { CREDENTIALS, ROUTES } from '../fixtures/test-data';

/**
 * Auth & route-guard. These run UNAUTHENTICATED (storageState cleared) so they
 * exercise the middleware redirect and the credentials flow from scratch.
 */
test.describe('auth', () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  test('unauthenticated access to a protected route redirects to /login', async ({
    page
  }) => {
    await page.goto(ROUTES.dashboard);
    await expect(page).toHaveURL(/\/login/);
    // middleware appends ?callbackUrl=<pathname>
    await expect(page).toHaveURL(/callbackUrl/);
  });

  test('invalid credentials show an error and stay on /login', async ({
    page
  }) => {
    const login = new LoginPage(page);
    await login.goto();
    await login.login('seed.admin', 'wrong-password');
    await expect(login.errorAlert()).toBeVisible();
    await expect(page).toHaveURL(/\/login/);
  });

  test('valid admin credentials reach the dashboard', async ({ page }) => {
    const login = new LoginPage(page);
    await login.goto();
    await login.loginAndWait(
      CREDENTIALS.admin.login,
      CREDENTIALS.admin.password
    );
    await expect(page).toHaveURL(/\/dashboard/);
  });
});
