# Profile & Dashboard

## 1. Purpose

Two user-facing areas:

- **`features/profile`** — the signed-in user's own account: view profile, edit profile, change
  password, and settings.
- **`features/dashboard`** — the landing analytics screen (لوحة التحكم) after login: quick stats,
  alerts, attendance trends, department stats, device status, top performers, etc.

## 2. Routes & key components

| Route | Page file | Renders |
|---|---|---|
| `/dashboard` | `dashboard/page.tsx` | dashboard landing (currently an animated/diagram UI shell) |
| `/profile` | `profile/page.tsx` | profile main (change-password form) |
| `/profile/view-profile` | `profile/view-profile/page.tsx` | `ProfileViewPage` |
| `/profile/edit-profile` (+ new, [id], [id]/edit) | `profile/edit-profile/…` | edit form |
| `/profile/change-password` (+ …) | `profile/change-password/…` | change-password page |
| `/profile/settings` (+ …) | `profile/settings/…` | settings page |

The sidebar footer dropdown links **الملف الشخصي** → `/profile` and **تسجيل الخروج**
(`signOut()`); the top nav links Dashboard → `/dashboard` (`[Admin, Manager, Employee]`).

### Dashboard components (`features/dashboard/components`)

`dashboard.tsx`, `dashboard-page.tsx`, `dashboard-toolbar.tsx`, `quick-stats-card.tsx`,
`alerts-card.tsx`, `attendance-trends-card.tsx`, `department-stats-card.tsx`,
`device-status-card.tsx`, `navigation-card.tsx`, `performance-metrics-card.tsx`,
`recent-activities-card.tsx`, `top-performers-card.tsx`, `refresh-button.tsx`.

### Profile components (`features/profile/components`)

`profile-create-form.tsx` (multi-step profile/resume form), `profile-view-page.tsx` (avatar + name
view). Profile editing reuses employee-profile and password endpoints (see below).

## 3. Data flow

### Dashboard — `features/dashboard/api/dashboard.service.ts`

Server (`axiosInstance`) + `…Client` (`axiosClient`) variants.

| Function | Method | Endpoint |
|---|---|---|
| `getDashboard` | GET | `/dashboard` |
| `getQuickStats` | GET | `/dashboard/quick-stats` |
| `getDashboardStats` | GET | `/dashboard/stats` |

> Per root `CLAUDE.md`, some dashboard endpoints require query params (e.g.
> `GET /dashboard/stats?OrganizationId=<guid>`); a missing required param surfaces as a 500.

Types: `features/dashboard/types/{dashboard.ts, dashboard-stats.ts, quick-stats.ts}`.

### Profile

`features/profile` has no dedicated service; profile/password operations go through the
employee/users services:

| Operation | Method | Endpoint | Service |
|---|---|---|---|
| View profile | GET | `/employees/profile` | `employees.service.ts` (`getProfile`) |
| Update profile | PUT | `/employees/profile` | `employees.service.ts` (`updateProfile`) |
| Change password | POST | `/employees/change-password` | `employees.service.ts` (`changePassword`) |
| Change/reset password (admin) | PUT/POST | `/users(-permissions)/change-password`, `/users/reset-password` | `users-permissions.service.ts` |
| Current user (role/identity) | GET | `/users/me` | `currentUserService` via `useCurrentUser` |

## 4. Flow

```mermaid
graph LR
    L[login -> session JWT] --> D[/dashboard]
    D --> Q[getQuickStats / getDashboardStats / getDashboard]
    Q --> API[GET /dashboard*]
    SB[sidebar footer dropdown] -->|الملف الشخصي| P[/profile]
    P --> CP[change password -> POST /employees/change-password]
    P --> VP[/profile/view-profile -> GET /employees/profile]
    SB -->|تسجيل الخروج| SO[signOut]
```

## 5. Source map

```
src/features/dashboard/
  api/dashboard.service.ts
  types/{dashboard.ts, dashboard-stats.ts, quick-stats.ts}
  components/ (dashboard.tsx, dashboard-page.tsx, *-card.tsx, dashboard-toolbar.tsx, refresh-button.tsx)
  hooks/

src/features/profile/
  components/ (profile-create-form.tsx, profile-view-page.tsx)
  (profile/password operations use features/employee + features/system/users-permissions services)

src/app/(routes)/dashboard/page.tsx
src/app/(routes)/profile/{page.tsx, view-profile, edit-profile, change-password, settings}
```
