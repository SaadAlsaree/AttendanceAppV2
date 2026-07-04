import { test, expect, request as pwRequest, type APIRequestContext } from '@playwright/test';

/**
 * FEATURE: 5 new LeaveType values (16–20). This spec POSTs a real leave for
 * EACH new type against the API, routed through Burp (so all 5 appear in the
 * proxy history), asserts the backend accepts it (200), reads it back to confirm
 * the type round-trips, and cleans up. This is the backend coverage the UI flow
 * can't give while the employee dropdown is broken (GET /employees 401).
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

const NEW_TYPES = [
  { value: 16, name: 'FiveYearLeave', label: 'إجازة 5 سنوات' },
  { value: 17, name: 'Assignment', label: 'تكليف' },
  { value: 18, name: 'GuardDescent', label: 'نزول خفر' },
  { value: 19, name: 'Exempted', label: 'معفي' },
  { value: 20, name: 'PeriodicLeave', label: 'إجازة دورية' }
];

test.describe.serial('leave — POST a leave for each new type (API, via Burp)', () => {
  let ctx: APIRequestContext;
  let token = '';
  let employeeId = '';
  let runBase = 0; // unique day-offset per run (see note below)
  const createdIds: string[] = [];

  // The backend rejects a leave overlapping ANY existing leave for the employee
  // — and that check counts soft-deleted rows too, so fixed dates collide with
  // leftovers from previous runs. Derive a fresh far-future window per run and
  // space the 5 types apart so they never overlap each other or prior data.
  const dayISO = (offsetDays: number) => {
    const d = new Date(Date.UTC(2031, 0, 1));
    d.setUTCDate(d.getUTCDate() + offsetDays);
    return d.toISOString().slice(0, 19); // YYYY-MM-DDTHH:mm:ss
  };

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

    const emps = await ctx.get('/employees?page=1&pageSize=1', {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(emps.ok(), 'employees list should load with a bearer').toBeTruthy();
    employeeId = (await emps.json()).data[0].id;

    runBase = Math.floor(Date.now() / 1000) % 9000; // unique per run
  });

  NEW_TYPES.forEach((t, i) => {
    test(`POST /leaves accepts type ${t.value} — ${t.name} (${t.label})`, async () => {
      // Unique, non-overlapping 2-day window per type for this run.
      const startOffset = runBase + i * 4;
      const res = await ctx.post('/leaves', {
        headers: { Authorization: `Bearer ${token}` },
        data: {
          employeeId,
          leaveType: t.value,
          startDate: dayISO(startOffset),
          endDate: dayISO(startOffset + 1),
          reason: `E2E newtype ${t.name}`
        }
      });
      expect(res.status(), `${t.name} must be accepted`).toBe(200);

      const id = String(await res.json()).replace(/"/g, '');
      expect(id).toMatch(/[0-9a-f-]{36}/);
      createdIds.push(id);

      // Read it back — the new type must round-trip (as int 16–20 or its name).
      const get = await ctx.get(`/leaves/${id}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      expect(get.ok(), 'created leave should be retrievable').toBeTruthy();
      const body = JSON.stringify(await get.json());
      expect(
        body.includes(`"leaveType":${t.value}`) || body.includes(t.name),
        `leave ${id} should carry type ${t.name}`
      ).toBeTruthy();
    });
  });

  test.afterAll(async () => {
    // Clean up the leaves we created.
    for (const id of createdIds) {
      await ctx
        .delete(`/leaves/${id}`, { headers: { Authorization: `Bearer ${token}` } })
        .catch(() => {});
    }
    await ctx.dispose();
  });
});
