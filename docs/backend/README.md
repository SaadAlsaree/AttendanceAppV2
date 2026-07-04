# Attendance Backend — Module Map

The Attendance backend is a **.NET 9 Web API** built with **Clean Architecture + CQRS**. Code lives
under `original/AttendanceAppV2/src` and is split into four projects: **Domain** (entities, enums,
domain events, errors), **Application** (CQRS commands/queries grouped into vertical-slice *Features*,
plus abstractions), **Infrastructure** (EF Core / Postgres, authentication, caching, Hangfire jobs,
blob storage, time), and **Web.Api** (minimal-API endpoints, middleware, composition). Endpoints are
mapped one-per-file under `Web.Api/Endpoints/<Module>/` and dispatch to `ICommandHandler` /
`IQueryHandler` implementations in `Application/Features/...`. Authorization is **role-based via JWT
claims** (a `RequireAuthorization(... RequireAssertion ...)` policy reads the `Role` claim); a
permission-based infrastructure also exists but its `PermissionProvider` is currently a stub. All
business logic lives in handlers — domain entities are largely anemic.

## Modules

| Module | Description | Doc |
|---|---|---|
| Attendance | Daily attendance records + breaks; check-in/check-out, approval, status calculation. | [attendance.md](./attendance.md) |
| Attendance Logs | Raw device/card-swipe & manual logs; how they relate to Attendance records. | [attendance-logs.md](./attendance-logs.md) |
| Attendance Schedules | Per-employee schedules + per-day shift assignments, bulk insert, SQL functions. | [attendance-schedules.md](./attendance-schedules.md) |
| Leaves | Leave-request lifecycle (Pending → Approved / Rejected). | [leaves.md](./leaves.md) |
| Employees & Users | Employee records and application users, login, roles, password reset. | _(see Endpoints/Users, Endpoints/Employees)_ |
| Organizations | Organizational units, work locations, unit work-hours, shifts, holidays, attachments. | _(see Features/Organizations)_ |
| Devices | Biometric / access devices feeding attendance logs. | _(see Endpoints/Devices)_ |
| Dashboard & Reports | Aggregated stats, quick stats, and attendance reports. | _(see Features/Dashboard, Features/Reports)_ |
| Files & Attachments | Blob upload/download/delete via Azure Blob (Azurite locally) + attachments. | _(see Endpoints/Files.cs, Features/Organizations/Attachments)_ |
| Cross-cutting | JWT auth, authorization, Redis caching, logging, Hangfire jobs, blob storage, time, security middleware. | [cross-cutting.md](./cross-cutting.md) |

> Modules without a dedicated doc are listed for completeness; this set focuses on the
> attendance-core slices and the shared concerns.

## Conventions used across modules

- **CQRS**: writes are `Command` (POST/PUT/DELETE), reads are `Query` (GET); each lives in its own
  folder under `Application/Features/<Area>/<Module>/<Operation>/`.
- **Result pattern**: handlers return `Result<T>` and endpoints call `.Match(Results.Ok, CustomResults.Problem)`.
- **Auth**: every business endpoint uses `.RequireAuthorization(policy => policy.RequireAssertion(...))`
  checking the JWT `Role` claim against an allow-list, plus `.RequireRateLimiting("per-user")`.
  Anonymous endpoints (e.g. login) use `.AllowAnonymous()` with `.RequireRateLimiting("fixed")`.
- **Time**: all persisted timestamps are UTC; `IDateTimeProvider` (Asia/Baghdad) converts for display.
