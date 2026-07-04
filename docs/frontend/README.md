# Attendance Frontend — Module Map

The Attendance frontend (`original/attendance-frontend`) is a **Next.js 15 App Router**
application for the daily-attendance management platform (منصة أدارة الموقف اليومي). It is an
internal admin/operations console: login, dashboard, attendance tracking, employee/schedule/shift
management, leave handling, reports, and system administration.

## Tech stack

| Concern | Choice |
|---|---|
| Framework | Next.js 15 (App Router, React 19, Server + Client Components) |
| Language | TypeScript |
| UI components | shadcn/ui (Radix primitives) in `src/components/ui` |
| Styling | Tailwind CSS v4 (`globals.css`, `theme.css`) |
| Server state / data fetching | TanStack React Query (`@tanstack/react-query`) + axios |
| Forms | React Hook Form + Zod resolvers |
| Tables | TanStack Table (data tables under each feature's `*-tables/`) |
| URL/search-param state | `nuqs` (server-driven table pagination/filtering) |
| Auth | NextAuth (credentials provider, JWT session) |
| Command palette | kbar (Cmd-K) |
| Direction / locale | **RTL / Arabic** — `<html dir="rtl">`, sidebar `side="right"`, Arabic nav labels |
| Toasts | `sonner` |

## Feature-folder convention

Almost all domain code lives under `src/features/<feature>/`. Each feature is self-contained and
follows the same internal layout:

```
src/features/<feature>/
├── api/        # *.service.ts — axios calls to the backend (server + client variants)
├── components/ # feature UI: listing (server), forms, view pages, *-tables/ (TanStack Table)
├── types/      # TypeScript interfaces, enums, request/response DTOs
└── utils/      # feature-specific helpers (optional)
```

Routes live separately under `src/app/(routes)/<area>/...`; each `page.tsx` is a thin shell that
renders a feature component. The two route groups are `(auth)` (login / unauthorized — public) and
`(routes)` (everything behind the authenticated layout).

A recurring pattern in the `api/` services: **paired functions** — a server-side variant using
`axiosInstance` (attaches the bearer token from `getServerSession`) and a client-side variant with a
`Client` suffix using `axiosClient`. Listing components are async Server Components that fetch via
the server variant and pass data to a Client table; forms/dialogs use the `Client` variant.

## Documentation index

| Doc | Covers |
|---|---|
| [architecture.md](./architecture.md) | App Router layout, route groups, root + protected layouts, NextAuth config, middleware route protection, axios setup (`axiosInstance` vs `axiosClient`, `X-Client-IP`), React Query provider, theming, the single-`NEXT_PUBLIC_API_URL` host decision |
| [attendance.md](./attendance.md) | `features/attendance` + `features/attendance-logs` — view-all, not-attendance, logs; check-in/out + approve |
| [employees.md](./employees.md) | `features/employee` — list, add/edit (register), assign managers, employee schedules, profile/role helpers |
| [schedules-and-shifts.md](./schedules-and-shifts.md) | `features/schedule` + `features/shift` — schedules, schedule-days, shifts, assign schedules |
| [leave.md](./leave.md) | `features/leave` — leave list, create, approve/reject/cancel |
| [reports.md](./reports.md) | `features/reports` — organizational report, organizational summary, attendance summary |
| [system-admin.md](./system-admin.md) | `features/system` (users-permissions, devices, organizationsunits, work-locations, system-configuration) + RBAC roles and role-filtered navigation |
| [profile-and-dashboard.md](./profile-and-dashboard.md) | `features/profile` (view/edit/change-password/settings) + `features/dashboard` |

## Backend contract

All services target the .NET 9 Web API via `process.env.NEXT_PUBLIC_API_URL` (locally
`http://localhost:7080`; falls back to `http://localhost:7000` in code). The bearer token is the
JWT issued by the backend's `/auth/login`, carried in the NextAuth session. See
[architecture.md](./architecture.md) for the full auth + transport story.
