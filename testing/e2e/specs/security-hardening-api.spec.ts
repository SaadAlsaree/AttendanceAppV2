import {
  test,
  expect,
  request as pwRequest,
  type APIRequestContext
} from '@playwright/test';
import { Client } from 'pg';
import { pgClient } from '../fixtures/db';

/**
 * SECURITY HARDENING — the three critical/high code fixes from the FP28 pentest
 * re-assessment, each proven CLOSED and each proven NOT to break the features
 * that legitimately use the same endpoints.
 *
 *  1. GET /employees/search — was unscoped (any Employee could dump every
 *     employee incl. email / RFID / national-ID scans, with no page-size cap).
 *     Now: non-Admin scoped to own unit sub-tree, PageSize ≤ 100, sensitive
 *     fields blanked for non-Admin. Admin behaviour unchanged.
 *  2. /files — POST/GET/DELETE were anonymous with no validation and the
 *     download echoed the uploader's Content-Type (stored XSS). Now: POST needs
 *     a staff role + real image bytes, DELETE is Admin-only, GET stays anonymous
 *     (face images are rendered by plain <img src>) but serves only whitelisted
 *     image types with nosniff.
 *  3. POST /attendance-logs and POST /holidays — Employee role could forge
 *     punches / create holidays. Now 403 for Employee; Admin & Manager still pass
 *     the role gate (asserted with an invalid body ⇒ 400, so nothing is created).
 *
 * Self-contained: creates an Employee + a Manager user via the API as admin and
 * deletes them in afterAll. Run isolated with --no-deps.
 */

const API = process.env.E2E_API_URL || 'http://localhost:7080';
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

const ROLE_MANAGER = 3; //  Domain.Enums.Role.Manager
const ROLE_EMPLOYEE = 4; // Domain.Enums.Role.Employee

// 1x1 PNG (valid signature + IHDR) and a fake "webshell" payload.
const PNG_BYTES = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==',
  'base64'
);
const ASPX_BYTES = Buffer.from('<%@ Page Language="C#" %><% Response.Write("PENTEST"); %>');

async function login(ctx: APIRequestContext, userLogin: string, password: string) {
  const res = await ctx.post('/auth/login', { data: { userLogin, password } });
  expect(res.ok(), `login ${userLogin} should succeed (${res.status()})`).toBeTruthy();
  return (await res.json()).data.token as string;
}

test.describe.serial('security hardening — search scope, /files, write BFLA (API)', () => {
  let ctx: APIRequestContext;
  let db: Client;
  const auth = (token: string) => ({ Authorization: `Bearer ${token}` });
  const stamp = Date.now().toString().slice(-7);

  let adminToken = '';
  let empToken = '';
  let mgrToken = '';
  let empUserId = '';
  let mgrUserId = '';
  let unitA = '';
  let subtreeCount = 0;
  let totalEmployees = 0;
  let foreignEmpName = '';
  let uploadedPng = '';

  const empLogin = `e2e_sec_emp_${stamp}`;
  const mgrLogin = `e2e_sec_mgr_${stamp}`;
  const password = 'E2eSec@123456';

  test.beforeAll(async () => {
    ctx = await pwRequest.newContext({
      baseURL: API,
      ignoreHTTPSErrors: true,
      ...(BURP ? { proxy: { server: BURP } } : {})
    });
    db = pgClient();
    await db.connect();
    adminToken = await login(ctx, 'seed.admin', 'Admin@123456');

    // Unit with children + employees, so scoping is non-trivial.
    const unitRow = await db.query(`
      WITH RECURSIVE tree AS (
        SELECT id AS root, id AS node FROM public."OrganizationalUnits"
        UNION ALL
        SELECT t.root, c.id FROM public."OrganizationalUnits" c JOIN tree t ON c.parent_unit_id = t.node
      )
      SELECT ou.id,
             (SELECT count(*) FROM public."Employees" e
              WHERE e.organizational_unit_id IN (SELECT node FROM tree WHERE root = ou.id)
                AND e.is_deleted = false) AS subtree_emps
      FROM public."OrganizationalUnits" ou
      WHERE ou.parent_unit_id IS NOT NULL
        AND (SELECT count(*) FROM public."OrganizationalUnits" c WHERE c.parent_unit_id = ou.id) > 0
      ORDER BY subtree_emps DESC
      LIMIT 1`);
    expect(unitRow.rowCount).toBe(1);
    unitA = unitRow.rows[0].id;
    subtreeCount = Number(unitRow.rows[0].subtree_emps);

    totalEmployees = Number(
      (await db.query(`SELECT count(*) FROM public."Employees" WHERE is_deleted = false`)).rows[0].count
    );
    expect(totalEmployees, 'scoping must actually reduce the set').toBeGreaterThan(subtreeCount);

    const foreign = await db.query(
      `WITH RECURSIVE tree AS (
         SELECT $1::uuid AS node
         UNION ALL
         SELECT c.id FROM public."OrganizationalUnits" c JOIN tree t ON c.parent_unit_id = t.node
       )
       SELECT e.full_name FROM public."Employees" e
       WHERE e.organizational_unit_id IS NOT NULL
         AND e.organizational_unit_id NOT IN (SELECT node FROM tree)
         AND e.is_deleted = false AND length(e.full_name) > 6
       ORDER BY e.created_at DESC LIMIT 1`,
      [unitA]
    );
    expect(foreign.rowCount).toBe(1);
    foreignEmpName = foreign.rows[0].full_name;

    for (const [login_, role, name] of [
      [empLogin, ROLE_EMPLOYEE, 'Emp'],
      [mgrLogin, ROLE_MANAGER, 'Mgr']
    ] as const) {
      const res = await ctx.post('/users/new', {
        headers: auth(adminToken),
        data: {
          username: `E2E Sec ${name} ${stamp}`,
          userLogin: login_,
          password,
          confirmPassword: password,
          role,
          organizationalUnitId: unitA
        }
      });
      expect(res.ok(), `creating ${name} should succeed (${res.status()})`).toBeTruthy();
      const id = (await res.json()).data.userId as string;
      if (role === ROLE_EMPLOYEE) empUserId = id;
      else mgrUserId = id;
    }
    empToken = await login(ctx, empLogin, password);
    mgrToken = await login(ctx, mgrLogin, password);
  });

  // ───────────────────────── 1. /employees/search ─────────────────────────

  test('CLOSED: Employee role is no longer allowed to search employees at all', async () => {
    const res = await ctx.get('/employees/search?SearchTerm=&Page=1&PageSize=10', { headers: auth(empToken) });
    expect(res.status(), 'no UI feature exists for Employee search — role removed').toBe(403);
  });

  test('CLOSED: Manager empty-term search is scoped to own sub-tree and capped at 100', async () => {
    const res = await ctx.get('/employees/search?SearchTerm=&Page=1&PageSize=1000', { headers: auth(mgrToken) });
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.totalCount, 'totalCount == employees in own sub-tree').toBe(subtreeCount);
    expect(body.data.length, 'page size is clamped').toBeLessThanOrEqual(100);
    expect(body.pageSize).toBe(100);
  });

  test('CLOSED: Manager never receives email / RFID / national-ID scans', async () => {
    const res = await ctx.get('/employees/search?SearchTerm=&Page=1&PageSize=50', { headers: auth(mgrToken) });
    const rows = (await res.json()).data as Array<Record<string, unknown>>;
    expect(rows.length).toBeGreaterThan(0);
    for (const r of rows) {
      expect(r.email ?? '').toBe('');
      expect(r.rfid ?? '').toBe('');
      expect(r.nationalIdFrontUrl ?? null).toBeNull();
      expect(r.nationalIdBackUrl ?? null).toBeNull();
    }
  });

  test('CLOSED: Manager cannot find a foreign-unit employee by name; Admin can', async () => {
    const q = `/employees/search?SearchTerm=${encodeURIComponent(foreignEmpName)}&Page=1&PageSize=50`;
    const emp = await (await ctx.get(q, { headers: auth(mgrToken) })).json();
    expect(emp.totalCount, 'foreign employee hidden from Manager').toBe(0);
    const adm = await (await ctx.get(q, { headers: auth(adminToken) })).json();
    expect(adm.totalCount, 'same search as Admin still works').toBeGreaterThan(0);
  });

  test('INTACT: Admin search is unscoped and still returns the sensitive columns', async () => {
    const res = await ctx.get('/employees/search?SearchTerm=&Page=1&PageSize=10', { headers: auth(adminToken) });
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.totalCount, 'Admin sees every employee').toBe(totalEmployees);
    expect(body.data.length).toBe(10);
    // Columns are present for Admin (values may legitimately be empty per row).
    expect(body.data[0]).toHaveProperty('email');
    expect(body.data[0]).toHaveProperty('rfid');
    expect(body.data[0]).toHaveProperty('nationalIdFrontUrl');
  });

  test('INTACT: GET /employees (the picker endpoint the UI uses) is unchanged for Employee', async () => {
    const res = await ctx.get('/employees?page=1&pageSize=1', { headers: auth(empToken) });
    expect(res.status()).toBe(200);
    expect((await res.json()).totalCount).toBe(subtreeCount);
  });

  // ─────────────────────────────── 2. /files ───────────────────────────────

  test('CLOSED: anonymous upload / delete are rejected', async () => {
    const up = await ctx.post('/files', {
      multipart: { file: { name: 'a.png', mimeType: 'image/png', buffer: PNG_BYTES } }
    });
    expect(up.status()).toBe(401);
    const del = await ctx.delete('/files/00000000-0000-0000-0000-000000000001');
    expect(del.status()).toBe(401);
  });

  test('CLOSED: Employee role cannot upload', async () => {
    const up = await ctx.post('/files', {
      headers: auth(empToken),
      multipart: { file: { name: 'a.png', mimeType: 'image/png', buffer: PNG_BYTES } }
    });
    expect(up.status()).toBe(403);
  });

  test('CLOSED: webshell / non-image bytes are rejected even for Admin', async () => {
    const aspx = await ctx.post('/files', {
      headers: auth(adminToken),
      multipart: { file: { name: 'shell.aspx', mimeType: 'application/octet-stream', buffer: ASPX_BYTES } }
    });
    expect(aspx.status(), 'wrong content type').toBe(400);

    const spoofed = await ctx.post('/files', {
      headers: auth(adminToken),
      multipart: { file: { name: 'shell.png', mimeType: 'image/png', buffer: ASPX_BYTES } }
    });
    expect(spoofed.status(), 'png content-type but not png bytes').toBe(400);

    const html = await ctx.post('/files', {
      headers: auth(adminToken),
      multipart: { file: { name: 'x.html', mimeType: 'text/html', buffer: Buffer.from('<script>alert(1)</script>') } }
    });
    expect(html.status(), 'html is not an image').toBe(400);
  });

  test('INTACT: Admin can upload a real PNG and it is served back as an image with nosniff', async () => {
    const up = await ctx.post('/files', {
      headers: auth(adminToken),
      multipart: { file: { name: 'face.png', mimeType: 'image/png', buffer: PNG_BYTES } }
    });
    expect(up.status(), 'real png upload').toBe(200);
    uploadedPng = String(await up.json()).replace(/"/g, '');
    expect(uploadedPng).toMatch(/^[0-9a-f-]{36}$/);

    // Anonymous GET — this is how <img src> loads employee face images.
    const get = await ctx.get(`/files/${uploadedPng}`);
    expect(get.status()).toBe(200);
    expect(get.headers()['content-type']).toContain('image/png');
    expect(get.headers()['x-content-type-options']).toBe('nosniff');
    expect(Buffer.from(await get.body()).equals(PNG_BYTES)).toBeTruthy();
  });

  test('INTACT: Manager (staff role) can also upload; Employee cannot delete; Admin can', async () => {
    const up = await ctx.post('/files', {
      headers: auth(mgrToken),
      multipart: { file: { name: 'face.png', mimeType: 'image/png', buffer: PNG_BYTES } }
    });
    expect(up.status()).toBe(200);
    const id = String(await up.json()).replace(/"/g, '');

    expect((await ctx.delete(`/files/${id}`, { headers: auth(empToken) })).status()).toBe(403);
    expect((await ctx.delete(`/files/${id}`, { headers: auth(mgrToken) })).status()).toBe(403);
    expect((await ctx.delete(`/files/${id}`, { headers: auth(adminToken) })).status()).toBe(204);
    expect((await ctx.get(`/files/${id}`)).status()).toBe(404);
  });

  test('INTACT: POST /auth/register still accepts a PNG face image and rejects a non-image', async () => {
    const empId = `E2ESEC${stamp}`;
    const form = (fileName: string, mime: string, buf: Buffer) => ({
      EmpId: empId,
      FirstName: 'E2E',
      SecondName: 'Sec',
      ThirdName: 'Reg',
      FourthName: 'Test',
      FamilyName: 'Case',
      RFID: `RF${stamp}`,
      OrganizationalUnitId: unitA,
      IsManager: 'false',
      FaceImage: { name: fileName, mimeType: mime, buffer: buf }
    });

    const bad = await ctx.post('/auth/register', {
      headers: auth(adminToken),
      multipart: form('shell.aspx', 'application/octet-stream', ASPX_BYTES)
    });
    expect(bad.status(), 'register with non-image is rejected').toBe(400);

    const good = await ctx.post('/auth/register', {
      headers: auth(adminToken),
      multipart: form('face.png', 'image/png', PNG_BYTES)
    });
    expect(good.status(), `register with png (${await good.text()})`).toBe(201);
    const newEmployeeId = String(await good.json()).replace(/"/g, '');
    await ctx.delete(`/employees/${newEmployeeId}`, { headers: auth(adminToken) }).catch(() => {});
  });

  // ───────────────── 3. POST /attendance-logs, POST /holidays ─────────────────

  test('CLOSED: Employee gets 403 on POST /attendance-logs and POST /holidays', async () => {
    const log = await ctx.post('/attendance-logs', {
      headers: auth(empToken),
      data: {
        employeeId: '00000000-0000-0000-0000-000000000001',
        dateTimeAttend: '2031-01-01T08:00:00',
        dateWork: '2031-01-01',
        timeAttend: '08:00:00',
        direct: 1,
        deviceNo: 'E2E',
        deviceName: 'E2E',
        empId: 'E2E',
        empName: 'E2E',
        cardNo: 'E2E'
      }
    });
    expect(log.status()).toBe(403);

    const hol = await ctx.post('/holidays', {
      headers: auth(empToken),
      data: { organizationId: unitA, name: `E2E ${stamp}`, date: '2031-01-01', isRecurring: false }
    });
    expect(hol.status()).toBe(403);
  });

  test('INTACT: Admin and Manager still pass the role gate (invalid body ⇒ 400, not 403)', async () => {
    for (const token of [adminToken, mgrToken]) {
      const log = await ctx.post('/attendance-logs', { headers: auth(token), data: {} });
      expect(log.status(), 'attendance-logs gate admits staff').not.toBe(403);
      expect(log.status()).toBeLessThan(500);

      const hol = await ctx.post('/holidays', { headers: auth(token), data: {} });
      expect(hol.status(), 'holidays gate admits staff').not.toBe(403);
      expect(hol.status()).toBeLessThan(500);
    }
  });

  test.afterAll(async () => {
    if (uploadedPng) await ctx.delete(`/files/${uploadedPng}`, { headers: auth(adminToken) }).catch(() => {});
    for (const id of [empUserId, mgrUserId]) {
      if (id) await ctx.delete(`/users/${id}`, { headers: auth(adminToken) }).catch(() => {});
    }
    // Hard-delete the employee created by the register test (DELETE /employees only soft-deletes,
    // and GET /employees counts soft-deleted rows — which would skew other specs' ground truth).
    // Register creates a Leaves row per employee, so dependents go first. One statement per call
    // (node-postgres extended protocol executes a single statement when params are present).
    const empId = `E2ESEC${stamp}`;
    await db
      .query(
        `DELETE FROM public."Leaves" WHERE employee_id IN (SELECT id FROM public."Employees" WHERE emp_id = $1)
            OR employee_id1 IN (SELECT id FROM public."Employees" WHERE emp_id = $1)`,
        [empId]
      )
      .catch(() => {});
    await db.query(`DELETE FROM public."Employees" WHERE emp_id = $1`, [empId]).catch(() => {});
    await db.end().catch(() => {});
    await ctx.dispose();
  });
});
