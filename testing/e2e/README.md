# Attendance E2E Tests

> **Location & git:** this suite lives in `AttendanceAppV2/testing/` and is
> git-tracked. It is self-contained — its own `package.json` +
> `playwright.config.ts` here, separate from the frontend repo. Run all commands
> from `testing/`. Docs live alongside in `AttendanceAppV2/docs/`.

Playwright E2E suite for the Attendance frontend, built to run under the same
**run → aggressively review every HTTP request in Burp → verify DB in Postgres →
fix/retry → report** loop as the `e2e-test-review` skill.

- **One spec per module** under `e2e/specs/` (mirrors `docs/frontend` + `docs/backend`).
- Tests drive the real UI with role/label selectors (no `data-testid` in the app yet).
- The browser (and optionally the Next.js dev server) is proxied through Burp so
  every request is captured for review.

## Layout

```
e2e/
├── auth.setup.ts          # logs in as admin & super, saves session to .auth/*.json
├── fixtures/
│   ├── test-base.ts       # test fixture: collects console errors + failed API responses
│   └── test-data.ts       # creds, routes, per-module DB-table map, unique-suffix helper
├── pages/                 # Page Object Models (login, shell, shifts exemplar)
└── specs/                 # one *.spec.ts per module
```

## Prerequisites

1. **Backend + infra** (Docker) up and DB restored — see root `CLAUDE.md`:
   ```bash
   cd ..   # AttendanceAppV2 repo root
   docker compose -f docker-compose.local.yml up -d
   ```
   API on `http://localhost:7080`, Postgres on host `localhost:5435` (`AttendanceDb`, `postgres/postgres`).
2. **Seed users** exist: `seed.admin / Admin@123456`, `seed.super / Super@123456`.
3. **Frontend**: the config auto-starts `npm run dev` in `original/attendance-frontend`
   (reuses an existing one on :3000). Tip: start it yourself for stability.
4. First time only — from this `testing/` folder:
   ```bash
   npm install                  # installs @playwright/test (self-contained here)
   npx playwright install chromium
   ```
5. Copy env defaults if you want to override anything: `cp .env.e2e.example .env.e2e`.

## Running

```bash
npm run test:e2e                 # full suite
npm run test:e2e:shifts          # one module (re-run target for the fix loop)
npm run test:e2e:headed          # watch it in a browser
npm run test:e2e:ui              # Playwright UI mode
```

Stub screens (manual-corrections, approvereject-records, assign-managers,
employee-schedules, work-locations, system-configuration) are intentionally not
covered; data-dependent flows (check-in/out, leave approve, schedule create,
employee/user/device create) are scaffolded as `test.fixme` with TODOs.

## The review loop (mirrors the skill)

Run a single module spec with Burp in front, then review. **Start Burp** with a
proxy listener on `127.0.0.1:8383`, then:

```bash
BURP_PROXY=http://127.0.0.1:8383 npm run test:e2e:shifts
```

When `BURP_PROXY` is set the config enables the browser proxy, accepts Burp's CA,
and starts the dev server with `HTTP(S)_PROXY` so NextAuth's **server-side**
`/auth/login` call is captured too. (If you run `npm run dev` yourself, start it
with the same proxy env to capture login.)

### 1. Aggressive Burp review — EVERY request, not just failures

Fetch the proxy history with the Burp MCP and review each request:

- `mcp__burp__get_proxy_http_history` (paged: `count`/`offset`)
- `mcp__burp__get_proxy_http_history_regex` with `regex: ":7080/"` to focus API calls
- `mcp__burp__send_http1_request` to re-probe an endpoint directly (e.g. confirm a
  401/403 on a protected route, or replay a create payload)

Per request, check and assign a verdict **OK / WARNING / ISSUE**:

| Check | What to confirm |
|---|---|
| Status | matches expectation (200/201/204; 401/403 where intended) |
| Auth header | `Authorization: Bearer <jwt>` present on every `:7080` call |
| Content-Type | `application/json` (or `multipart/form-data` for file uploads) |
| Request body | all expected fields present, none malformed |
| **Response body** | correct data; no error hidden in a 200; no unexpected null/missing/extra fields |
| Timing | nothing > 5s |
| Sequence | login precedes protected calls; no duplicate/leaked calls |

Present results as a request-by-request table. **If any request is an ISSUE, do
not report success** — go to the fix loop.

### 2. DB verification (Postgres MCP)

Use `mcp__postgres-full__execute_query` against `AttendanceDb` (and
`mcp__postgres-full__describe_table` for column names). Per-module tables are in
`fixtures/test-data.ts` (`DB_TABLES`). Confirm:

- created rows exist with expected columns; `is_deleted = false`; audit fields set
- deletes are **soft** (`is_deleted = true`), not hard
- no duplicate / orphaned rows

Example (shifts):
```sql
SELECT id, name, is_active, is_deleted, created_at
FROM "Shifts" WHERE name = 'E2E Shift 1234567';
-- cleanup
UPDATE "Shifts" SET is_deleted = true WHERE name = 'E2E Shift 1234567';
```

> Prerequisite: confirm the `postgres-full` MCP is pointed at port **5435 /
> AttendanceDb**. If it targets a different DB, reconfigure it first.

### 3. Fix / retry (max 5)

On a failed assertion **or** a Burp ISSUE **or** unexpected DB state: find the root
cause (read the source), fix it, and re-run just that module spec. Cross-reference
test failure ↔ Burp response ↔ DB row. Stop after 5 iterations and report findings.

### 4. Report

Per module: test results + the request-by-request Burp verdict table + DB findings.

## Notes, findings & gotchas

- **Stability tip:** start one dev server yourself (`npm run dev`) and let the
  suite reuse it. Playwright auto-starts one otherwise, but rapid repeated runs
  can leave a stale/zombie `next dev` that the reuse health-check rejects (it
  then spawns a second on :3001 and times out). If a run mass-fails with
  "Cannot navigate to invalid URL" or webServer timeouts: `pkill -f "next dev"`,
  start a fresh `npm run dev`, wait for `:3000`, re-run.
- **Why no `networkidle`:** the Next dev server keeps an HMR websocket open and
  compiles routes on first hit, so `waitForLoadState('networkidle')` never
  settles reliably. The Page Objects use `domcontentloaded` + element waits, and
  `ShellPage.goto` reloads once if Turbopack's transient build-error overlay
  (`<nextjs-portal>`) appears mid-compile.
- **Finding already surfaced (triage):** on `/attendance/view-all-attendance`,
  the browser fires `GET :7080/employees?...` and gets **401** — the page still
  renders because the server-side client swallows the error into an empty list.
  Reported via the diagnostics annotation; confirm in Burp and trace the missing
  bearer on that browser-side call.
- **Capturing localhost in Burp:** Chromium skips the proxy for loopback by
  default, so `localhost:7080` calls never reach Burp. The config disables this
  with `--proxy-bypass-list=<-loopback>` whenever a proxy is set — that's what
  makes the browser's API calls (incl. the `Authorization: Bearer` POSTs) show
  up in the proxy history.
- **Only browser-side API calls are captured.** Listing screens fetch their data
  **server-side** (Next server components via `axiosInstance`), so those GETs go
  out from the Node process and do NOT appear in Burp. What you review in Burp is
  the **browser** traffic: auth, create/update/delete XHR, and any client-side
  fetches. To review a server-side endpoint, replay it with
  `mcp__burp__send_http1_request` or hit it via Swagger.
- **Tests vs review are separate gates** (as in the skill): a green test run
  means screens render and flows complete; per-request 401/500 verdicts come from
  the Burp review phase, fed by the diagnostics annotations.

## Module → spec → DB table

| Module | Spec | Primary table(s) |
|---|---|---|
| Auth / route-guard | `auth.spec.ts` | `"user"`, `"SecurityAuditLogs"` |
| Dashboard | `dashboard.spec.ts` | — (read-only stats) |
| Attendance | `attendance.spec.ts` | `"Attendances"`, `"AttendanceBreaks"` |
| Attendance logs | `attendance-logs.spec.ts` | `"AttendanceLogs"` |
| Employees | `employees.spec.ts` | `"Employees"`, `"user"` |
| Schedules | `schedules.spec.ts` | `"AttendanceSchedules"`, `"ScheduleDays"` |
| Shifts | `shifts.spec.ts` | `"Shifts"` |
| Leave | `leave.spec.ts` | `"Leaves"` |
| Organizational units | `organizational-units.spec.ts` | `"OrganizationalUnits"` |
| Reports | `reports.spec.ts` | — (read-only) |
| Users & permissions | `system-users-permissions.spec.ts` | `"user"`, `"permission"`, `"user_permission"` |
| Devices | `system-devices.spec.ts` | `"Devices"` |
| Profile | `profile.spec.ts` | `"user"` |
