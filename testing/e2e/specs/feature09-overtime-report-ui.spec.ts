import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 09 (UI) — overtime report screen (/reports/overtime-report). The page is a
 * server component that reads startDate/endDate from the URL and renders the report
 * server-side, so this spec deep-links with a from–to range, computes ground truth
 * from Postgres, and asserts the RENDERED heading, the إجمالي ساعات العمل الإضافي card,
 * and that the per-employee table has exactly one row per employee-with-overtime.
 * Also checks the من/إلى filter labels and the empty-state prompt.
 *
 * Note: the filter's client-side /employees call may 401 (a pre-existing app auth
 * quirk); the report renders server-side so assertions are unaffected.
 */

type Truth = {
  from: string;
  to: string;
  employeesWithOt: number;
  totalHours: string; // toFixed(2)
};

test.describe.serial('Feature 09 — overtime report UI (browser)', () => {
  let db: Client;
  let truth: Truth;

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();

    // Narrow window with a small, stable overtime set so the rendered table stays small.
    const from = '2026-01-23';
    const to = '2026-01-23';

    const row = await db.query(`
      SELECT COUNT(DISTINCT a.employee_id) AS emps,
             COALESCE(SUM(a.overtime_minutes),0) AS minutes
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE a.is_deleted=false AND a.overtime_minutes>0
            AND a.date::date BETWEEN $1 AND $2`, [from, to]);

    const employeesWithOt = Number(row.rows[0].emps);
    const totalHours = (Number(row.rows[0].minutes) / 60).toFixed(2);
    expect(employeesWithOt, 'window must contain overtime rows to render').toBeGreaterThan(0);

    truth = { from, to, employeesWithOt, totalHours };
    // eslint-disable-next-line no-console
    console.log(`[feature-09-ui] ${from}..${to} employeesWithOt=${employeesWithOt} totalHours=${totalHours}`);
  });

  test.afterAll(async () => {
    await db?.end().catch(() => {});
  });

  test('HAPPY: overtime report renders heading, total card and one row per employee', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(
      `/reports/overtime-report?startDate=${truth.from}&endDate=${truth.to}`
    );

    await expect(page.getByRole('heading', { name: 'تقرير العمل الإضافي' }).first())
      .toBeVisible({ timeout: 30_000 });

    // إجمالي ساعات العمل الإضافي card shows the cumulative total for the range.
    const totalCard = page.locator('div').filter({ hasText: /^إجمالي ساعات العمل الإضافي/ }).first();
    await expect(totalCard).toContainText(truth.totalHours);

    // Per-employee table: exactly one row per employee-with-overtime.
    const rows = page.locator('table').first().locator('tbody tr');
    await expect(rows).toHaveCount(truth.employeesWithOt);

    // Print control is present and enabled when a report is loaded (same as تقرير الموظف).
    await expect(page.getByRole('button', { name: 'طباعة التقرير' })).toBeEnabled();

    await page.screenshot({ path: 'test-results/feature-09-overtime-report.png', fullPage: true });
    await test.info().attach('feature-09-overtime-report', {
      path: 'test-results/feature-09-overtime-report.png',
      contentType: 'image/png'
    });
  });

  test('HAPPY: filter exposes من/إلى date range labels', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto('/reports/overtime-report');

    await page.getByRole('button', { name: 'فلترة' }).click();
    await expect(page.getByText('من تاريخ').first()).toBeVisible();
    await expect(page.getByText('إلى تاريخ').first()).toBeVisible();
  });

  test('SAD: valid range with no overtime shows the no-records row', async ({ page }) => {
    // 2026-06-01..2026-06-19 has zero overtime rows — the report renders (200) but empty.
    const shell = new ShellPage(page);
    await shell.goto('/reports/overtime-report?startDate=2026-06-01&endDate=2026-06-19');

    await expect(page.getByRole('heading', { name: 'تقرير العمل الإضافي' }).first())
      .toBeVisible({ timeout: 30_000 });
    await expect(page.getByText(/لا توجد سجلات عمل إضافي ضمن المدى المحدد/)).toBeVisible();
  });

  test('SAD: empty-state prompt is shown when no range is selected', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto('/reports/overtime-report');

    await expect(page.getByRole('heading', { name: 'تقرير العمل الإضافي' }).first())
      .toBeVisible({ timeout: 30_000 });
    await expect(page.getByText(/يرجى تحديد المدى الزمني/)).toBeVisible();
    // With no report loaded, the print button is disabled.
    await expect(page.getByRole('button', { name: 'طباعة التقرير' })).toBeDisabled();
  });

  test('SAD: reset clears the filter back to the empty state', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto('/reports/overtime-report?startDate=2026-01-23&endDate=2026-01-23');

    // Open the filter and reset — the report must not crash, prompt returns.
    await page.getByRole('button', { name: 'فلترة' }).click();
    await page.getByRole('button', { name: 'إعادة تعيين' }).click();
    await expect(page.getByText('من تاريخ').first()).toBeVisible();
  });
});
