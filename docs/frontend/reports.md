# Reports

## 1. Purpose

`features/reports` produces attendance analytics. Two report views are live:

- **Organizational Summary** (تقارير العام) — aggregated attendance stats per organizational unit
  for a date, with per-unit cards and a roll-up.
- **Organizational Report** (تقارير الجهات) — a detailed per-employee report for a unit/date/shift,
  with a table view and a print-friendly layout.

A third backend endpoint (`/reports/attendance-summary`) is wired in the service but its route page
(`attendance-reports`) is currently a static card.

## 2. Routes & key components

Routes under `src/app/(routes)/reports/`. All report routes enforce role checks (Admin/Manager;
organizational-report also allows Employee) and redirect to `/` if unauthorized.

| Route | Page file | Renders |
|---|---|---|
| `/reports/organizational-summary` | `organizational-summary/page.tsx` | `OrganizationalSummary` |
| `/reports/organizational-report` | `organizational-report/page.tsx` | `OrganizationalReportContainer` |
| `/reports/attendance-reports` | `attendance-reports/page.tsx` | static card (no dynamic component) |

The sidebar surfaces these under both **Reports** and **Reports & Analytics** groups in
`src/constants/data.ts`.

### Components (`features/reports/components`)

**organizational-summary/**

| File | Purpose |
|---|---|
| `organizational-summary.tsx` | Container; summary cards + per-unit data cards |
| `organizational-summary-card.tsx` | Stat card (attendance/absence/late/leave/overtime) |
| `organizational-summary-data-cart.tsx` | Per-unit detail table |
| `organizational-summary-filter.tsx` | Date + org-unit filters |

**organizational-report/**

| File | Purpose |
|---|---|
| `organizational-report-container.tsx` | Container with table/print toggle |
| `organizational-report-filter.tsx` | Unit / date / shift / search filters |
| `organizational-report-header.tsx` | Report metadata header |
| `organizational-report-table.tsx` | Per-employee attendance table |
| `organizational-report-print.tsx` | Print-friendly layout |

## 3. Data flow

Service: `features/reports/api/reports.service.ts` — each endpoint has a server (`axiosInstance`)
and a `…Client` (`axiosClient`) variant.

### Backend endpoints

| Function (server / `…Client`) | Method | Endpoint |
|---|---|---|
| `getAttendanceSummary` | GET | `/reports/attendance-summary` |
| `getOrganizationalSummary` | GET | `/reports/organizational-summary` |
| `getOrganization` | GET | `/reports/organization` |

Query params drive each report (organizational-unit id, start/end date or single date, shift id,
includeSubUnits, search term/pagination for the detailed report).

### Types

- `attendance-reports.ts` — `GetAttendanceReportQuery`, `AttendanceReportData` (`generalStats`,
  `shiftStats`, `leaveStats`).
- `organizational-summary.ts` — `OrganizationalSummaryQuery`, `OrganizationalSummaryData`
  (`units[]` + aggregated `GeneralStats`), `OrganizationalUnitSummary`.
- `organization-report.ts` — `OrganizationalReportRequest`, `OrganizationalReportData` (`units[]`
  each with `employeeDetails[]`), `EmployeeDetail` (checkIn/checkOut, isLate, isAbsent, isOnLeave,
  isEarlyLeave, overtimeDuration).

## 4. Flow

```mermaid
graph LR
    A[/reports/organizational-report] --> RC{role Admin/Manager/Employee?}
    RC -- no --> R[redirect /]
    RC -- yes --> C[OrganizationalReportContainer]
    C --> FT[filter: unit/date/shift/search]
    FT --> SVC[getOrganization -> GET /reports/organization]
    SVC --> TBL[report table]
    TBL --> PR[print layout]
```

## 5. Source map

```
src/features/reports/
  api/reports.service.ts
  types/{attendance-reports.ts, organizational-summary.ts, organization-report.ts}
  components/
    organizational-summary/{organizational-summary.tsx, *-card.tsx, *-data-cart.tsx, *-filter.tsx}
    organizational-report/{*-container.tsx, *-filter.tsx, *-header.tsx, *-table.tsx, *-print.tsx}

src/app/(routes)/reports/
  organizational-summary/page.tsx
  organizational-report/page.tsx
  attendance-reports/page.tsx   (static)
```
