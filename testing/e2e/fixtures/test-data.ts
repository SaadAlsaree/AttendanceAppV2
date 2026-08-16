/**
 * Shared E2E constants: credentials, storage-state paths, the real (non-stub)
 * module routes, and the per-module DB-table map used by the review loop.
 */

export const CREDENTIALS = {
  admin: {
    login: process.env.E2E_ADMIN_LOGIN || 'seed.admin',
    password: process.env.E2E_ADMIN_PASSWORD || 'Admin@123456'
  },
  super: {
    login: process.env.E2E_SUPER_LOGIN || 'seed.super',
    password: process.env.E2E_SUPER_PASSWORD || 'Super@123456'
  }
};

export const STORAGE_STATE = {
  admin: 'e2e/.auth/admin.json',
  super: 'e2e/.auth/super.json'
};

export const API_URL = process.env.E2E_API_URL || 'http://localhost:7080';

/**
 * Routes that map to REAL screens (present in the sidebar / not commented out).
 * Stub routes that exist on disk but render placeholders are intentionally
 * excluded here and covered as skipped smoke tests in their specs.
 */
export const ROUTES = {
  dashboard: '/dashboard',
  attendanceViewAll: '/attendance/view-all-attendance',
  attendanceNot: '/attendance/not-attendance',
  attendanceLogs: '/attendance/attendance-logs',
  employees: '/employee',
  employeesAddEdit: '/employee/addedit-employees',
  schedule: '/schedule',
  shifts: '/schedule/shifts',
  shiftsNew: '/schedule/shifts/new',
  leave: '/leave',
  leaveNew: '/leave/new',
  organizationalUnit: '/organizational-unit',
  reportsOrganizational: '/reports/organizational-report',
  reportsSummary: '/reports/organizational-summary',
  usersPermissions: '/system/users-permissions',
  devices: '/system/devices',
  profile: '/profile/view-profile'
} as const;

/**
 * Module -> primary Postgres table(s) to assert against in the DB step.
 * Tables are PascalCase + quoted in SQL (see docs/database/tables.md).
 */
export const DB_TABLES: Record<string, string[]> = {
  auth: ['"user"', '"SecurityAuditLogs"'],
  attendance: ['"Attendances"', '"AttendanceBreaks"'],
  'attendance-logs': ['"AttendanceLogs"'],
  employees: ['"Employees"', '"user"'],
  schedules: ['"AttendanceSchedules"', '"ScheduleDays"'],
  shifts: ['"Shifts"'],
  leave: ['"Leaves"'],
  'organizational-units': ['"OrganizationalUnits"'],
  devices: ['"Devices"'],
  'users-permissions': ['"user"', '"permission"', '"user_permission"']
};

/** A run-unique suffix so created records are easy to find and clean up. */
export function uniqueSuffix(): string {
  return `${Date.now().toString().slice(-7)}`;
}
