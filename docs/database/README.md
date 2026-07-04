# Attendance — Database Documentation

The Attendance system stores its application data in **PostgreSQL**, accessed through **EF Core 9**.
The schema is defined entirely in code (entity classes + Fluent `IEntityTypeConfiguration` + EF
migrations) — there is no hand-written SQL DDL. This document describes the conventions; see
[`erd.md`](./erd.md) for the relationship diagram and [`tables.md`](./tables.md) for a per-table
reference.

## DbContexts

There are **two** DbContexts (`original/AttendanceAppV2/src/Infrastructure/Database`):

| Context | Provider | Purpose | Mode |
|---|---|---|---|
| `ApplicationDbContext` | PostgreSQL | All first-party application data (users, employees, attendance, schedules, leaves, devices, audit). This is what this documentation covers. | Read/write |
| `ExternalAttendanceDbContext` | SQL Server (MSSQL) | Read-only window into the legacy biometric / fingerprint system. Maps a single keyless entity `EventTab` → `dbo.EventTab`. | **Read-only** (`HasNoKey()`, NoTracking) |

`ExternalAttendanceDbContext` points at a remote MSSQL host (`10.42.10.11` per the root
`CLAUDE.md`) that is unreachable in local dev; it backs a specific sync/integration feature only and
the API starts fine without it.

## PostgreSQL naming convention

`ApplicationDbContext` is registered with **`.UseSnakeCaseNamingConvention()`**
(`Infrastructure/DependencyInjection.cs`) and `HasDefaultSchema("public")`. The effect:

- **Columns, indexes, FK/PK constraint names → `snake_case`** (e.g. `created_at`,
  `organizational_unit_id`, `pk_attendances`, `fk_user_permission_users_user_id`).
- **Table names depend on whether a config calls `ToTable(...)` explicitly.** Most configs pin a
  PascalCase/plural table name (`"Attendances"`, `"Employees"`, `"Users"`, `"OrganizationalUnits"`),
  so those tables keep PascalCase. The handful with *no* explicit `ToTable` fall through to the
  convention and end up snake_case: **`permission`**, **`user_permission`**, **`attendance_exceptions`**,
  **`todo_items`**. (Verified against `Database/Migrations/ApplicationDbContextModelSnapshot.cs`.)

Enum-backed columns are persisted as **strings** via `.HasConversion<string>()` (e.g. `Role`,
`UserStatus`, `AttendanceStatus`, `ScheduleType`, `LogMethod`), not as integers — see `tables.md`.

## Primary keys & the auditable base

Every domain entity derives from **`AuditableEntity<Guid>`** (`Domain/Common/AuditableEntity.cs`),
so they share a uniform shape:

| Member | Column | Meaning |
|---|---|---|
| `Id` | `id` (`uuid`) | **Guid primary key** (one exception below) |
| `CreatedAt` / `CreatedBy` | `created_at` / `created_by` | creation audit (`created_by` is a nullable user Guid) |
| `LastUpdatedAt` / `LastUpdatedBy` | `last_updated_at` / `last_updated_by` | last-modification audit |
| `IsDeleted` | `is_deleted` | **soft-delete flag** — rows are flagged, not physically removed |
| `DeletedAt` / `DeletedBy` | `deleted_at` / `deleted_by` | soft-delete audit |
| `DoneProcdureDate` | `done_procdure_date` | misc processing timestamp (name spelled as in source) |

Because soft deletes are the norm, queries that should hide deleted records must filter
`is_deleted = false`. `tables.md` lists only the *business* columns per table and assumes these
audit columns are present everywhere.

> PK exception: `Employee.Id` is configured `ValueGeneratedOnAdd()` with PostgreSQL identity
> options (`HasIdentityOptions(startValue:1, incrementBy:1)`) in `EmployeeConfiguration`, i.e. it is
> a generated/identity Guid rather than a client-supplied one. All other entities use plain Guids.

## Seed / scale (local dump)

Per the root `CLAUDE.md`, the restored local database contains roughly:

- **~595,000** attendance rows
- **5,217** employees
- **28** users (plus two locally-seeded logins, `seed.admin` / `seed.super`)
- **9** EF migrations recorded in `public.__EFMigrationsHistory` (so the API does not re-migrate)

## Hangfire

Background jobs use **Hangfire**, which owns its own **12 tables in a separate `hangfire` schema**
(job, state, server, set, hash, list, counter, aggregatedcounter, lock, jobqueue, schema, etc.).
These are framework-managed and not part of the application domain model documented here.

## Where things live in the source

- DbContexts: `Infrastructure/Database/ApplicationDbContext.cs`, `ExternalAttendanceDbContext.cs`
- Entities: `Domain/Entities/{Users,Organizations,Attendance,Devices}/**`
- Fluent configs: `Infrastructure/Configuration/**`
- Enums: `Domain/Enums/**`
- Migration snapshot (authoritative table/column/FK names): `Infrastructure/Database/Migrations/ApplicationDbContextModelSnapshot.cs`
- DI / provider registration: `Infrastructure/DependencyInjection.cs`
