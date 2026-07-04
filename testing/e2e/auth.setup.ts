import { test as setup, expect } from '@playwright/test';
import { LoginPage } from './pages/login.page';
import { CREDENTIALS, STORAGE_STATE } from './fixtures/test-data';

/**
 * Authentication setup — runs as the "setup" project before the spec project.
 * Logs in through the real UI once per role and persists the NextAuth session
 * cookies to storageState files, so specs start already authenticated.
 */

setup('authenticate as admin', async ({ page }) => {
  const login = new LoginPage(page);
  await login.goto();
  await login.loginAndWait(CREDENTIALS.admin.login, CREDENTIALS.admin.password);
  await expect(page).toHaveURL(/\/dashboard/);
  await page.context().storageState({ path: STORAGE_STATE.admin });
});

setup('authenticate as super', async ({ page }) => {
  const login = new LoginPage(page);
  await login.goto();
  await login.loginAndWait(CREDENTIALS.super.login, CREDENTIALS.super.password);
  await expect(page).toHaveURL(/\/dashboard/);
  await page.context().storageState({ path: STORAGE_STATE.super });
});
