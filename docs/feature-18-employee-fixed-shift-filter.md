# Feature 18 — «الدوام الثابت» indicator + filter on the employee list

> Surface, on the main employee table, which employees still lack a fixed weekly
> shift pattern (تثبيت الدوام), let admins filter by has / hasn't, and assign one
> in a click.

## 1. The problem it solves

The fixed weekly pattern (feature 14, `EmployeeWeeklyShifts`) was only visible on
the dedicated «الدوام الثابت» tab and the employee detail page. From the main
employee list there was no way to tell who was still unassigned, and no way to
filter to just those employees to work through them. This adds the status to the
list itself.

## 2. What changed

| Layer | Change |
|---|---|
| `GET /employees` response | New `hasFixedShift` boolean per row (true ⇔ the employee has ≥1 `EmployeeWeeklyShifts` row). |
| `GET /employees` filter | New optional `HasFixedShift` (bool) query param — `true` returns only employees with a pattern, `false` only those without. |
| Employee table (UI) | New «الدوام الثابت» column with a مثبت / غير مثبت badge, a toolbar faceted filter driving `?hasFixedShift=`, and a row action «تثبيت الدوام / تعديل الدوام الثابت» linking to `/schedule/assign-shifts` pre-filtered to that employee. |

## 3. Backend implementation

Mirrors the existing `IsManager` filter — all in
`src/Application/Features/Organizations/Employees/Get/`:

- `GetEmployeesQuery` — `public bool? HasFixedShift { get; set; }`.
- `GetEmployeesQueryHandler` — applies `e.WeeklyShifts.Any()` / `!e.WeeklyShifts.Any()`
  before the count (so totals reflect the filter), and projects
  `HasFixedShift = e.WeeklyShifts.Any()` into `EmployeeResponse`. `WeeklyShifts`
  is the existing `Employee` nav collection; EF translates this to a SQL `EXISTS`.
- `EmployeeResponse` — `public bool HasFixedShift { get; set; }`.
- The endpoint (`Web.Api/Endpoints/Employees/Get.cs`) is unchanged — it binds
  `[AsParameters] GetEmployeesQuery`, so `?HasFixedShift=` is picked up
  automatically (model binding is case-insensitive, so the frontend's
  `?hasFixedShift=` binds too).

Reads are already scoped to the caller's accessible unit tree for non-Admins, so
the flag and filter respect that scope with no extra work.

## 4. Frontend implementation

- `features/employee/types/employees.ts` — `hasFixedShift: boolean` on `EmployeeData`.
- `features/employee/components/employee-tables/columns.tsx` — the badge column
  (`id: 'hasFixedShift'`, `meta.variant: 'select'` with `مثبت` / `غير مثبت`
  options so the toolbar renders a faceted filter), and the assign row action,
  gated to the roles that can assign a pattern (Admin, SuperAdmin, and
  OrgSupervisor once feature 17 lands — carried as the numeric `12`).
- `lib/searchparams.ts` — registers `hasFixedShift` so the server page can read it.
- `features/employee/components/employees-listing.tsx` — forwards the filter,
  normalizing the faceted selection to a single `true`/`false` (both / none = no filter).

## 5. Tests

`testing/e2e/specs/feature18-employee-fixed-shift-filter-{api,ui}.spec.ts` —
run with `npm run test:e2e:feature18` (`-api` / `-ui` variants exist).

- **API**: every row carries a boolean `hasFixedShift`; `true` + `false` counts
  partition the full list and `true` matches the DB's distinct-with-pattern
  count; assigning then clearing a pattern flips the subject between the two
  filters (verified via `searchTerm`).
- **UI** (real browser, seed.admin): the «الدوام الثابت» column and its status
  badges render, and picking «غير مثبت» in the toolbar filter pushes
  `?hasFixedShift=false` and reloads server-side.
