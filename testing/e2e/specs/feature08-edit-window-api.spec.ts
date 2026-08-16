import {
  test,
  expect,
  request as pwRequest,
  type APIRequestContext
} from '@playwright/test';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 08 — 24-hour edit window for a status (موقف).
 *   «عدم الرجوع ليوم سابق لتغيير موقف. التغيير مفتوح خلال 24 ساعة فقط.»
 *
 * Rule (confirmed interpretation): a leave/status may only be edited within 24h
 * of being recorded — anchored on the stored `created_at`, evaluated in Baghdad
 * local time. After that, PUT /leaves/{id} must fail with the Arabic
 * `Leave.EditWindowExpired` error (Error.Problem ⇒ HTTP 400).
 *
 * This spec also covers the field-persistence fix shipped with the feature: the
 * update handler previously never applied the edited fields. Within the window,
 * an edit must now actually round-trip.
 *
 * `created_at` is aged directly in Postgres (no API path to backdate it), via
 * the E2E_PG_* connection in fixtures/db.ts.
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

const EDIT_WINDOW_CODE = 'Leave.EditWindowExpired';
const OVERLAP_CODE = 'Leave.OverlappingLeave';

test.describe.serial('feature 08 — 24h edit window for a status (API)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  let token = '';
  // Feature 13 (admin-only bypass of this window): seed.admin is Role.Admin and now
  // bypasses the 24h window, so the "window applies" assertions below must use a
  // NON-exempt actor. seed.super (SuperAdmin) is privileged enough to pass the
  // object-level (IDOR) ownership check from feature 11, yet — per «للادمن فقط» — is
  // still bound by the window. See feature13-edit-long-leave-admin-api.spec.ts.
  let superToken = '';
  let employeeId = '';
  let runBase = 0;
  const createdIds: string[] = [];

  // Far-future, per-run, non-overlapping windows (the backend rejects any
  // overlap for the employee — see leave-newtypes-api.spec.ts for the rationale).
  const dayISO = (offsetDays: number) => {
    const d = new Date(Date.UTC(2032, 0, 1));
    d.setUTCDate(d.getUTCDate() + offsetDays);
    return d.toISOString().slice(0, 19); // YYYY-MM-DDTHH:mm:ss
  };

  const auth = () => ({ Authorization: `Bearer ${token}` });
  const authSuper = () => ({ Authorization: `Bearer ${superToken}` });

  async function createLeave(startOffset: number, reason = 'E2E feature08') {
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
    expect(id).toMatch(/[0-9a-f-]{36}/);
    createdIds.push(id);
    return id;
  }

  /** Backdate created_at so the leave falls outside / near the 24h window. */
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

    const login = await ctx.post('/auth/login', {
      data: { userLogin: 'seed.admin', password: 'Admin@123456' }
    });
    expect(login.ok(), 'login should succeed').toBeTruthy();
    token = (await login.json()).data.token;

    const superLogin = await ctx.post('/auth/login', {
      data: { userLogin: 'seed.super', password: 'Super@123456' }
    });
    expect(superLogin.ok(), 'superadmin login should succeed').toBeTruthy();
    superToken = (await superLogin.json()).data.token;

    const emps = await ctx.get('/employees?page=1&pageSize=1', { headers: auth() });
    expect(emps.ok(), 'employees list should load').toBeTruthy();
    employeeId = (await emps.json()).data[0].id;

    db = pgClient();
    await db.connect();

    runBase = Math.floor(Date.now() / 1000) % 9000;
  });

  test('within window: an edit is accepted AND actually persists', async () => {
    const id = await createLeave(runBase, 'before edit');

    const put = await ctx.put(`/leaves/${id}`, {
      headers: auth(),
      data: {
        leaveType: 1,
        startDate: dayISO(runBase),
        endDate: dayISO(runBase + 1),
        reason: 'after edit — feature08'
      }
    });
    expect(put.status(), 'fresh leave should be editable').toBe(200);

    // Field-persistence fix: the new reason must round-trip.
    const get = await ctx.get(`/leaves/${id}`, { headers: auth() });
    expect(get.ok()).toBeTruthy();
    const body = await get.json();
    expect(body.reason, 'edited reason must be persisted').toBe(
      'after edit — feature08'
    );
  });

  test('outside window: editing a >24h-old status is blocked (400 EditWindowExpired)', async () => {
    const id = await createLeave(runBase + 10, 'aged');
    await ageCreatedAt(id, 48);

    // Acted as SuperAdmin (non-exempt): an Admin would bypass the window (feature 13).
    const put = await ctx.put(`/leaves/${id}`, {
      headers: authSuper(),
      data: { reason: 'should be rejected' }
    });
    expect(put.status(), 'aged leave edit must be rejected').toBe(400);
    const body = await put.json();
    expect(body.title || body.detail).toContain(EDIT_WINDOW_CODE);

    // And the rejected edit must NOT have persisted.
    const get = await ctx.get(`/leaves/${id}`, { headers: auth() });
    expect((await get.json()).reason).toBe('aged');
  });

  test('boundary: ~23h old is editable, ~25h old is rejected', async () => {
    // Acted as SuperAdmin (non-exempt) so this exercises the window itself, not the
    // feature-13 admin bypass.
    const editable = await createLeave(runBase + 20, 'boundary-editable');
    await ageCreatedAt(editable, 23);
    const okPut = await ctx.put(`/leaves/${editable}`, {
      headers: authSuper(),
      data: { reason: 'still inside window' }
    });
    expect(okPut.status(), '~23h old should still be editable').toBe(200);

    const expired = await createLeave(runBase + 30, 'boundary-expired');
    await ageCreatedAt(expired, 25);
    const rejPut = await ctx.put(`/leaves/${expired}`, {
      headers: authSuper(),
      data: { reason: 'just past window' }
    });
    expect(rejPut.status(), '~25h old should be rejected').toBe(400);
    expect((await rejPut.json()).title).toContain(EDIT_WINDOW_CODE);
  });

  test('within window: editing dates into an overlap still returns OverlappingLeave (409)', async () => {
    // Two fresh, non-overlapping leaves; move A onto B's window.
    const a = await createLeave(runBase + 40, 'overlap-A');
    const b = await createLeave(runBase + 42, 'overlap-B');

    const put = await ctx.put(`/leaves/${a}`, {
      headers: auth(),
      data: { startDate: dayISO(runBase + 42), endDate: dayISO(runBase + 43) }
    });
    expect(put.status(), 'overlap on the NEW dates must be detected').toBe(409);
    expect((await put.json()).title).toContain(OVERLAP_CODE);
    expect(b).toMatch(/[0-9a-f-]{36}/);
  });

  test.afterAll(async () => {
    for (const id of createdIds) {
      await ctx
        .delete(`/leaves/${id}`, { headers: auth() })
        .catch(() => {});
    }
    await db?.end().catch(() => {});
    await ctx.dispose();
  });
});
