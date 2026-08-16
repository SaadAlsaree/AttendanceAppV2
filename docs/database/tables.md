# Tables Reference (public schema)

One section per table managed by `ApplicationDbContext`. Only **key columns** are listed: primary
keys, foreign keys, and business-meaningful fields. **Every table also has the audit columns from
`AuditableEntity<Guid>`** (`id` PK, `created_at/by`, `last_updated_at/by`, `is_deleted`,
`deleted_at/by`, `done_procdure_date`) — these are documented once in [`README.md`](./README.md) and
not repeated below. Soft deletes apply throughout (`is_deleted`).

Column names shown in `snake_case` (the DB convention). Table names follow the source: most are
PascalCase (explicit `ToTable`); the snake_case ones are flagged.

Enum-backed columns are stored as **strings** (`.HasConversion<string>()`).

---

## Users
Login accounts and their role/status. Table: `Users`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `user_login` | varchar(50) | **unique** login handle |
| `username` | varchar(100) | display name |
| `password_hash` | varchar(256) | PBKDF2-SHA512 `HEX(hash)-HEX(salt)` |
| `role` | varchar(50) | enum **`Role`** (Admin, User, Manager, Employee, Guest, HR_Manager, Viewer, SuperAdmin, SystemUser, SystemManager) |
| `status` | varchar(50) | enum **`UserStatus`** (Active, Inactive, Pending, Locked, Expired, Deleted, Suspended, Archived) |
| `is_active` | bool | |
| `is_default_password` | bool | forces password change on first login |
| `last_login_date` | timestamp | |
| `organizational_unit_id` | uuid FK → OrganizationalUnits | nullable, `SetNull` |

## Permission
Catalog of fine-grained permissions. Table: **`permission`** (snake_case — no explicit `ToTable`).

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `name` | varchar(100) | |
| `description` | varchar(255) | |
| `resource` | varchar(50) | the protected resource |
| `action` | varchar(50) | the allowed action |
| `is_active` | bool | |
| `metadata` | jsonb | extra key/value context |

## UserPermission
Join table for the User ↔ Permission many-to-many, with grant metadata. Table: **`user_permission`** (snake_case).

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `user_id` | uuid FK → Users | |
| `permission_id` | uuid FK → permission | |
| `expiry_date` | timestamp | nullable — time-limited grants |
| `is_active` | bool | |

## OrganizationalUnit
Self-referencing org tree (departments/units). Table: `OrganizationalUnits`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `unit_code` | varchar(20) | **unique** (e.g. HR, IT) |
| `unit_name` | varchar(100) | |
| `unit_description` | varchar(500) | nullable |
| `parent_unit_id` | uuid FK → OrganizationalUnits | self-ref parent, `Restrict` |
| `manager_id` | uuid FK → Employees | managing employee, `SetNull` |
| `unit_level` | int | depth in tree |
| `email` / `phone_number` / `address` / `postal_code` / `unit_logo` | varchar | contact / branding |

## Employee
Core workforce record (~5,217 rows). Table: `Employees`. `id` is a generated identity Guid.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK (identity-generated) |
| `emp_id` | string | **unique** business employee id |
| `code` | varchar(20) | nullable secondary code |
| `rfid` | varchar(50) | RFID card number (indexed) |
| `full_name` | varchar(256) | composed name (indexed) |
| `first/second/third/fourth/family_name` | varchar(50) | name parts |
| `email` | varchar(256) | **unique** |
| `is_manager` | bool? | |
| `face_image_url` / `national_id_front_url` / `national_id_back_url` / `profile_image_url` | varchar(500) | media |
| `organizational_unit_id` | uuid FK → OrganizationalUnits | `SetNull` |
| `manager_id` | uuid FK → Employees | self-ref manager (→ subordinates), `SetNull` |
| `user_id` | uuid FK → Users | optional 1:1 login link, `SetNull` |

## Shift
A work-time window assignable to employees and schedule days. Table: `Shifts`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `name` | string | |
| `shift_type` | string | enum **`ShiftType`** (Morning, Afternoon, Evening, Night, Flexible, Custom) |
| `start_time` / `end_time` | time | `TimeOnly` |
| `is_active` | bool | |
| `grace_period_minutes` | int? | tolerance before "late" |
| `max_late_minutes` | int? | |
| `allow_early_check_in` / `allow_late_check_out` | bool | |

## Holiday
Org-scoped holiday dates. Table: `Holidays`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `organization_id` | uuid FK → OrganizationalUnits | owning unit |
| `name` | string | |
| `date` | date | `DateOnly` |
| `is_recurring` | bool | repeats yearly |

## WorkLocation
Geofenced location for location-verified check-in. Table: `WorkLocations`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `organization_id` | uuid FK → OrganizationalUnits | owning unit |
| `name` / `address` | string | |
| `latitude` / `longitude` | double | geofence centre |
| `radius_meters` | int | allowed radius (Haversine check in domain) |
| `wifi_ssid` / `beacon_id` | string? | extra verification signals |
| `is_active` | bool | |

## Device
Biometric / fingerprint hardware registry. Table: `Devices`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `ip_address` | string | |
| `device_id` / `serial_number` / `mac_address` | string? | identity |
| `device_model` / `firmware_version` / `protocol` / `port` | string? | config |
| `is_active` | bool | |
| `last_connected` | timestamp? | |
| `organization_id` | uuid FK → OrganizationalUnits | nullable |

## AttendanceSchedule
A dated working plan for an employee that expands into days. Table: `AttendanceSchedules`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `employee_id` | uuid FK → Employees | `Restrict` |
| `start_date` | date | |
| `end_date` | date? | open-ended if null |
| `schedule_type` | string | enum **`ScheduleType`** (Regular, Rotating, Flexible, Custom) |
| `is_active` | bool | |
| `excluded_dates` | varchar(1000) | comma-joined `DateOnly` list (value-converted) |

## ScheduleDay
One concrete dated day of a schedule, pinned to a shift. Table: `ScheduleDays`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `attendance_schedule_id` | uuid FK → AttendanceSchedules | `Cascade` |
| `schedule_day_date` | date | |
| `shift_id` | uuid FK → Shifts | shift for that day |
| `is_active` | bool | |

## ScheduleIssue
Per-date exception/override on a schedule (apply a different shift). Table: `ScheduleIssues`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `attendance_schedule_id` | uuid FK → AttendanceSchedules | |
| `date` | date | the affected date |
| `shift_id` | uuid FK → Shifts | the substitute shift |
| `exception_type` | string | enum **`ExceptionType`** (Holiday, Overtime, Different_Shift, No_Work, Duty) |
| `reason` | string | |

## Attendance
Daily attendance fact, one per employee-per-day (~595k rows). Table: `Attendances`.
Unique index on `(employee_id, date)`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `employee_id` | uuid FK → Employees | `Restrict` |
| `organization_id` | uuid | denormalized org (no FK) |
| `date` | timestamptz | the attendance day |
| `check_in_time` / `check_out_time` | timestamptz? | |
| `status` | varchar(50) | enum **`AttendanceStatus`** (Present, Absent, Break, Vacation, Holiday, Late, Early_Out, Overtime, Duty, Exempted, Permitted, Pending) |
| `shift_id` | uuid FK → Shifts | `SetNull` |
| `attendance_schedule_id` | uuid FK → AttendanceSchedules | `SetNull` |
| `working_minutes` / `break_minutes` / `overtime_minutes` / `late_minutes` / `early_leave_minutes` | int? | computed durations |
| `check_in_method` / `check_out_method` | varchar(50) | enum **`LogMethod`** (Mobile_App, Web, Biometric, RFID_Card, NFC_Card, QR_Card, Manual_Entry, API) |
| `approved_by` / `approved_at` | uuid? / timestamptz? | approval audit |
| `notes` | varchar(500) | |

## AttendanceBreak
A break period within an attendance day. Table: `AttendanceBreaks`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `attendance_id` | uuid FK → Attendances | |
| `start_time` | timestamp | |
| `end_time` | timestamp? | null while in progress |
| `duration_minutes` | int | |
| `break_type` | string | enum **`BreakType`** (Compensatory, Holiday, Vacation) |

## AttendanceException
Manual correction/adjustment to an attendance record. Table: **`attendance_exceptions`** (snake_case).

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `attendance_id` | uuid FK → Attendances | |
| `exception_type` | string | enum **`ExceptionType`** |
| `start_date` / `end_date` | timestamp | adjustment window |
| `reason` | string | |
| `modifier_id` | uuid FK → Employees | who made the change |
| `approver_id` / `approved_by` / `approved_at` | uuid? / timestamp? | approval audit |
| `is_approved` | bool? | |
| `priority` | int? | |

## Leave
Leave / vacation requests. Table: `Leaves`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `employee_id` | uuid FK → Employees | |
| `leave_type` | string | enum **`LeaveType`** (None, Ordinary, Sick, Emergency, Maternity, TimeOff, Hajj, Umrah, Study, Unpaid, Compensatory, Duty, Night_Break, Permitted, Cycle, Workshop) |
| `status` | string | enum **`LeaveStatus`** (Pending, Approved, Rejected, Cancelled, Expired, UnderReview) |
| `start_date` / `end_date` | timestamp | |
| `reason` | string | |
| `approved_by` / `approved_at` | uuid? / timestamp? | |
| `rejection_reason` | string? | |

## Attachment
Files attached to an employee (IDs, photos, documents). Table: `Attachments`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `employee_id` | uuid FK → Employees | |
| `file_name` / `content_type` | string | |
| `blob_path` | string | path in blob storage (Azurite locally) |
| `type` | string | enum **`AttachmentType`** (NationalIdFront, NationalIdBack, Other) |
| `description` | string? | |

## SecurityAuditLog
Security/audit events (login, biometric match, location). Table: `SecurityAuditLogs`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `user_id` | uuid? FK → Users | actor |
| `employee_id` | uuid? FK → Employees | subject |
| `event_type` | string | e.g. LOGIN, LOGOUT, FAILED_LOGIN, BIOMETRIC_MATCH |
| `event_description` | string | |
| `severity` | string | Info / Warning / Error / Critical |
| `is_successful` | bool | |
| `failure_reason` | string? | |
| `ip_address` / `user_agent` | string? | |
| `device_id` | uuid? | originating device |
| `location_latitude` / `location_longitude` | double? | |
| `biometric_id` / `biometric_confidence` | uuid? / double? | match metadata |
| `timestamp` | timestamp | event time |

---

## AttendanceLog (unmapped to the relational model)
Flat landing table for raw biometric punches; **no foreign keys** — joined to `Employee` only by the
business string `emp_id` during downstream processing. Table: `AttendanceLogs`.

| Column | Type | Note |
|---|---|---|
| `id` | uuid | PK |
| `emp_id` | string | business employee id (not a FK) |
| `card_no` | string | RFID/card number |
| `date_time_attend` | timestamp | punch timestamp |
| `date_work` | date | working day |
| `time_attend` | interval | time component |
| `direct` | int | direction — 1=In, 2=Out (`DirectType`) |
| `device_name` / `device_no` | string | source device |

> The live source for these punches is the legacy MSSQL `dbo.EventTab` keyless table, accessed
> read-only via `ExternalAttendanceDbContext` (see [`README.md`](./README.md)).
