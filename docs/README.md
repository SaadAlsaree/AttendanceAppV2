# Attendance System — Documentation

The Attendance system is a workforce time-and-attendance platform built as a **.NET 9 Web API**
(Clean Architecture + CQRS) backed by **PostgreSQL** and **Redis**, with a **Next.js 15** frontend.
It ingests punch events from **Hikvision** biometric/face-recognition devices (and optionally a
legacy **MSSQL** event store), turns raw logs into validated attendance records against employee
**schedules and shifts**, manages **leaves, employees, organizational units and devices**, and
produces **dashboards and reports**. Authentication is JWT-based: the browser talks to NextAuth,
NextAuth authenticates against the API, and subsequent calls carry the API-issued bearer token.

## Documentation index

| Area | Document | Description |
|---|---|---|
| Overview | [00-system-overview.md](00-system-overview.md) | Components, topology, ports, end-to-end flow |
| Overview | [01-architecture-patterns.md](01-architecture-patterns.md) | Clean Architecture, CQRS, Result, decorators, domain events |
| Backend | [backend/README.md](backend/README.md) | Backend guide and feature index |
| Backend | [backend/attendance.md](backend/attendance.md) | Attendance records, check-in/out, breaks, approval |
| Backend | [backend/attendance-logs.md](backend/attendance-logs.md) | Raw device logs, verification, rejection |
| Backend | [backend/attendance-schedules.md](backend/attendance-schedules.md) | Schedules, schedule days, schedule issues |
| Backend | [backend/leaves.md](backend/leaves.md) | Leave requests and approval workflow |
| Backend | [backend/employees-and-users.md](backend/employees-and-users.md) | Employees, users, auth, permissions |
| Backend | [backend/organizations.md](backend/organizations.md) | Organizational units, shifts, holidays, work locations |
| Backend | [backend/devices.md](backend/devices.md) | Device registry and status lifecycle |
| Backend | [backend/dashboard-and-reports.md](backend/dashboard-and-reports.md) | Dashboard stats and report generation |
| Backend | [backend/files-and-attachments.md](backend/files-and-attachments.md) | Blob storage and attachments |
| Backend | [backend/cross-cutting.md](backend/cross-cutting.md) | Auth, caching, logging, rate limiting, background jobs |
| Frontend | [frontend/README.md](frontend/README.md) | Frontend guide and route index |
| Frontend | [frontend/architecture.md](frontend/architecture.md) | App Router, NextAuth, data fetching, providers |
| Frontend | [frontend/attendance.md](frontend/attendance.md) | Attendance screens |
| Frontend | [frontend/employees.md](frontend/employees.md) | Employee management screens |
| Frontend | [frontend/schedules-and-shifts.md](frontend/schedules-and-shifts.md) | Schedule and shift screens |
| Frontend | [frontend/leave.md](frontend/leave.md) | Leave screens |
| Frontend | [frontend/reports.md](frontend/reports.md) | Report screens |
| Frontend | [frontend/system-admin.md](frontend/system-admin.md) | Devices, organizations, users administration |
| Frontend | [frontend/profile-and-dashboard.md](frontend/profile-and-dashboard.md) | Profile and dashboard screens |
| Database | [database/README.md](database/README.md) | Database guide, schemas, conventions |
| Database | [database/erd.md](database/erd.md) | Entity-relationship diagram |
| Database | [database/tables.md](database/tables.md) | Table reference |
| Integrations | [integrations/hikvision-devices.md](integrations/hikvision-devices.md) | Hikvision device integration |
| Integrations | [integrations/mssql-sync.md](integrations/mssql-sync.md) | Legacy MSSQL event sync |
| Features | [feature-14-weekly-shift.md](feature-14-weekly-shift.md) | تثبيت الدوام — fixed weekly shift pattern (data model, resolution, API, UI, tests) |
| Features | [feature-18-employee-fixed-shift-filter.md](feature-18-employee-fixed-shift-filter.md) | الدوام الثابت indicator + filter on the employee list (hasFixedShift flag, filter, badge column, assign action) |

> Note: this set was authored starting from the overview and architecture documents
> ([00](00-system-overview.md), [01](01-architecture-patterns.md)). The remaining linked documents
> form the intended documentation tree; consult the source if a target is not yet present.

## Reading order (new developer)

1. [00-system-overview.md](00-system-overview.md) — what the pieces are and how they connect.
2. [01-architecture-patterns.md](01-architecture-patterns.md) — the backend layering and request pipeline.
3. [database/README.md](database/README.md) + [database/erd.md](database/erd.md) — the data model the whole system revolves around.
4. [backend/README.md](backend/README.md) then the feature docs you need (start with
   [attendance-logs](backend/attendance-logs.md) → [attendance](backend/attendance.md) →
   [attendance-schedules](backend/attendance-schedules.md), which form the core processing pipeline).
5. [frontend/architecture.md](frontend/architecture.md) then the screen-specific docs.
6. [integrations/hikvision-devices.md](integrations/hikvision-devices.md) and
   [integrations/mssql-sync.md](integrations/mssql-sync.md) once the core model is clear.

## Diagram legend

All diagrams are [Mermaid](https://mermaid.js.org) code blocks (```` ```mermaid ````). Three types
are used:

| Type | Mermaid keyword | Used for |
|---|---|---|
| Flowchart | `graph` / `flowchart` | Component topology and layer-dependency diagrams (boxes = components/layers, arrows = "depends on" or "talks to") |
| Sequence diagram | `sequenceDiagram` | Time-ordered flows (login, authenticated request, internal request pipeline); vertical lifelines = participants, arrows = calls/responses |
| Entity-relationship | `erDiagram` | The database model in [database/erd.md](database/erd.md) |

Arrow direction in flowcharts reads as "uses / depends on / sends to". Solid arrows in sequence
diagrams are calls; dashed arrows are returns.
