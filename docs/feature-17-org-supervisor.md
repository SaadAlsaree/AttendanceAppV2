# Feature 17 — مشرف جهة (OrgSupervisor role)

> A scoped operations role that runs schedules, fixed weekly patterns and
> attendance **for its own organizational unit tree only** — with no access to
> reports, or to user, unit, device or shift administration.

## 1. The problem it solves

Only global **Admin/SuperAdmin** could run daily operations (assign schedules,
fix weekly patterns, manual check-in/out, edit/approve attendance).
**Manager** is read-only. There was no way to delegate day-to-day
operations for a single directorate to someone who should see and touch **only
their own organization** (their unit + all sub-units) and nothing else.

`OrgSupervisor` (Arabic **مشرف جهة**, `Role = 12`) fills that gap.

## 2. What the role can and cannot do

| Area | OrgSupervisor gets (own unit tree) | Stays closed |
|---|---|---|
| Employees | view list / search / detail / weekly-shifts | edit, delete, assign-manager, roles, passwords |
| تثبيت الدوام | assign / change / clear fixed patterns | — |
| الجداول (schedules) | view + create / update / update-days | delete (SuperAdmin), bulk insert |
| Attendance | view + manual check-in/out, create, update, approve | delete (SuperAdmin) |
| Reports | — | **all** reports (admin/manager surface; risk of whole-org exposure) |
| Dashboard | dashboard + stats + quick-stats (scoped to own tree) | — |
| Leaves / Shifts / Org units | view only (pickers) | any writes / CRUD |
| Users, Devices, Files, Logs | — | everything |

"Own unit tree" = the supervisor's `OrganizationalUnitId` plus every descendant
unit, resolved by the existing `IHasPermission.GetAccessibleUnitIdsAsync`.

## 3. How enforcement works

Two layers, both server-side. The frontend gates only hide UI; the API enforces.

### a) Read scoping (already existed, inherited for free)
Every list handler already scopes non-Admin callers to
`GetAccessibleUnitIdsAsync`. Granting `OrgSupervisor` on the read endpoints makes
`GET /employees`, attendance lists, etc. return only the caller's sub-tree — no
new code. The shell-critical common endpoints (`GET /users/me` — without it the
sidebar can't read the role and falls open, attendance logs, holidays, employee
profile, self change-password) are granted too so the app loads for the role.

### b) Write ownership (the new primitive)
Before this feature **no write endpoint checked units** — role alone decided
access, so a scoped role would have been fake (anyone with it could write to any
employee by id). Added:

```csharp
// IHasPermission
Task<bool> CanManageEmployeeAsync(Guid employeeId, CancellationToken ct = default);
```

Admin/SuperAdmin → always true (zero behaviour change). Any other role → the
employee's `OrganizationalUnitId` must be inside the caller's accessible tree;
an employee with no unit is unreachable. On failure the handler returns
`EmployeeErrors.AccessDenied` → `Error.Forbidden` → **HTTP 403**.

It is called in the **9 employee-write handlers**: `AssignWeeklyShifts`,
schedule `Create` / `Update` / `UpdateScheduleDays`, attendance `CheckIn` /
`CheckOut` / `Create` / `Update` / `Approve`. (The background attendance worker
and device-sync path build rows directly, not through these command handlers, so
they are unaffected.)

### c) Reports are NOT granted; the dashboard is (and is scoped)
Reports are an admin/manager surface and stay **fully closed** to OrgSupervisor
— every report endpoint (`attendance-summary`, `organization`,
`organizational-summary`, `overtime`, `employee`) returns **403**, and the whole
التقارير / التقارير والتحليلات sidebar is hidden. This is deliberate: several
report handlers are caller-blind and default to the *whole organization* when no
unit filter is passed (e.g. `organizational-summary` returns all ~5,200
employees), so exposing them to a scoped role would leak other directorates.
The unit-scoped view the supervisor needs lives in the dashboard instead.

The **dashboard IS granted**, scoped **only when `Role == OrgSupervisor`**
— deliberately not `!= Admin`, because SuperAdmin often has no unit and would
otherwise get empty results:

- `GetDashboardStatsQueryHandler` — rejects a `DepartmentId` outside the tree
  (403) and filters every section (attendance, trends, department, leave,
  performance, top-performers) to the tree. Device stats stay global.
- `GetQuickStatsQueryHandler` — 403 if the requested `OrganizationId` is outside
  the tree.

## 4. Security fix riding along

`PUT /attendance-schedules/{id}` and `PUT /attendance-schedules/{id}/schedule-days`
previously allowed the **`Employee`** role — a pre-existing privilege hole (any
employee could rewrite schedules via the API; the UI never exposed it). The new
role lists are `[Admin, SuperAdmin, OrgSupervisor]`. A regression test asserts a
plain Employee now gets 403.

## 5. Assigning the role

`OrgSupervisor = 12` is a plain enum value stored as a string, so it is
assignable immediately from the existing users screen (or `POST /users/new` with
`role: 12` + the target `organizationalUnitId`). No migration — no schema change.

## 6. Testing

Backend E2E (self-contained, creates its own fixture user — **no seed
dependency**):

```
cd .local/testing && npm run test:e2e:feature17
```

`feature17-org-supervisor-api.spec.ts` pins a supervisor to a unit that has both
a sub-tree of employees and a foreign employee, then proves the boundary both
ways: scoped employee list (foreign absent), own-tree writes succeed / foreign
writes 403 (weekly-shifts + schedule), admin-only surfaces 403 (POST /shifts,
GET /users, employee edit, user creation), **all reports 403**, dashboard granted
but scoped (foreign quick-stats 403), and the Employee-role schedule-update
regression. `feature17-org-supervisor-ui.spec.ts` logs in through the browser and
checks the sidebar hides System Settings **and the reports sections**, the write
screens open, and report deep-links redirect away.

## 7. Changed files (for review)

**Backend** — `RolesEnum.cs` (+OrgSupervisor), `IHasPermission` /
`HasPermission` (+`CanManageEmployeeAsync`), `EmployeeErrors` (+`AccessDenied`),
the 9 write handlers, the endpoint role lists under `Web.Api/Endpoints/`
(incl. the schedule-update `Employee` removal; reports NOT granted), and the two
dashboard handlers (`GetDashboardStats`, `GetQuickStats`).

**Frontend** — `users-permissions.ts` (enum + display name), `constants/data.ts`
(nav `requiredRoles`; no report sections), and page/button gates under
`app/(routes)/` for schedule, attendance and employee.

## 8. Notes / left as-is (pre-existing)

- Manager/Employee can still query arbitrary units on the caller-blind report
  paths that this feature did not touch — out of scope, flagged only.
- Dashboard **device** stats remain global for the supervisor (not employee
  data); noted intentionally.
