import { type Page, expect } from '@playwright/test';

/**
 * Login screen — src/app/(auth)/login/page.tsx
 * Fields: #userLogin, #password. Submit button text: "تسجيل الدخول".
 * On success the app router.push('/dashboard').
 */
export class LoginPage {
  constructor(private readonly page: Page) {}

  async goto() {
    await this.page.goto('/login');
    await expect(this.page.locator('#userLogin')).toBeVisible();
  }

  async login(login: string, password: string) {
    await this.page.locator('#userLogin').fill(login);
    await this.page.locator('#password').fill(password);
    await this.page.getByRole('button', { name: 'تسجيل الدخول' }).click();
  }

  /** Fill creds, submit, and wait until the session lands on /dashboard. */
  async loginAndWait(login: string, password: string) {
    await this.login(login, password);
    await this.page.waitForURL('**/dashboard', { timeout: 45_000 });
  }

  errorAlert() {
    // The destructive <Alert> shown on a failed signIn.
    return this.page.getByRole('alert');
  }
}
