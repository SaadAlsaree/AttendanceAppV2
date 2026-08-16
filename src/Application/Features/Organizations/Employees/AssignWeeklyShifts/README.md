# Assign Weekly Shifts to Employee (تثبيت الدوام — feature 14)

## Description

Assigns an employee's fixed weekly shift pattern — one shift per day of the week — replacing
the need to regenerate attendance schedules periodically. The pattern is a standing rule with
no date range: attendance processing and check-in fall back to it whenever no `ScheduleDay`
covers the date.

## Operations

- **Command**: `AssignWeeklyShiftsCommand` — `Id` (employee) + `Days` (list of `{ DayOfWeek, ShiftId }`)
- **Handler**: `AssignWeeklyShiftsCommandHandler`
- **Validator**: `AssignWeeklyShiftsCommandValidator`
- **Endpoint**: `PUT employees/{id:guid}/weekly-shifts` (Admin/SuperAdmin, per-user rate limit)

## Semantics

- **Full replace**: the submitted list becomes the employee's entire pattern; an empty list
  clears it. Existing rows are hard-deleted and re-inserted.
- `DayOfWeek` uses the .NET convention: **0 = Sunday (الأحد) … 6 = Saturday (السبت)**
  (`System.DayOfWeek`, NOT the 1-based `Domain.Enums.DayOfWeek`).
- A weekday not present in the pattern is a day off — no shift is resolved for it.
- If today's attendance row exists and is still untouched (`Pending`, no check-in, not
  approved, `AttendanceScheduleId == null`), its `ShiftId` is updated immediately so the new
  pattern applies the same day.

## Business Rules

- Employee must exist.
- Every referenced shift must exist, not be soft-deleted, and be **active**
  (`ShiftErrors.NotFound` / `ShiftErrors.InactiveShift`).
- Each weekday may appear at most once; `DayOfWeek` must be 0–6.
- Schedules always take precedence over the pattern at resolution time
  (see `Application.Attendance.Shared.ShiftResolution`):
  `ScheduleIssue → ScheduleDay → weekly pattern → none`.
- A shift referenced by any pattern cannot be deleted (`Shift.CannotDeleteInUse`) or
  deactivated (`Shift.CannotUpdateInUse`).

## Related

- Entity: `Domain.Entities.Organizations.EmployeeWeeklyShift`
  (table `EmployeeWeeklyShifts`, unique `(employee_id, day_of_week)`).
- Consumers of the resolution: `AttendanceProcessingService` (Hangfire),
  `CheckInCommandHandler`, dashboard `LateToday`.
- Read model: `GetEmployeeByIdVm.WeeklyShifts`.
- Spec: `original/feature-requests/14-direct-shift-assignment.md` (heading «ادارة الجداول»).
