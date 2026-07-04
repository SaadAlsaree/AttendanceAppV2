import {
  test,
  expect,
  request as pwRequest,
  type APIRequestContext
} from '@playwright/test';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 13 — Open editing of long leaves, admin only.
 *   «13. فتح التعديل على الاجازات الطويلة للادمن فقط.»
 *
 * Rule (Arabic = source of truth): the Feature-08 24-hour edit window
 * («التغيير مفتوح خلال 24 ساعة فقط») normally locks a status/موقف after 24h.
 * Feature 13 lets an Admin — and «فقط» (only) an Admin, NOT SuperAdmin — bypass
 * that window to edit a long/old leave. Every other role stays bound by the window.
 *
 * Concretely, in UpdateLeaveCommandHandler the EditWindowExpired check is skipped
 * iff `user.Role == Role.Admin`. This spec proves:
 *   - Admin CAN edit a >24h-old leave (200) and the edit persists.
 *   - SuperAdmin (privileged for the IDOR ownership check, but not exempt) is still
 *     blocked on the same aged leave (400 EditWindowExpired) — the Admin-only discriminator.
 *   - Admin still edits normally within the window (regression).
 *
 * `created_at` is aged directly in Postgres (no API path to backdate it), via the
 * E2E_PG_* connection in fixtures/db.ts. Seed users come from CLAUDE.md:
 *   seed.admin / Admin@123456 (Admin)   seed.super / Super@123456 (SuperAdmin)
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

const EDIT_WINDOW_CODE = 'Leave.EditWindowExpired';

test.describe.serial('feature 13 — admin-only edit of long/old leaves (API)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  let adminToken = '';
  let superToken = '';
  let employeeId = '';
  let runBase = 0;
  const createdIds: string[] = [];

  // Far-future, per-run, non-overlapping windows (the backend rejects overlap
  // for the employee — see leave-newtypes-api.spec.ts / feature08 for rationale).
  const dayISO = (offsetDays: number) => {
    const d = new Date(Date.UTC(2034, 0, 1));
    d.setUTCDate(d.getUTCDate() + offsetDays);
    return d.toISOString().slice(0, 19); // YYYY-MM-DDTHH:mm:ss
  };

  const authAdmin = () => ({ Authorization: `Bearer ${adminToken}` });
  const authSuper = () => ({ Authorization: `Bearer ${superToken}` });

  async function createLeave(startOffset: number, reason = 'E2E feature13') {
    const res = await ctx.post('/leaves', {
      headers: authAdmin(),
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
    expect(id).toMatch(/[0-9a-f-]{36}/);
    createdIds.push(id);
    return id;
  }

  /** Backdate created_at so the leave falls outside the 24h window. */
  async function ageCreatedAt(id: string, hoursAgo: number) {
    const r = await db.query(
      `UPDATE public."Leaves" SET created_at = now() - ($2 || ' hours')::interval WHERE id = $1`,
      [id, String(hoursAgo)]
    );
    expect(r.rowCount, 'created_at should be aged for exactly one row').toBe(1);
  }

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({
      baseURL: API,
      ignoreHTTPSErrors: true,
      ...(BURP ? { proxy: { server: BURP } } : {})
    });

    const adminLogin = await ctx.post('/auth/login', {
      data: { userLogin: 'seed.admin', password: 'Admin@123456' }
    });
    expect(adminLogin.ok(), 'admin login should succeed').toBeTruthy();
    adminToken = (await adminLogin.json()).data.token;

    const superLogin = await ctx.post('/auth/login', {
      data: { userLogin: 'seed.super', password: 'Super@123456' }
    });
    expect(superLogin.ok(), 'superadmin login should succeed').toBeTruthy();
    superToken = (await superLogin.json()).data.token;

    const emps = await ctx.get('/employees?page=1&pageSize=1', { headers: authAdmin() });
    expect(emps.ok(), 'employees list should load').toBeTruthy();
    employeeId = (await emps.json()).data[0].id;

    db = pgClient();
    await db.connect();

    runBase = Math.floor(Date.now() / 1000) % 9000;
  });

  test('HAPPY: admin can edit a 48h-old long leave (200) and the edit persists', async () => {
    const id = await createLeave(runBase, 'aged-long-leave');
    await ageCreatedAt(id, 48);

    const put = await ctx.put(`/leaves/${id}`, {
      headers: authAdmin(),
      data: {
        leaveType: 1,
        startDate: dayISO(runBase),
        endDate: dayISO(runBase + 1),
        reason: 'admin edited past the window — feature13'
      }
    });
    expect(put.status(), 'admin bypasses the 24h window').toBe(200);

    const get = await ctx.get(`/leaves/${id}`, { headers: authAdmin() });
    expect(get.ok()).toBeTruthy();
    expect((await get.json()).reason, 'edited reason must round-trip').toBe(
      'admin edited past the window — feature13'
    );
  });

  test('SAD: superadmin is blocked on the same kind of aged leave (400 EditWindowExpired)', async () => {
    // SuperAdmin passes the object-level (IDOR) ownership check but «فقط» excludes it
    // from the bypass — the window still applies.
    const id = await createLeave(runBase + 10, 'aged-for-super');
    await ageCreatedAt(id, 48);

    const put = await ctx.put(`/leaves/${id}`, {
      headers: authSuper(),
      data: { reason: 'super should be rejected — feature13' }
    });
    expect(put.status(), 'superadmin must NOT bypass the window').toBe(400);
    expect((await put.json()).title || '').toContain(EDIT_WINDOW_CODE);

    // The rejected edit must not have persisted.
    const get = await ctx.get(`/leaves/${id}`, { headers: authAdmin() });
    expect((await get.json()).reason).toBe('aged-for-super');
  });

  test('boundary: admin edits a 25h-old leave (200) while superadmin is rejected on a sibling', async () => {
    const adminLeave = await createLeave(runBase + 20, 'boundary-admin');
    await ageCreatedAt(adminLeave, 25);
    const adminPut = await ctx.put(`/leaves/${adminLeave}`, {
      headers: authAdmin(),
      data: { reason: 'admin just past window — ok' }
    });
    expect(adminPut.status(), 'admin allowed just past 24h').toBe(200);

    const superLeave = await createLeave(runBase + 30, 'boundary-super');
    await ageCreatedAt(superLeave, 25);
    const superPut = await ctx.put(`/leaves/${superLeave}`, {
      headers: authSuper(),
      data: { reason: 'super just past window — blocked' }
    });
    expect(superPut.status(), 'super blocked just past 24h').toBe(400);
    expect((await superPut.json()).title || '').toContain(EDIT_WINDOW_CODE);
  });

  test('regression: admin still edits a fresh in-window leave (200)', async () => {
    const id = await createLeave(runBase + 40, 'fresh');
    const put = await ctx.put(`/leaves/${id}`, {
      headers: authAdmin(),
      data: { reason: 'fresh edit — feature13' }
    });
    expect(put.status(), 'in-window admin edit still works').toBe(200);
    const get = await ctx.get(`/leaves/${id}`, { headers: authAdmin() });
    expect((await get.json()).reason).toBe('fresh edit — feature13');
  });

  test.afterAll(async () => {
    for (const id of createdIds) {
      await ctx.delete(`/leaves/${id}`, { headers: authAdmin() }).catch(() => {});
    }
    await db?.end().catch(() => {});
    await ctx.dispose();
  });
});
