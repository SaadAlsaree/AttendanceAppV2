import { type Page, expect } from '@playwright/test';

/**
 * App shell helpers — protected layout with sidebar + main content.
 * Generic, module-agnostic navigation + table assertions used by smoke specs.
 */
export class ShellPage {
  constructor(private readonly page: Page) {}

  /**
   * Navigate directly to a route and confirm we were NOT bounced to /login.
   * Uses 'domcontentloaded' (NOT 'networkidle') — the Next dev server keeps an
   * HMR websocket open + compiles routes on demand, so networkidle never
   * settles reliably. Readiness is asserted by callers via element waits.
   */
  async goto(route: string) {
    await this.page.goto(route, { waitUntil: 'domcontentloaded' });
    await expect(this.page).not.toHaveURL(/\/login/);
    await this.dismissTransientDevError();
    await expect(this.page.locator('main').first()).toBeVisible();
  }

  /**
   * The Next.js dev server (Turbopack) can briefly show a build-error overlay
   * while compiling a route on first hit (e.g. a transient CSS parse error in
   * the bundled output). It clears on recompile — reload once if we see it.
   * Dev-only; harmless in any environment without the overlay.
   */
  private async dismissTransientDevError() {
    const overlay = this.page.locator('nextjs-portal');
    for (let i = 0; i < 2; i++) {
      if (!(await overlay.count())) return;
      await this.page.waitForTimeout(1500);
      await this.page.reload({ waitUntil: 'domcontentloaded' });
    }
  }

  /** A data table (TanStack Table) OR an "empty state" — either is a valid render. */
  async expectTableOrEmptyState() {
    const table = this.page.locator('table');
    const noResults = this.page.getByText(/no results|لا توجد|لا يوجد/i);
    await expect(table.or(noResults).first()).toBeVisible({ timeout: 30_000 });
  }

  /** Click a toolbar "add/new" button if the listing exposes one. */
  addButton() {
    return this.page
      .getByRole('button', { name: /add|new|إضافة|جديد|إنشاء/i })
      .or(this.page.getByRole('link', { name: /add|new|إضافة|جديد|إنشاء/i }))
      .first();
  }

  /** Count visible body rows in the first table. */
  async rowCount(): Promise<number> {
    const rows = this.page.locator('table tbody tr');
    return rows.count();
  }
}
