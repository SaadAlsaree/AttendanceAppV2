import { test, expect } from '../fixtures/test-base';
import {
  request as pwRequest,
  type APIRequestContext
} from '@playwright/test';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';
import { API_URL, CREDENTIALS } from '../fixtures/test-data';

/**
 * FEATURE 08 — 24h edit window for a status (موقف), UI gating.
 *
 * The leave detail screen (/leave/{id}) shows a تعديل (Edit) button only while
 * the status is Pending. Feature 08 additionally disables it once the status is
 * older than 24h (advisory client check on `created_at`; the backend is the real
 * authority — proven in feature08-edit-window-api.spec.ts).
 *
 * Real-browser assertions:
 *   - a freshly recorded status → Edit button is ENABLED.
 *   - HAPPY PATH: editing a fresh status through the form succeeds — success
 *     toast appears AND the change is persisted (no error overlay/toast).
 *   - a >24h-old status (created_at aged in Postgres) → Edit button is DISABLED
 *     and carries the Arabic "انتهت مدة التعديل لهذا الموقف" tooltip.
 */

const EDIT = 'تعديل';
const SAVE = 'حفظ';
const NOTES = 'ملاحظات';
const EDIT_SUCCESS = 'تم تعديل طلب الإجازة بنجاح!';
const EXPIRED_TOOLTIP = 'انتهت مدة التعديل لهذا الموقف';

test.describe.serial('feature 08 — Edit button is gated by the 24h window (UI)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  let token = '';
  let employeeId = '';
  let runBase = 0;
  let freshId = '';
  let editId = '';
  let agedId = '';
  const createdIds: string[] = [];

  const dayISO = (offsetDays: number) => {
    const d = new Date(Date.UTC(2033, 0, 1));
    d.setUTCDate(d.getUTCDate() + offsetDays);
    return d.toISOString().slice(0, 19);
  };
  const auth = () => ({ Authorization: `Bearer ${token}` });

  async function createLeave(startOffset: number, reason: string) {
    const res = await ctx.post('/leaves', {
      headers: auth(),
      data: {
        employeeId,
        leaveType: 1,
        startDate: dayISO(startOffset),
        endDate: dayISO(startOffset + 1),
        reason
      }
    });
    expect(res.status(), 'create leave should be accepted').toBe(200);
    const id = String(await res.json()).replace(/"/g, '');
    createdIds.push(id);
    return id;
  }

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({ baseURL: API_URL, ignoreHTTPSErrors: true });
    const login = await ctx.post('/auth/login', {
      data: {
        userLogin: CREDENTIALS.admin.login,
        password: CREDENTIALS.admin.password
      }
    });
    expect(login.ok(), 'login should succeed').toBeTruthy();
    token = (await login.json()).data.token;

    const emps = await ctx.get('/employees?page=1&pageSize=1', { headers: auth() });
    employeeId = (await emps.json()).data[0].id;

    db = pgClient();
    await db.connect();

    runBase = Math.floor(Date.now() / 1000) % 9000;

    // Spaced far apart: re-saving an edit re-sends the dates, which the backend
    // shifts by a couple of days (pre-existing EnsureUtc convention), so adjacent
    // windows could otherwise collide and trip the overlap check.
    freshId = await createLeave(runBase + 50, 'ui-fresh');
    editId = await createLeave(runBase + 150, 'ui-edit-before');
    agedId = await createLeave(runBase + 250, 'ui-aged');
    const r = await db.query(
      `UPDATE public."Leaves" SET created_at = now() - interval '48 hours' WHERE id = $1`,
      [agedId]
    );
    expect(r.rowCount).toBe(1);
  });

  test('fresh status: Edit button is enabled', async ({ page }) => {
    await page.goto(`/leave/${freshId}`);
    const editBtn = page.getByRole('button', { name: EDIT });
    await expect(editBtn).toBeVisible({ timeout: 30_000 });
    await expect(editBtn).toBeEnabled();
  });

  test('happy path: editing a fresh status through the form succeeds', async ({ page }) => {
    const newReason = `ui-edit-after-${runBase}`;

    await page.goto(`/leave/${editId}/edit`);
    const notes = page.getByRole('textbox', { name: NOTES });
    await expect(notes).toBeVisible({ timeout: 30_000 });
    await notes.fill(newReason);
    await page.getByRole('button', { name: SAVE }).click();

    // The success toast must appear (and no error toast).
    await expect(page.getByText(EDIT_SUCCESS)).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText(EXPIRED_TOOLTIP)).toHaveCount(0);

    // Ground truth: the edit actually persisted via the API.
    const get = await ctx.get(`/leaves/${editId}`, { headers: auth() });
    expect(get.ok()).toBeTruthy();
    expect((await get.json()).reason).toBe(newReason);
  });

  test('aged status (>24h): admin is now exempt (feature 13) so Edit stays ENABLED', async ({ page }) => {
    // The 24h window still blocks NON-admins (proven in feature08-edit-window-api.spec.ts,
    // acted as SuperAdmin). But feature 13 (admin-only bypass) un-grays this button for an
    // Admin — and Admin is the only role that can view an arbitrary leave at all
    // (GetLeaveByIdQueryHandler scopes every non-Admin to its org units), so the
    // disabled-button state is no longer browser-observable with the seed users. Here we
    // assert the post-feature-13 truth for the admin viewer: enabled, no expiry tooltip.
    await page.goto(`/leave/${agedId}`);
    const editBtn = page.getByRole('button', { name: EDIT });
    await expect(editBtn).toBeVisible({ timeout: 30_000 });
    await expect(editBtn).toBeEnabled();
    await expect(editBtn).not.toHaveAttribute('title', EXPIRED_TOOLTIP);
  });

  test.afterAll(async () => {
    for (const id of createdIds) {
      await ctx.delete(`/leaves/${id}`, { headers: auth() }).catch(() => {});
    }
    await db?.end().catch(() => {});
    await ctx.dispose();
  });
});
