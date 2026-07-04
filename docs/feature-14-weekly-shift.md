# Feature 14 — تثبيت الدوام (Fixed Weekly Shift Pattern)

> Spec (source of truth), from `original/feature-requests/14-direct-shift-assignment.md`:
> **«ادارة الجداول: تثبيت دوام الموظف بشكل مباشر دون الحاجة الى خلق جدول تلقائي كل 10 ايام،
> لان اغلب الموظفين دوامهم ثابت. وامكانية تغيير دوامه بشكل يدوي في حالة التغيير الى مسائي او
> خفر، وهذا الاجراء نادر مايحدث.»**

Pin an employee's working shift **per day of the week** — once — instead of regenerating
schedules every ~10 days. Implemented 2026-07-04 on branch `feature/14-direct-shift-assignment`
(both repos).

---

## 1. The problem it solves

Before this feature, an employee's shift was derivable **only** from `ScheduleDays` rows inside
date-ranged `AttendanceSchedules`. When admins stopped regenerating schedules (last real batch
ended 2026-02-28), the whole measurement chain died:

| Symptom | Cause |
|---|---|
| Check-in failed `Attendance.MissingScheduleOrShift` for everyone | no `ScheduleDay` covers the date |
| 100% of attendance rows since March 2026 had `shift_id = NULL` | Hangfire job had nothing to resolve from |
| Lateness, work hours, غير المبصمين, dashboard «متأخرين» all read zero/empty | they all need the shift snapshot |

Data point: 96% of all schedules ever created used ONE shift for every day — the per-day
schedule machinery was pure overhead for almost everyone.

## 2. The concept

Each employee can have a standing **weekly pattern**: one shift per weekday, e.g.
الأحد–الأربعاء = صباحي (9-4), الخميس = خفر, الجمعة/السبت unassigned (= يوم راحة).

- **No start/end date. No regeneration. No admin routine.** The pattern applies every week
  until an admin changes or clears it.
- A weekday **without** a pattern row is a **day off** — nothing is invented (there is no
  hardcoded Friday/Saturday rule; a خفر guard *can* be assigned Friday/Saturday).
- **Schedules still exist and always win.** They are now the *exception* mechanism: a
  temporary schedule (e.g. two weeks of خفر) overrides the pattern for its date range, and the
  employee falls back to his pattern automatically when it ends.

### Shift resolution — one rule everywhere

For a given employee and date:

```
ScheduleIssue (exception)  →  active ScheduleDay  →  weekly pattern row for that weekday  →  none
```

- If schedules cover the date but ALL exclude it (`ExcludedDates`) → **none** (explicit day off
  beats the pattern).
- Implemented once in `Application.Attendance.Shared.ShiftResolution` (pure static,
  delegate-based) and used by all three consumers, so the logic cannot drift:
  1. `AttendanceProcessingService.CreateAttendanceRecordsAsyncIfNotExists` — the Hangfire job
     (every 5 min) that pre-creates the day's attendance rows.
  2. `CheckInCommandHandler` — device/UI check-in (previously hard-failed without a schedule).
  3. Dashboard `LateToday` — now counts the stored snapshot (`CheckInTime != null && LateMinutes > 0`)
     instead of live-recomputing from lapsed ScheduleDays.
- `Attendance.ShiftId` remains a **snapshot** written at row creation — changing a pattern
  never rewrites past attendance. (Reports already read this snapshot; they needed no change.)

## 3. Data model

New table `public."EmployeeWeeklyShifts"` (migration `20260703212340_AddEmployeeWeeklyShifts`):

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | PK |
| `employee_id` | uuid | FK → Employees, **cascade** delete |
| `day_of_week` | int | **.NET convention: 0=Sunday(الأحد) … 6=Saturday(السبت)** |
| `shift_id` | uuid | FK → Shifts, **restrict** delete |
| + standard audit columns | | `created_at/by`, `last_updated_*`, `is_deleted`, … |

- **Unique index** `(employee_id, day_of_week)` — max one shift per weekday per employee.
- Replacement is a hard delete+insert (this codebase has no soft-delete interceptor), so the
  unique index never collides.
- Deliberately used `System.DayOfWeek` (0-based) end-to-end, NOT the 1-based
  `Domain.Enums.DayOfWeek`, so resolution compares directly with `date.DayOfWeek` — no
  off-by-one conversions anywhere.
- The orphaned `Employees.shift_id` column (from InitialCreate, never mapped, all NULL) is left
  untouched; removing it is a separate cleanup.

## 4. API

### `PUT /employees/{id:guid}/weekly-shifts` — assign / replace / clear

Roles: **Admin, SuperAdmin**. Rate limit: per-user. **Full-replace semantics** — the submitted
list becomes the employee's entire pattern.

```jsonc
// assign: الأحد-الأربعاء صباحي، الخميس خفر
{ "days": [
    { "dayOfWeek": 0, "shiftId": "<صباحي-guid>" },
    { "dayOfWeek": 1, "shiftId": "<صباحي-guid>" },
    { "dayOfWeek": 2, "shiftId": "<صباحي-guid>" },
    { "dayOfWeek": 3, "shiftId": "<صباحي-guid>" },
    { "dayOfWeek": 4, "shiftId": "<خفر-guid>" }
] }

// clear the whole pattern
{ "days": [] }
```

Responses / errors:

| Result | Status | Code |
|---|---|---|
| Saved | 204 | — |
| Duplicate weekday / dayOfWeek outside 0–6 / empty shiftId | 400 | `Validation.General` |
| Employee not found | 404 | `Employees.NotFound` |
| Shift not found / soft-deleted | 404 | `Shift.NotFound` |
| Shift inactive | 400 | `Shift.Inactive` |

Side effect: if **today's** attendance row exists and is still untouched (`Pending`, no
check-in, not approved, not schedule-sourced), its `shift_id` is updated immediately to the new
pattern — the change takes effect the same day, not tomorrow.

### `GET /employees/{id}` — read

The employee VM now includes:

```jsonc
"weeklyShifts": [
  { "dayOfWeek": 0, "shiftId": "…", "shiftName": "صباحي (9-4)",
    "startTime": "09:00:00", "endTime": "16:00:00" },
  …
]
```

### Guards on shifts

A shift referenced by any weekly pattern can no longer be:
- **deleted** → 400 `Shift.CannotDeleteInUse` (`DeleteShiftCommandHandler`)
- **deactivated** (`isActive: true → false`) → 400 `Shift.CannotUpdateInUse` (`UpdateShiftCommandHandler`)

## 5. UI

| Screen | Path | What it does |
|---|---|---|
| **إدارة الجداول ← تثبيت الدوام** | `/schedule/assign-shifts` | Admin-only. Searchable employee picker (server-side, all employees, by name **or** code — schedule-form pattern), quick-fill «تطبيق على كل الأيام» (fills الأحد–الخميس), seven per-day selects with «بدون دوام (راحة)», «تثبيت الدوام» save + «مسح الدوام الثابت» clear. Re-selecting an employee preloads his saved pattern. |
| **Employee detail page** | `/employee/{id}` | Read-only «الدوام الثابت» block: each assigned day with shift name and hours. |
| **عرض جميع الحضور** | `/attendance/view-all-attendance` | The «تسجيل الحضور لموظف» manual check-in button was re-enabled (admin-only) — previously commented out. Rows now show المناوبة/دقائق التأخير for patterned employees. |

Placement note: the nav entry lives under **إدارة الجداول** (not إدارة الموظفين) because the
spec itself files item 14 under that heading, and the screen replaces schedule creation.

## 6. Testing

- **E2E** (extend, don't ad-hoc): `testing/e2e/specs/feature14-weekly-shift-api.spec.ts`
  (round-trip, validation negatives, guards, pattern-based check-in, clear) and
  `feature14-weekly-shift-ui.spec.ts` (quick-fill save, employee page display, preload + clear;
  Postgres as ground truth).
- Run: `cd testing && npm run test:e2e:feature14` (or `:feature14-api` / `:feature14-ui`).
- Manual smoke: assign a pattern that includes **today's weekday** → check in via the UI button
  → row shows the shift + lateness; an employee with no pattern still fails check-in
  (`MissingScheduleOrShift`) — that's the day-off contract.

## 7. Known quirks (pre-existing, unchanged by this feature)

- **UTC vs Baghdad day boundary**: the Hangfire job and dashboard use the UTC date while
  check-in uses Baghdad local — between 00:00–03:00 Baghdad they disagree about "today".
- **Lateness input format**: `AttendanceCalculationService` expects device-style timestamps;
  a raw UTC-naive value can inflate `LateMinutes`. The UI check-in dialog sends the right shape.
- `GET /employees` 500s without a `page` param, and returns the paginated array directly under
  `data` — relevant when writing new clients against it.

## 8. Changed files (for review)

**Backend** (`original/AttendanceAppV2`): `Domain/Entities/Organizations/EmployeeWeeklyShift.cs`*,
`Employee.cs`, `ShiftErrors.cs`, `Infrastructure/Configuration/Organizations/EmployeeWeeklyShiftConfiguration.cs`*,
`Infrastructure/Database/ApplicationDbContext.cs` (+ migration*), `Infrastructure/Services/AttendanceProcessingService.cs`,
`Application/Abstractions/Data/IApplicationDbContext.cs`,
`Application/Features/Attendance/Attendance/Shared/ShiftResolution.cs`*,
`…/Attendance/CheckIn/CheckInCommandHandler.cs`, `…/Dashboard/GetDashboardStats/GetDashboardStatsQueryHandler.cs`,
`…/Organizations/Employees/AssignWeeklyShifts/`* (command/validator/handler),
`…/Organizations/Employees/GetById/` (VM + handler), `…/Organizations/Shifts/{Delete,Update}/` (guards),
`Web.Api/Endpoints/Employees/AssignWeeklyShifts.cs`*.

**Frontend** (`original/attendance-frontend`): `app/(routes)/schedule/assign-shifts/page.tsx`*,
`features/employee/components/assign-weekly-shifts-form.tsx`*, `employee-view-page.tsx`,
`features/employee/{types/employees.ts, api/employees.service.ts}`, `constants/data.ts`,
`app/(routes)/attendance/view-all-attendance/page.tsx` (check-in button re-enabled).

\* = new file
