import { test, expect } from '../fixtures/test-base';
import {
  request as pwRequest,
  type APIRequestContext
} from '@playwright/test';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';
import { API_URL, CREDENTIALS, STORAGE_STATE } from '../fixtures/test-data';

/**
 * FEATURE 13 — Open editing of long leaves, admin only (UI).
 *   «13. فتح التعديل على الاجازات الطويلة للادمن فقط.»
 *
 * The leave detail screen (/leave/{id}) disables the تعديل (Edit) button once the
 * status is older than the Feature-08 24h window. Feature 13 un-grays it again —
 * but «للادمن فقط» (admin only): an Admin may edit the long/old leave, while every
 * other role (incl. SuperAdmin) keeps the disabled button + expiry tooltip.
 *
 * Each scenario records a video (video: 'on') so the happy/sad flows are reviewable.
 * The disabled-button + backend authority for non-Admins is already proven in
 * feature08-*.spec.ts and feature13-edit-long-leave-admin-api.spec.ts; this spec is
 * the real-browser proof of the Admin bypass and the SuperAdmin block on the SAME
 * aged leave.
 *
 * created_at is aged directly in Postgres (no API path to backdate it).
 */

const EDIT = 'تعديل';
const SAVE = 'حفظ';
const NOTES = 'ملاحظات';
const EDIT_SUCCESS = 'تم تعديل طلب الإجازة بنجاح!';

// Record a video for every scenario in this file (happy + sad), not just failures.
test.use({ video: 'on' });

const dayISO = (offsetDays: number) => {
  const d = new Date(Date.UTC(2036, 0, 1));
  d.setUTCDate(d.getUTCDate() + offsetDays);
  return d.toISOString().slice(0, 19);
};

/** Shared API helper: log in as admin, create a leave, age its created_at. */
async function makeAgedLeave(
  ctx: APIRequestContext,
  db: Client,
  startOffset: number,
  reason: string,
  hoursAgo = 48
) {
  const login = await ctx.post('/auth/login', {
    data: { userLogin: CREDENTIALS.admin.login, password: CREDENTIALS.admin.password }
  });
  expect(login.ok(), 'admin login should succeed').toBeTruthy();
  const token = (await login.json()).data.token;
  const auth = { Authorization: `Bearer ${token}` };

  const emps = await ctx.get('/employees?page=1&pageSize=1', { headers: auth });
  const employeeId = (await emps.json()).data[0].id;

  const res = await ctx.post('/leaves', {
    headers: auth,
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

  const r = await db.query(
    `UPDATE public."Leaves" SET created_at = now() - ($2 || ' hours')::interval WHERE id = $1`,
    [id, String(hoursAgo)]
  );
  expect(r.rowCount, 'created_at aged for one row').toBe(1);

  return { id, token, auth };
}

// ── HAPPY: Admin edits a long/old leave past the window ────────────────────────
test.describe.serial('feature 13 — admin can edit a long/old leave (UI · happy)', () => {
  test.use({ storageState: STORAGE_STATE.admin });

  let ctx: APIRequestContext;
  let db: Client;
  let agedId = '';
  let auth: { Authorization: string };
  const runBase = Math.floor(Date.now() / 1000) % 9000;

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({ baseURL: API_URL, ignoreHTTPSErrors: true });
    db = pgClient();
    await db.connect();
    ({ id: agedId, auth } = await makeAgedLeave(ctx, db, runBase, 'ui13-admin-aged'));
  });

  test('admin sees an ENABLED Edit button on a 48h-old leave', async ({ page }) => {
    await page.goto(`/leave/${agedId}`);
    const editBtn = page.getByRole('button', { name: EDIT });
    await expect(editBtn).toBeVisible({ timeout: 30_000 });
    await expect(editBtn, 'feature 13 un-grays the button for admin').toBeEnabled();
  });

  test('admin edits the aged leave through the form — success toast + persists', async ({ page }) => {
    const newReason = `ui13-admin-edited-${runBase}`;
    await page.goto(`/leave/${agedId}/edit`);
    const notes = page.getByRole('textbox', { name: NOTES });
    await expect(notes).toBeVisible({ timeout: 30_000 });
    await notes.fill(newReason);
    await page.getByRole('button', { name: SAVE }).click();

    await expect(page.getByText(EDIT_SUCCESS)).toBeVisible({ timeout: 15_000 });

    // Ground truth: the edit past the window actually persisted.
    const get = await ctx.get(`/leaves/${agedId}`, { headers: auth });
    expect(get.ok()).toBeTruthy();
    expect((await get.json()).reason).toBe(newReason);
  });

  test.afterAll(async () => {
    await ctx.delete(`/leaves/${agedId}`, { headers: auth }).catch(() => {});
    await db?.end().catch(() => {});
    await ctx.dispose();
  });
});

// ── SAD: SuperAdmin is still blocked on the same kind of aged leave ─────────────
test.describe.serial('feature 13 — superadmin stays blocked on a long/old leave (UI · sad)', () => {
  test.use({ storageState: STORAGE_STATE.super });

  let ctx: APIRequestContext;
  let db: Client;
  let agedId = '';
  let auth: { Authorization: string };
  const runBase = (Math.floor(Date.now() / 1000) % 9000) + 100;

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({ baseURL: API_URL, ignoreHTTPSErrors: true });
    db = pgClient();
    await db.connect();
    ({ id: agedId, auth } = await makeAgedLeave(ctx, db, runBase, 'ui13-super-aged'));
  });

  test('superadmin gets NO Edit affordance on the long leave (admin-only gate)', async ({ page }) => {
    // «للادمن فقط»: a SuperAdmin is not the Admin, so it cannot edit the long leave.
    // (At the API layer the same SuperAdmin PUT is rejected with EditWindowExpired —
    // see feature13-edit-long-leave-admin-api.spec.ts. In the browser, GetLeaveById
    // additionally scopes non-Admins to their org units, so super never gets an
    // enabled تعديل control.) The sad outcome is observable as: no usable Edit button.
    await page.goto(`/leave/${agedId}`);
    await expect(page.getByRole('heading', { name: 'تفاصيل الموقف' })).toBeVisible({
      timeout: 30_000
    });
    // Give the client a moment to settle, then assert there is no ENABLED Edit button.
    await page.waitForTimeout(1500);
    const enabledEdit = page.getByRole('button', { name: EDIT }).and(page.locator(':not([disabled])'));
    await expect(enabledEdit, 'superadmin must not be able to edit the long leave').toHaveCount(0);
  });

  test.afterAll(async () => {
    await ctx.delete(`/leaves/${agedId}`, { headers: auth }).catch(() => {});
    await db?.end().catch(() => {});
    await ctx.dispose();
  });
});
