# Feature 19 — Sites (الموقع) and the `SiteSupervisor` role

## 1. Why

Access scoping was tied entirely to the **organizational unit tree**: `OrgSupervisor`
([feature-17](./feature-17-org-supervisor.md)) sees its own unit and everything beneath it. There was
no way to put units from *different branches* under one supervisor.

A **Site** is an administrative grouping of units whose membership is **explicit and
non-transitive** — assigning a unit to a site does not bring its child units along. `SiteSupervisor`
(مشرف موقع, value 13) is a **view-only** role scoped to its site's unit list.

A site is not a `WorkLocation`. That entity remains the geofence (lat/long/radius) used to verify
where a check-in physically happened, and is untouched by this feature.

## 2. Domain

| Change | File |
|---|---|
| `Role.SiteSupervisor = 13` (`[Display(Name = "مشرف موقع")]`) | `Domain/Enums/RolesEnum.cs` |
| New `Site : AuditableEntity<Guid>` — `SiteName`, `SiteCode`, `Description`, `Address`, `IsActive` | `Domain/Entities/Organizations/Site.cs` |
| New `SiteErrors` | `Domain/Entities/Organizations/SiteErrors.cs` |
| `OrganizationalUnit.SiteId` (nullable) | `Domain/Entities/Organizations/OrganizationalUnit.cs` |
| `User.SiteId` (nullable) | `Domain/Entities/Users/User.cs` |

Role is persisted as a **string**, so the new member needs no data migration.

## 3. Database

Migration `20260905120954_AddSites` — additive and zero-downtime:

- table `Sites` (audit columns from `AuditableEntity`), unique index on `site_code`
- nullable `site_id` on `Users` and `OrganizationalUnits`, both FKs `ON DELETE SET NULL`
- indexes `ix_users_site_id`, `ix_organizational_units_site_id` — every scoped request filters on
  `OrganizationalUnits.site_id`

Deleting a site orphans its units rather than cascading into the org tree. In practice the delete
handler refuses while any unit or user still points at the site.

## 4. The scoping change — one method

`Infrastructure/Authentication/HasPermission.cs :: GetAccessibleUnitIdsAsync` is the single
funnel through which ~25 read handlers scope their queries. It gains one branch:

```csharp
if (user.Role == Role.SiteSupervisor)
{
    if (user.SiteId is not { } siteId) { return accessibleUnitIds; }   // empty ⇒ sees nothing
    ...Where(ou => ou.SiteId == siteId && !ou.IsDeleted).Select(ou => ou.Id)
}
```

Four properties, each deliberate:

1. **It never descends the tree.** No `GetAllSubUnitIdsAsync`. This is the feature's definition —
   descending would make a one-unit site equivalent to an `OrgSupervisor`.
2. **It ignores `user.OrganizationalUnitId`.** A row may still carry one from a previous role;
   unioning it would silently widen the scope past the site.
3. **No site ⇒ empty set, never "everything".** All consumers are fail-closed (`Contains` on an
   empty set is `false`; `GetEmployeesQueryHandler` and `GetAttendanceLogsQueryHandler` return
   `PaginatedResponse.Empty`).
4. **It filters `!IsDeleted`** — stricter than the legacy tree walk, which does not.

**`CanManageEmployeeAsync` denies the role outright.** Without that deny it would fall through to
the accessible-unit check and authorize every write inside the site. This is the only structural
backstop against the role being added by mistake to one of the 91 endpoint role lists.

Everything else scoped itself for free: attendance, **not-attendance (غير المبصمين)**, attendance
logs, leaves, employees, schedules, users, and the six `Attendance/Reports/*` handlers all already
had `if (role != Admin) { …GetAccessibleUnitIdsAsync… }`.

## 5. Pre-existing holes fixed as a precondition

Granting a scoped role to these endpoints without fixing them first would have leaked the whole
organization. All four were verified against the source before changing anything.

| Handler | Was | Now |
|---|---|---|
| `Reports/GetOrganizationalSummary` | **No role check at all.** Loaded every unit; `totalEmployees` was an unfiltered global `CountAsync` for *every* caller | Resolves `_scopedUnitIds` up front and routes every unit-set derivation through it; forbids an out-of-scope unit; skips the `UnitLevel 1..3` filter for scoped callers (a site may hold a level-4 unit) |
| `Organizations/Employees/Search` | No scoping — whole employee directory | Standard `!= Admin` + accessible-units filter |
| `Organizations/Employees/GetById` | No scoping — any employee by id | Accessible-unit containment; returns `NotFound`, not `AccessDenied`, so existence is not confirmed |
| `OrganizationalUnits/GetAsTree :: BuildTree` | Dropped any unit whose parent was filtered out | Orphans become roots — otherwise a `SiteSupervisor`, whose units are scattered by design, got an **empty tree** |

The first three were already reachable by `OrgSupervisor`/`SecurityOfficer`; this feature did not
create them, but could not deepen them either.

Also corrected while scoping `GetOrganizationalSummary`: its four top-level totals filtered on
`unitIds.Contains(user.OrganizationalUnitId)` — a per-request constant rather than a per-row
predicate, so they evaluated to 0 for any caller whose own unit was not in the report. They now
filter on the row's own employee unit, matching `BuildUnitSummary`.

## 6. Handlers with an explicit branch

- `Reports/GetOrganizationReport` — new `HandleSiteSupervisorAsync`. The existing path always builds
  from `user.OrganizationalUnitId`, so a supervisor (who has none) got `NotFound`. The new branch
  builds from the flat site list; `BuildOrganizationReportAsync` already accepted a flat
  `List<Guid>`, so nothing below the entry point changed. **No sub-unit descent.**
- `Dashboard/GetDashboardStats` and `Dashboard/GetQuickStats` — guard widened to
  `is Role.OrgSupervisor or Role.SiteSupervisor`. Both already used flat-set semantics. Device stats
  stay global, same as feature-17.
- `Reports/GetAttendanceReport` — guard widened **defensively only**. The endpoint is deliberately
  **not** granted to the role: `query.IncludeSubUnits` and `BuildSubUnitStatisticsAsync` both walk
  the requested unit's children straight from the DB. Fix those two before granting it.

## 7. API

New `Web.Api/Endpoints/Sites/`:

| Route | allowedRoles |
|---|---|
| `GET sites`, `GET sites/{id}` | Admin, SuperAdmin, Manager, **SiteSupervisor** |
| `POST sites`, `PUT sites/{id}`, `DELETE sites/{id}`, `PUT sites/{id}/units` | Admin, SuperAdmin |

The two reads let the UI name the supervised site; the handlers restrict a `SiteSupervisor` to its
own site. `PUT sites/{id}/units` is admin-only on purpose — granting it would let the role widen its
own access scope.

`"SiteSupervisor"` was added to **24 GET endpoints** plus `Users/ChangePassword` (a `POST`, but
self-service: it requires the current password, and feature-17 granted it for the same reason).
Audit command:

```bash
for p in $(grep -rl '"SiteSupervisor"' src/Web.Api/Endpoints --include=*.cs); do
  grep -q "app\.MapGet" "$p" || echo "NON-GET: $p"
done
```

`Users/ChangePassword.cs` must be the only line of output.

## 8. Membership is its own command

`SetSiteUnitsCommand(SiteId, OrganizationalUnitIds)` replaces membership wholesale in one atomic
save. `SiteId` is deliberately **not** added to `UpdateOrganizationalUnitCommand`: that handler
assigns every property unconditionally, so the existing org-unit form — which knows nothing about
sites — would silently clear `SiteId` on every unit edit.

The same trap applies to `UpdateUserCommandHandler`, which is why the user form always sends
`siteId`.

`SiteId` is required when the role is `SiteSupervisor`, validated in both `SignUpCommandHandler` and
`UpdateUserCommandHandler` — the runtime empty-set fallback is the backstop, not the guarantee.

## 9. Frontend

| Change | File |
|---|---|
| `SiteSupervisor = 13` + `"مشرف موقع"` | `features/system/users-permissions/types/users-permissions.ts` |
| **`VIEW_ONLY_ROLES`** — the single highest-impact line | `utils/auth/auth-utils.ts` |
| Nav `requiredRoles` + «المواقع» entry | `constants/data.ts` |
| Page view gates (not `canCheckIn`, not `canWrite`) | `app/(routes)/{attendance,employee,reports}/**` |
| Site selector, required for role 13 | `features/system/users-permissions/components/users-permissions-form.tsx` |
| Sites management UI | `features/system/sites/**`, `app/(routes)/system/sites/**` |

`canWrite()` is `!isViewOnly()` — **allow-by-default**. Omitting the `VIEW_ONLY_ROLES` line would
hand the role every create/edit/delete button in the app.

`not-attendance` and `attendance-logs` had **no server-side role check at all** and were reachable
by direct URL for any authenticated user. They now carry explicit gates.

The unit picker in the site form uses a flat checkbox list with **no cascade selection** — ticking a
parent must not tick its children — and shows each unit's `parentUnitName` to disambiguate
same-named units in different branches.

## 10. Verification

```bash
cd AttendanceAppV2   && dotnet build AttendanceApp.sln -c Release   # 0 errors
cd attendance-frontend && npx tsc --noEmit && npm run build          # 0 errors
```

Manual, in order — step 3 is the assertion that defines the feature:

1. Admin → `/system/sites` → create "موقع الشمال" (`NORTH`).
2. Assign **two units from different branches, each having children**.
3. **Re-open the site: only those two are ticked, none of their children.**
4. Create a user with role «مشرف موقع» + that site and **no organizational unit** (the form must
   allow this).
5. As that user: `/employee` lists employees of the two units and **nobody from a child unit**. No
   add/edit/delete buttons anywhere.
6. `/attendance/not-attendance` — same population.
7. `/reports/organizational-summary` — **critical**: total employees equals the site headcount, not
   the whole-org count. If it shows the global number, §5 was not applied; un-grant the endpoint.
8. `GET /organizational-units/tree` returns the two units as roots, **not `[]`**.
9. With the role's bearer token: `PUT /attendance/{id}/approve`, `POST /leaves`,
   `PUT /sites/{id}/units` → all **403**. `GET /employees/{id}` for an employee in a child unit →
   404, not 200.
10. Null the supervisor's `SiteId` → every list is empty, nothing 500s (fail-closed check).

## 11. Known gaps

- `Reports/GetAttendanceReport` is not granted to the role (two tree descents, §6).
- `OrganizationalUnits/GetById` still has its unimplemented scoping TODO. It exposes unit metadata
  only — no employee data — so it was granted knowingly.
- Dashboard **device** statistics remain global for the role, same as feature-17.
- `Users/ChangePassword` is a write. It is self-service (requires the current password), but if a
  strict no-writes-whatsoever reading is preferred, remove `"SiteSupervisor"` from
  `Endpoints/Users/ChangePassword.cs` — the role then cannot change its own password.
