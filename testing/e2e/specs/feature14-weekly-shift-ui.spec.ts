import { test, expect } from '../fixtures/test-base';
import {
  request as pwRequest,
  type APIRequestContext
} from '@playwright/test';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';
import { API_URL, CREDENTIALS, STORAGE_STATE, uniqueSuffix } from '../fixtures/test-data';

/**
 * FEATURE 14 — تثبيت الدوام (weekly fixed shift) — UI.
 *
 * Drives the new admin screen /schedule/assign-shifts end-to-end in a real
 * browser: pick an employee, quick-fill one shift across the work week
 * (الأحد–الخميس), save, see the pattern on the employee view page, then clear
 * it. Ground truth for every step is Postgres (EmployeeWeeklyShifts).
 */

const SAVE_TOAST = 'تم تثبيت الدوام بنجاح';
const CLEAR_TOAST = 'تم مسح الدوام الثابت';

test.describe.serial('feature 14 — weekly fixed shift (UI)', () => {
  test.use({ storageState: STORAGE_STATE.admin });

  let ctx: APIRequestContext;
  let db: Client;
  let token = '';
  let employeeId = '';
  let employeeName = '';
  let shiftId = '';
  const shiftName = `E2E W14ui ${uniqueSuffix()}`;

  const auth = () => ({ Authorization: `Bearer ${token}` });

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({ baseURL: API_URL, ignoreHTTPSErrors: true });
    db = pgClient();
    await db.connect();

    const login = await ctx.post('/auth/login', {
      data: { userLogin: CREDENTIALS.admin.login, password: CREDENTIALS.admin.password }
    });
    expect(login.ok(), 'admin login should succeed').toBeTruthy();
    token = (await login.json()).data.token;

    // The form's dropdown lists the first page of employees — pick a pattern-less
    // one from that same page so it is guaranteed to be selectable.
    const emps = await ctx.get('/employees?page=1&pageSize=100', { headers: auth() });
    expect(emps.ok()).toBeTruthy();
    const list: { id: string; fullName: string }[] = (await emps.json()).data;
    const used = await db.query(
      'SELECT DISTINCT employee_id FROM public."EmployeeWeeklyShifts"'
    );
    const usedIds = new Set(used.rows.map((r: { employee_id: string }) => r.employee_id));
    const candidate = list.find((e) => !usedIds.has(e.id));
    expect(candidate, 'a pattern-less employee on page 1').toBeTruthy();
    employeeId = candidate!.id;
    employeeName = candidate!.fullName;

    // A dedicated active shift with a unique, searchable name for the selects.
    const mk = await ctx.post('/shifts', {
      headers: auth(),
      data: {
        name: shiftName,
        startTime: '09:00:00',
        endTime: '16:00:00',
        shiftType: 'Morning',
        isActive: true,
        gracePeriodMinutes: 10,
        allowEarlyCheckIn: false,
        allowLateCheckOut: false
      }
    });
    expect(mk.ok(), 'create UI test shift').toBeTruthy();
    shiftId = String(await mk.json()).replace(/"/g, '');
  });

  test('assign a weekly pattern via quick-fill and save', async ({ page }) => {
    await page.goto('/schedule/assign-shifts');

    // Employee select
    const employeeSelect = page
      .getByRole('combobox')
      .filter({ hasText: 'اختر موظف' });
    await expect(employeeSelect).toBeVisible({ timeout: 30_000 });
    await employeeSelect.click();
    await page.getByRole('option', { name: employeeName }).first().click();

    // Quick-fill: one shift for الأحد–الخميس
    await page.getByRole('combobox').filter({ hasText: 'اختر الدوام' }).click();
    await page.getByRole('option', { name: new RegExp(shiftName) }).click();
    await page.getByRole('button', { name: 'تطبيق على كل الأيام' }).click();

    await page.getByRole('button', { name: 'تثبيت الدوام', exact: true }).click();
    await expect(page.getByText(SAVE_TOAST)).toBeVisible({ timeout: 15_000 });

    // Ground truth: 5 rows (Sun..Thu), all on the created shift.
    const rows = await db.query(
      `SELECT day_of_week FROM public."EmployeeWeeklyShifts"
       WHERE employee_id = $1 AND shift_id = $2 ORDER BY day_of_week`,
      [employeeId, shiftId]
    );
    expect(rows.rows.map((r: { day_of_week: number }) => r.day_of_week)).toEqual([
      0, 1, 2, 3, 4
    ]);
  });

  test('«الدوام الثابت» tab on /schedule lists the patterned employee (feature 14b)', async ({
    page
  }) => {
    await page.goto('/schedule?tab=fixed');

    // Tab header present, fixed tab renders the grouped row.
    await expect(
      page.getByRole('link', { name: 'الدوام الثابت', exact: true })
    ).toBeVisible({ timeout: 30_000 });

    // Narrow to the subject via the table search (name or code).
    await page.getByPlaceholder('ابحث بالاسم أو الرمز...').fill(employeeName);
    await expect(page.getByText(employeeName).first()).toBeVisible({
      timeout: 15_000
    });
    // Pattern summary badge carries the E2E shift name (quick-fill = الأحد–الخميس run).
    await expect(page.getByText(new RegExp(shiftName)).first()).toBeVisible();

    // Toggling back to «الجداول» shows the schedules table again.
    await page.getByRole('link', { name: 'الجداول', exact: true }).click();
    await expect(page).toHaveURL(/\/schedule$/);
  });

  test('create-schedule form warns when the employee has a fixed pattern (feature 14b)', async ({
    page
  }) => {
    await page.goto(
      `/schedule/create-schedule?searchTerm=${encodeURIComponent(employeeName)}`
    );

    const employeeSelect = page
      .getByRole('combobox')
      .filter({ hasText: 'اختر موظف' });
    await expect(employeeSelect).toBeVisible({ timeout: 30_000 });
    await employeeSelect.click();
    await page.getByRole('option', { name: employeeName }).first().click();

    // Amber informational alert with the pattern summary appears.
    await expect(page.getByText('دوام ثابت').first()).toBeVisible({
      timeout: 15_000
    });
    await expect(page.getByText(new RegExp(shiftName)).first()).toBeVisible();
  });

  test('employee view page shows the fixed weekly pattern', async ({ page }) => {
    await page.goto(`/employee/${employeeId}`);
    await expect(page.getByText('الدوام الثابت')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText(shiftName).first()).toBeVisible();
    await expect(page.getByText('الأحد:')).toBeVisible();
    await expect(page.getByText('الخميس:')).toBeVisible();
  });

  test('reselecting the employee preloads the saved pattern, and clearing empties it', async ({
    page
  }) => {
    await page.goto('/schedule/assign-shifts');

    const employeeSelect = page
      .getByRole('combobox')
      .filter({ hasText: 'اختر موظف' });
    await expect(employeeSelect).toBeVisible({ timeout: 30_000 });
    await employeeSelect.click();
    await page.getByRole('option', { name: employeeName }).first().click();

    // The saved pattern is loaded back into the per-day selects.
    await expect(
      page.getByRole('combobox').filter({ hasText: shiftName }).first()
    ).toBeVisible({ timeout: 15_000 });

    await page.getByRole('button', { name: 'مسح الدوام الثابت' }).click();
    await expect(page.getByText(CLEAR_TOAST)).toBeVisible({ timeout: 15_000 });

    const rows = await db.query(
      'SELECT 1 FROM public."EmployeeWeeklyShifts" WHERE employee_id = $1',
      [employeeId]
    );
    expect(rows.rowCount, 'pattern cleared').toBe(0);
  });

  test('failed pattern load shows an error and blocks saving (full-replace guard)', async ({
    page
  }) => {
    // Kill the client-side GET /employees/{id} the form uses to preload the
    // pattern. Saving is full-replace, so a failed load must NOT be savable —
    // it would wipe the employee's real pattern with "all days off".
    await page.route(`**/employees/${employeeId}`, (route) =>
      route.request().method() === 'GET' ? route.abort() : route.fallback()
    );

    await page.goto('/schedule/assign-shifts');
    const employeeSelect = page
      .getByRole('combobox')
      .filter({ hasText: 'اختر موظف' });
    await expect(employeeSelect).toBeVisible({ timeout: 30_000 });
    await employeeSelect.click();
    await page.getByRole('option', { name: employeeName }).first().click();

    await expect(
      page.getByText('تعذر تحميل الدوام الحالي للموظف')
    ).toBeVisible({ timeout: 15_000 });
    await expect(
      page.getByRole('button', { name: 'تثبيت الدوام', exact: true })
    ).toBeDisabled();
    await expect(
      page.getByRole('button', { name: 'مسح الدوام الثابت' })
    ).toBeDisabled();
  });

  test.afterAll(async () => {
    await ctx
      .put(`/employees/${employeeId}/weekly-shifts`, { headers: auth(), data: { days: [] } })
      .catch(() => {});
    await ctx.delete(`/shifts/${shiftId}`, { headers: auth() }).catch(() => {});
    await db?.end().catch(() => {});
    await ctx.dispose();
  });
});
