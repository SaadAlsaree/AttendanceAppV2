import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 03 (UI) — part B: the admin-only per-employee report screen
 * (/reports/employee-report). The page is a server component that reads
 * employeeId/fromDate/toDate from the URL and renders the report server-side, so
 * this spec deep-links with a real employee + range (computing ground truth from
 * Postgres) and asserts the RENDERED header, summary card, day rows, and print
 * control. The default admin storageState satisfies the page's admin-only guard.
 *
 * Also checks the empty state (no params → a prompt to use the filter).
 *
 * Note: the filter's client-side /employees call may 401 (a pre-existing app auth
 * quirk) and surface in diagnostics; the report itself renders server-side so the
 * assertions are unaffected.
 */

type Truth = {
  employeeId: string;
  employeeName: string;
  from: string;
  to: string;
  totalDays: number;
};

test.describe.serial('Feature 03 — employee report UI (browser)', () => {
  let db: Client;
  let truth: Truth;

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();

    // Employee with the most data; use a short recent window so the table is small.
    const pick = await db.query(`
      SELECT a.employee_id AS id, e.full_name AS name
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id = a.employee_id AND e.is_deleted = false
      WHERE a.is_deleted = false
      GROUP BY a.employee_id, e.full_name
      ORDER BY COUNT(*) DESC
      LIMIT 1;`);
    expect(pick.rows.length, 'an employee with attendance data must exist').toBeGreaterThan(0);
    const employeeId: string = pick.rows[0].id;
    const employeeName: string = pick.rows[0].name;
    const from = '2026-06-01';
    const to = '2026-06-19';

    const totals = await db.query(
      `SELECT COUNT(*) AS total FROM public."Attendances"
       WHERE employee_id=$1 AND is_deleted=false
             AND date::date >= $2::date AND date::date <= $3::date`,
      [employeeId, from, to]
    );

    truth = {
      employeeId,
      employeeName,
      from,
      to,
      totalDays: Number(totals.rows[0].total)
    };
    expect(truth.totalDays, 'window must contain some rows to render').toBeGreaterThan(0);
    // eslint-disable-next-line no-console
    console.log(`[feature-03B-ui] employee=${employeeName} ${from}..${to} days=${truth.totalDays}`);
  });

  test.afterAll(async () => {
    await db?.end().catch(() => {});
  });

  test('employee report screen renders header, totals and day rows', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(
      `/reports/employee-report?employeeId=${truth.employeeId}` +
      `&fromDate=${truth.from}&toDate=${truth.to}`
    );

    // Page heading + the selected employee's name.
    await expect(page.getByRole('heading', { name: 'تقرير موظف' }).first())
      .toBeVisible({ timeout: 30_000 });
    await expect(page.getByText(truth.employeeName).first()).toBeVisible();

    // "إجمالي الأيام" summary card shows the period day count.
    const totalCard = page.locator('div').filter({ hasText: /^إجمالي الأيام/ }).first();
    await expect(totalCard).toContainText(String(truth.totalDays));

    // The daily table has exactly one row per attendance day. Scope to the FIRST
    // (on-screen) table — the page also holds a hidden print clone with its own table.
    const rows = page.locator('table').first().locator('tbody tr');
    await expect(rows).toHaveCount(truth.totalDays);

    // Print control is present and enabled when a report is loaded.
    await expect(page.getByRole('button', { name: 'طباعة التقرير' })).toBeEnabled();

    await page.screenshot({ path: 'test-results/feature-03B-employee-report.png', fullPage: true });
    await test.info().attach('feature-03B-employee-report', {
      path: 'test-results/feature-03B-employee-report.png',
      contentType: 'image/png'
    });
  });

  test('shows an empty-state prompt when no employee/range is selected', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto('/reports/employee-report');

    await expect(page.getByRole('heading', { name: 'تقرير موظف' }).first())
      .toBeVisible({ timeout: 30_000 });
    await expect(page.getByText(/يرجى اختيار الموظف والمدى الزمني/)).toBeVisible();
    // With no report, the print button is disabled.
    await expect(page.getByRole('button', { name: 'طباعة التقرير' })).toBeDisabled();
  });
});
