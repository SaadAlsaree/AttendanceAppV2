import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';
import { pgClient } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 04 (API): اسماء الحاضرين — order attendees by check-in precedence
 * (الاسبقية بالوقت = earliest check-in first).
 *
 * This is the backend half. It proves the `sortBy=checkInTime&sortOrder=asc`
 * contract the UI relies on actually works — which it did NOT before this change:
 * `GetAttendanceQueryHandler` switched on `SortBy.ToUpperInvariant()` against
 * lowercase case labels, so every sort silently fell back to date-desc. The fix
 * lowercases the switch input and makes the check-in branch null-safe + stable.
 *
 * Ground truth is computed from Postgres for a real busy date, mirroring the
 * handler's ordering: rows without a check-in last, then check_in_time asc, then
 * id asc (the stable tiebreaker the fix adds).
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;
const PAGE_SIZE = 100;

test.describe.serial('Feature 04 — attendees ordered by check-in (API)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  let token = '';
  let date = '';

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();

    // Pick the date with the most attendance rows that have a check-in time, so
    // there is a meaningful ordering to assert (need at least 2 distinct times).
    const pick = await db.query(`
      SELECT to_char(a.date::date, 'YYYY-MM-DD') AS d
      FROM public."Attendances" a
      WHERE a.is_deleted = false AND a.check_in_time IS NOT NULL
      GROUP BY a.date::date
      HAVING COUNT(DISTINCT a.check_in_time) >= 2
      ORDER BY COUNT(*) DESC
      LIMIT 1;`);
    expect(pick.rows.length, 'a date with >=2 distinct check-in times must exist').toBeGreaterThan(0);
    date = pick.rows[0].d;

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
  });

  test.afterAll(async () => {
    await ctx?.dispose();
    await db?.end().catch(() => {});
  });

  const fetchPage = async (sortOrder: 'asc' | 'desc') => {
    const res = await ctx.get(
      `/attendance?date=${date}&sortBy=checkInTime&sortOrder=${sortOrder}` +
        `&page=1&pageSize=${PAGE_SIZE}`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    expect(res.ok(), `GET /attendance (${sortOrder}) should succeed`).toBeTruthy();
    return (await res.json()).data as Array<{ id: string; checkInTime: string | null }>;
  };

  test('ascending check-in order matches DB ground truth', async () => {
    // Ground truth: nulls last, then check_in_time asc, then id asc (uuid order).
    const truth = await db.query(
      `
      SELECT a.id::text AS id
      FROM public."Attendances" a
      WHERE a.is_deleted = false AND a.date::date = $1
            AND (a.check_in_time IS NOT NULL OR a.check_out_time IS NOT NULL)
      ORDER BY (CASE WHEN a.check_in_time IS NULL THEN 1 ELSE 0 END),
               a.check_in_time ASC, a.id ASC
      LIMIT $2;`,
      [date, PAGE_SIZE]
    );
    const expectedIds = truth.rows.map((r) => r.id);

    const rows = await fetchPage('asc');
    const actualIds = rows.map((r) => r.id);

    expect(actualIds, 'API page-1 order must equal DB ground-truth order')
      .toEqual(expectedIds);

    // Non-null check-in times must be non-decreasing (the visible "اسبقية").
    const times = rows.map((r) => r.checkInTime).filter((t): t is string => !!t);
    for (let i = 1; i < times.length; i++) {
      expect(times[i] >= times[i - 1], `row ${i} check-in not before row ${i - 1}`).toBeTruthy();
    }
    // Any null-check-in rows must come after all timed rows.
    const firstNull = rows.findIndex((r) => !r.checkInTime);
    if (firstNull !== -1) {
      expect(rows.slice(firstNull).every((r) => !r.checkInTime),
        'rows without a check-in must all be at the end').toBeTruthy();
    }
  });

  test('sortOrder is honored — asc and desc differ (regression for the switch fix)', async () => {
    const asc = await fetchPage('asc');
    const desc = await fetchPage('desc');
    // Before the fix both fell back to date-desc and were identical. With >=2
    // distinct check-in times the first element must now differ.
    expect(asc[0]?.id, 'asc and desc first row should differ').not.toBe(desc[0]?.id);
  });
});
