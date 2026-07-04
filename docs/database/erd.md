# Entity-Relationship Diagram (public schema)

The diagram below covers the first-party `public`-schema tables managed by `ApplicationDbContext`.
Audit columns inherited from `AuditableEntity<Guid>` (`created_at/by`, `last_updated_at/by`,
`is_deleted`, `deleted_at/by`) are omitted for clarity — see [`tables.md`](./tables.md) and
[`README.md`](./README.md). Relationships and cardinalities are taken from the Fluent configs under
`Infrastructure/Configuration/**` and `ApplicationDbContextModelSnapshot.cs`.

```mermaid
erDiagram
    OrganizationalUnit ||--o{ OrganizationalUnit : "parent_unit_id (self-ref)"
    OrganizationalUnit ||--o{ Employee : "employs"
    OrganizationalUnit ||--o{ User : "scopes"
    OrganizationalUnit ||--o{ Holiday : "organization_id"
    OrganizationalUnit ||--o{ WorkLocation : "organization_id"
    OrganizationalUnit ||--o{ Device : "organization_id"
    Employee ||--o{ OrganizationalUnit : "manages (manager_id)"

    Employee ||--o{ Employee : "manager_id (self-ref)"
    User |o--o| Employee : "links (user_id)"

    Employee ||--o{ Attendance : "has"
    Employee ||--o{ AttendanceSchedule : "has"
    Employee ||--o{ Leave : "requests"
    Employee ||--o{ Attachment : "owns"

    Shift ||--o{ Employee : "assigned"
    Shift ||--o{ Attendance : "worked"
    Shift ||--o{ ScheduleDay : "applies"
    Shift ||--o{ ScheduleIssue : "overrides"

    AttendanceSchedule ||--o{ ScheduleDay : "contains"
    AttendanceSchedule ||--o{ ScheduleIssue : "exceptions"
    AttendanceSchedule ||--o{ Attendance : "drives"

    Attendance ||--o{ AttendanceBreak : "has"
    Attendance ||--o{ AttendanceException : "adjusted by"
    Employee ||--o{ AttendanceException : "modifier / approver"

    User ||--o{ UserPermission : "granted"
    Permission ||--o{ UserPermission : "in"

    Employee ||--o{ SecurityAuditLog : "subject"
    User ||--o{ SecurityAuditLog : "actor"

    OrganizationalUnit {
        uuid id PK
        string unit_code UK
        string unit_name
        uuid parent_unit_id FK
        uuid manager_id FK
    }
    Employee {
        uuid id PK
        string emp_id UK
        string full_name
        uuid organizational_unit_id FK
        uuid manager_id FK
        uuid user_id FK
    }
    User {
        uuid id PK
        string user_login UK
        string role
        string status
        uuid organizational_unit_id FK
    }
    Permission {
        uuid id PK
        string name
        string resource
        string action
    }
    UserPermission {
        uuid id PK
        uuid user_id FK
        uuid permission_id FK
        datetime expiry_date
    }
    Shift {
        uuid id PK
        string name
        string shift_type
        time start_time
        time end_time
    }
    Holiday {
        uuid id PK
        uuid organization_id FK
        date date
        bool is_recurring
    }
    WorkLocation {
        uuid id PK
        uuid organization_id FK
        double latitude
        double longitude
        int radius_meters
    }
    Device {
        uuid id PK
        string ip_address
        uuid organization_id FK
        bool is_active
    }
    AttendanceSchedule {
        uuid id PK
        uuid employee_id FK
        date start_date
        date end_date
        string schedule_type
    }
    ScheduleDay {
        uuid id PK
        uuid attendance_schedule_id FK
        uuid shift_id FK
        date schedule_day_date
    }
    ScheduleIssue {
        uuid id PK
        uuid attendance_schedule_id FK
        uuid shift_id FK
        string exception_type
    }
    Attendance {
        uuid id PK
        uuid employee_id FK
        uuid organization_id
        uuid shift_id FK
        uuid attendance_schedule_id FK
        datetime date
        string status
    }
    AttendanceBreak {
        uuid id PK
        uuid attendance_id FK
        string break_type
        int duration_minutes
    }
    AttendanceException {
        uuid id PK
        uuid attendance_id FK
        uuid modifier_id FK
        uuid approver_id FK
        string exception_type
    }
    Leave {
        uuid id PK
        uuid employee_id FK
        string leave_type
        string status
    }
    Attachment {
        uuid id PK
        uuid employee_id FK
        string type
        string blob_path
    }
    SecurityAuditLog {
        uuid id PK
        uuid employee_id FK
        uuid user_id FK
        string event_type
        bool is_successful
    }
```

## Relationship clusters

**Organization hierarchy.** `OrganizationalUnit` is a self-referencing tree
(`parent_unit_id` → parent, `OnDelete: Restrict`). Each unit can have a managing `Employee`
(`manager_id`, `SetNull`) — note this is the one place an Employee points "up" into the org tree.
Units also act as the `organization_id` owner for `Holiday`, `WorkLocation` and `Device`.

**People (Employee ↔ User).** `Employee` belongs to an `OrganizationalUnit` (`SetNull`) and to a
manager `Employee` via `manager_id` (self-reference → `Subordinates`, `SetNull`). An `Employee`
optionally links to a login `User` one-to-one (`user_id`, `SetNull`); `User` itself can also be
scoped to an `OrganizationalUnit`.

**Authorization (User ↔ Permission).** Many-to-many resolved through the `user_permission` join
entity: `User ||--o{ UserPermission }o--|| Permission`. `UserPermission` carries its own grant
metadata (`expiry_date`, `is_active`), and `Role`/`UserStatus` on `User` are coarse-grained
string enums layered on top.

**Scheduling.** An `Employee` has many `AttendanceSchedule`s (a dated plan, `Restrict` on the FK).
Each schedule expands into `ScheduleDay` rows (one per date, each pinned to a `Shift`,
cascade-deleted with the schedule) and may carry `ScheduleIssue` exceptions (a date that should use
a *different* `Shift`, e.g. holiday / overtime / different-shift).

**Attendance facts.** The high-volume `Attendance` table (~595k rows) records one row per
employee-per-day (`unique(employee_id, date)`). It references the `Employee` (`Restrict`), the
applicable `Shift` (`SetNull`) and the originating `AttendanceSchedule` (`SetNull`). Each attendance
can have several `AttendanceBreak`s and `AttendanceException`s (manual corrections, where an
`Employee` is both the `modifier` and optional `approver`).

**Legacy device log.** `AttendanceLog` is a *flat, FK-free* landing table for raw biometric punches
(`emp_id`, `card_no`, `device_no`, direction) — it is not joined to the relational model and is
therefore drawn without edges; downstream processing turns these into `Attendance` rows. The live
fingerprint source itself lives in MSSQL behind `ExternalAttendanceDbContext` (`dbo.EventTab`).

**Audit.** `SecurityAuditLog` optionally points at both a `User` (actor) and an `Employee`
(subject), capturing login/biometric/location events.
