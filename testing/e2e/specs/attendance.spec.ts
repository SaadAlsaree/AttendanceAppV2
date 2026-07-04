import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { ROUTES } from '../fixtures/test-data';

test.describe('attendance', () => {
  test('view-all attendance lists records', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.attendanceViewAll);
    await shell.expectTableOrEmptyState();
  });

  test('not-attendance screen loads', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.attendanceNot);
    await shell.expectTableOrEmptyState();
  });

  // Check-in/out + approve flows require a known employee + open record. These
  // are stateful and data-dependent — implement against a seeded employee, then
  // verify in "Attendances" / "AttendanceBreaks" via the Postgres MCP.
  test.fixme('check-in then check-out creates/updates an attendance row', async () => {
    // TODO: drive check-in form -> assert "Attendances" row -> check-out -> approve.
  });
});
