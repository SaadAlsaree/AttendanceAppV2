import { test, expect, type Page } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 05 (UI): انشاء جداول — schedules for newly added employees (admin only),
 * «حتى يظهر كل الحضور بشكل كامل». Drives the REAL create-schedule screen.
 *
 *  1) Admin gating + render: /schedule shows the "add schedule" button and
 *     /schedule/create-schedule renders the form (not Unauthorized).
 *  2) A newly-added employee is SELECTABLE in the employee combobox — proves the
 *     server-side search + newest-first ordering fix.
 *  3) Full create flow: select that employee, set a date range, assign a shift to
 *     each generated day, submit, land on the schedule list, and verify the
 *     AttendanceSchedule + one ScheduleDay per day were written. Cleaned up after.
 *
 * NOTE on resilience: the admin gate (`schedule/shifts/new` pattern) calls
 * `getCurrentUser()` server-side on every load. Under E2E speed the backend's
 * per-user token-bucket limiter (50 burst / 25 per min) 429s `/users/me`, so the
 * gate transiently renders «غير مصرح». The helpers below reload with backoff to
 * let tokens replenish — this is a harness artifact, not a product bug.
 */

// react-day-picker v8 caption month label, e.g. "June 2026".
const captionLabel = (d: Date) =>
  new Intl.DateTimeFormat('en-US', { month: 'long', year: 'numeric' }).format(d);

/** Load /schedule/create-schedule, retrying past transient rate-limit 403s. */
async function gotoCreateForm(page: Page) {
  for (let i = 0; i < 8; i++) {
    await page.goto('/schedule/create-schedule', { waitUntil: 'domcontentloaded' });
    await expect(page).not.toHaveURL(/\/login/);
    const label = page.getByText('اسم الموظف');
    const denied = page.getByText(/غير مصرح|unauthorized/i);
    await expect(label.or(denied).first()).toBeVisible({ timeout: 20_000 });
    if (await label.count()) return;
    await page.waitForTimeout(8000); // let the per-user token bucket replenish
  }
  throw new Error('create-schedule kept returning 403 (rate limited)');
}

/**
 * Open the employee combobox and pick a name. Match by VISIBLE text (the trigger's
 * accessible name is the shadcn field label via aria-labelledby). The server-side
 * search triggers a soft navigation that can occasionally drop the popover, so
 * retry open+search+pick until the trigger shows the chosen name.
 */
async function selectEmployee(page: Page, token: string) {
  const selected = () => page.getByRole('combobox').filter({ hasText: token });
  if (await selected().count()) return;

  for (let attempt = 0; attempt < 3; attempt++) {
    await page.getByRole('combobox').filter({ hasText: 'اختر موظف' }).click();
    await page.getByPlaceholder('ابحث عن موظف...').fill(token);
    const option = page.getByRole('option', { name: new RegExp(token) }).first();
    try {
      await option.waitFor({ state: 'visible', timeout: 12_000 });
      await option.click();
      await page.keyboard.press('Escape'); // CommandItem doesn't auto-close the popover
      if (await selected().count()) return;
    } catch {
      /* popover dropped during the server-search nav — retry */
    }
    await page.keyboard.press('Escape').catch(() => {});
    await page.waitForTimeout(800);
  }
  throw new Error(`could not select employee matching "${token}"`);
}

/** Open a date Popover (trigger shows `اختر التاريخ`) and click the target day. */
async function pickDate(page: Page, target: Date) {
  await page.getByRole('button', { name: /اختر التاريخ/ }).first().click();
  const popover = page.locator('[data-radix-popper-content-wrapper]').last();
  await expect(popover).toBeVisible();

  const wantCaption = captionLabel(target);
  for (let i = 0; i < 14; i++) {
    if (await popover.getByText(wantCaption, { exact: false }).count()) break;
    await popover.getByRole('button', { name: /next month/i }).click();
    await page.waitForTimeout(150);
  }

  const day = String(target.getDate());
  await popover
    .locator('button:not(.day-outside)', { hasText: new RegExp(`^${day}$`) })
    .first()
    .click();
}

test.describe.serial('Feature 05 — schedules for newly added employees (UI)', () => {
  let db: Client;
  let employeeName = '';
  let employeeId = '';

  // Range: next Sunday..Tuesday (>= today). All Iraqi working days, so NO day is
  // auto-excluded (Fri/Sat) — every generated day card is enabled and needs a
  // shift, which the form's "all active days have shifts" rule requires.
  const start = new Date();
  start.setHours(0, 0, 0, 0);
  start.setDate(start.getDate() + ((7 - start.getDay()) % 7)); // next Sunday (Sun=0); today if Sunday
  const end = new Date(start);
  end.setDate(end.getDate() + 2); // Sun, Mon, Tue

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();
    const row = await db.query(
      `SELECT e.id, e.full_name
         FROM "Employees" e
        WHERE e.organizational_unit_id IS NOT NULL
          AND e.is_deleted = false
          AND e.full_name IS NOT NULL AND length(trim(e.full_name)) > 0
          AND NOT EXISTS (
            SELECT 1 FROM "AttendanceSchedules" s
             WHERE s.employee_id = e.id AND s.is_deleted = false)
        ORDER BY e.created_at DESC
        LIMIT 1`
    );
    expect(row.rows.length, 'need a new employee with an org unit and no schedule').toBe(1);
    employeeId = row.rows[0].id;
    employeeName = row.rows[0].full_name;
  });

  test.afterAll(async () => {
    if (db && employeeId) {
      await db.query(
        `DELETE FROM "ScheduleDays" WHERE attendance_schedule_id IN
           (SELECT id FROM "AttendanceSchedules" WHERE employee_id = $1)`,
        [employeeId]
      ).catch(() => {});
      await db.query(`DELETE FROM "AttendanceSchedules" WHERE employee_id = $1`, [employeeId]).catch(() => {});
      await db.end().catch(() => {});
    }
  });

  test('admin sees the add button and the create form renders', async ({ page }) => {
    test.slow();
    const shell = new ShellPage(page);

    // The add link is gated on getCurrentUser too — retry past transient 403s.
    let sawAdd = false;
    for (let i = 0; i < 8; i++) {
      await shell.goto('/schedule');
      const add = page.getByRole('link', { name: /إضافة جدول دوام جديد/ });
      if (await add.count()) {
        await expect(add).toBeVisible();
        sawAdd = true;
        break;
      }
      await page.waitForTimeout(8000);
    }
    expect(sawAdd, 'admin should see the «إضافة جدول دوام جديد» button').toBe(true);

    await gotoCreateForm(page);
    await expect(page.getByText('اسم الموظف')).toBeVisible();
  });

  test('a newly-added employee is selectable via search', async ({ page }) => {
    test.slow();
    await gotoCreateForm(page);

    const token = employeeName.split(' ')[0];
    await selectEmployee(page, token);

    await expect(
      page.getByRole('combobox').filter({ hasText: token })
    ).toBeVisible();
  });

  test('admin creates a schedule for the new employee (full flow + DB)', async ({ page }) => {
    test.slow();
    await gotoCreateForm(page);

    // 1) Select the employee.
    const token = employeeName.split(' ')[0];
    await selectEmployee(page, token);

    // 2) Pick start + end dates (generates one day row per day in range).
    await pickDate(page, start);
    await pickDate(page, end);

    const expectedDays =
      Math.round((end.getTime() - start.getTime()) / 86_400_000) + 1;

    // 3) One day card per day in range; then assign a shift to ALL days via the
    //    "ملء جميع الأيام" control. It uses replace() under the hood, which
    //    refreshes the useFieldArray snapshot that gates the submit button —
    //    per-day Controller selects alone don't, so the button would stay disabled.
    const dayShiftTriggers = page
      .getByRole('combobox')
      .filter({ hasText: /اختر الوردية/ });
    await expect(dayShiftTriggers.first()).toBeVisible();
    expect(await dayShiftTriggers.count(), 'a day card per day in range').toBe(expectedDays);

    await page.getByRole('combobox').filter({ hasText: 'ملء جميع الأيام' }).click();
    await page.getByRole('option').first().click(); // "ملء بـ <first shift>"

    // 4) Submit (now enabled) and expect to land back on the schedule list.
    const submit = page.getByRole('button', { name: 'حفظ الجدولة' });
    await expect(submit).toBeEnabled({ timeout: 15_000 });
    await submit.click();
    await expect(page).toHaveURL(/\/schedule(\?|$)/, { timeout: 30_000 });

    // 5) DB verification: schedule + one ScheduleDay per day in the range.
    const sched = await db.query(
      `SELECT s.id,
              (SELECT count(*)::int FROM "ScheduleDays" d WHERE d.attendance_schedule_id = s.id) AS days
         FROM "AttendanceSchedules" s
        WHERE s.employee_id = $1 AND s.is_deleted = false
        ORDER BY s.created_at DESC LIMIT 1`,
      [employeeId]
    );
    expect(sched.rows.length, 'a schedule row should have been created').toBe(1);
    expect(sched.rows[0].days, 'one ScheduleDay per day in range').toBe(expectedDays);
  });
});
