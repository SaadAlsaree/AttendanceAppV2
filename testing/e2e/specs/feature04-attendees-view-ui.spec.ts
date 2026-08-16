import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 04 (UI): اسماء الحاضرين — "عند فتح واجهة الحضور تثبيت حضور اليوم فقط
 * مع الامكان الفلترة لباقي الايام".
 *
 * On first open the attendance screen must PIN to today (date + earliest-check-in
 * sort seeded into the URL) while keeping the date filter usable for other days.
 * A one-time `attendanceDefaultsApplied` marker means clearing the date does NOT
 * re-pin today on the next render.
 */

/** Mirror src/lib/utils/date-utils.ts getBaghdadToday(). */
function baghdadToday(): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Baghdad',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).format(new Date());
}

/** How the date chip renders the selected date — mirrors src/lib/format.ts formatDate(). */
function chipDateText(ymd: string): string {
  return new Intl.DateTimeFormat('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric'
  }).format(new Date(`${ymd}T00:00:00`));
}

const ROUTE = '/attendance/view-all-attendance';

test.describe.serial('Feature 04 — attendees view defaults to today (UI)', () => {
  let db: Client;
  let busyDate = '';

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();
    const pick = await db.query(`
      SELECT to_char(a.date::date, 'YYYY-MM-DD') AS d
      FROM public."Attendances" a
      WHERE a.is_deleted = false AND a.check_in_time IS NOT NULL
      GROUP BY a.date::date
      ORDER BY COUNT(*) DESC
      LIMIT 1;`);
    busyDate = pick.rows[0]?.d ?? '';
  });

  test.afterAll(async () => {
    await db?.end().catch(() => {});
  });

  test('bare URL redirects to today + earliest-check-in sort', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTE);

    const url = new URL(page.url());
    expect(url.searchParams.get('date'), 'date pinned to Baghdad today').toBe(baghdadToday());
    expect(url.searchParams.get('sortBy')).toBe('checkInTime');
    expect(url.searchParams.get('sortOrder')).toBe('asc');
    expect(url.searchParams.get('attendanceDefaultsApplied')).toBe('1');

    // The date chip is active (shows today) and clearable — proving the filter is
    // visible/usable for other days (req. 2). The chip renders a "Clear ... filter"
    // control only when a date is selected.
    await shell.expectTableOrEmptyState();
    const dateChip = page.getByRole('button', { name: /Clear .* filter/i }).first();
    await expect(dateChip).toBeVisible({ timeout: 30_000 });

    // Regression: the chip must show today's date, NOT epoch (Jan 1, 1970), which
    // is what the URL-value mangling in use-data-table produced before the fix.
    await expect(dateChip).toContainText(chipDateText(baghdadToday()));
    await expect(dateChip).not.toContainText('1970');
  });

  test('filtering to another day works and is not re-pinned to today', async ({ page }) => {
    test.skip(!busyDate, 'no attendance data to pick another day');
    const shell = new ShellPage(page);
    // Simulate the state after the user picks a different day (marker already set).
    await shell.goto(`${ROUTE}?attendanceDefaultsApplied=1&date=${busyDate}&sortBy=checkInTime&sortOrder=asc`);

    const url = new URL(page.url());
    expect(url.searchParams.get('date'), 'chosen day is kept, not overwritten by today').toBe(busyDate);
    await shell.expectTableOrEmptyState();
    expect(await shell.rowCount(), 'busy day should render rows').toBeGreaterThan(0);
  });

  test('clearing the date does not re-pin today', async ({ page }) => {
    const shell = new ShellPage(page);
    // Marker present but no date == the state right after the user clears the chip.
    await shell.goto(`${ROUTE}?attendanceDefaultsApplied=1`);

    const url = new URL(page.url());
    expect(url.searchParams.get('date'), 'date stays cleared (no re-pin)').toBeNull();
    expect(url.searchParams.get('attendanceDefaultsApplied')).toBe('1');
    await shell.expectTableOrEmptyState();
  });
});
