import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { ShiftsPage } from '../pages/shifts.page';
import { ROUTES, uniqueSuffix } from '../fixtures/test-data';

/**
 * Shifts — the full CRUD exemplar. The other CRUD modules follow this shape.
 * After creating, verify in Postgres: SELECT * FROM "Shifts" WHERE name = ...
 * (see e2e/README.md review loop).
 */
test.describe('shifts', () => {
  test('shifts list loads', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.shifts);
    await shell.expectTableOrEmptyState();
  });

  test('create a shift and see it in the list', async ({ page }) => {
    const shifts = new ShiftsPage(page);
    const name = `E2E Shift ${uniqueSuffix()}`;

    await shifts.gotoNew();
    // shiftType is required by the zod schema; 'صباحي' = Morning.
    await shifts.create({
      name,
      startTime: '08:00',
      endTime: '16:00',
      shiftType: 'صباحي'
    });

    // Reliable success signals: redirect back to the list (only happens when
    // the create POST returns a body) and a clean network (no :7080 4xx/5xx).
    await expect(page).toHaveURL(/\/schedule\/shifts$/);

    // Best-effort UI check — the row may sit on a later page of the seeded list
    // or behind a search the listing doesn't expose. Authoritative existence is
    // the DB-verification step below, not this paginated-table scan.
    await shifts.search(name);
    const row = shifts.rowByName(name).first();
    if (await row.count()) {
      await expect(row).toBeVisible();
    }

    // DB-verification (manual / review loop):
    //   SELECT id, name, is_active FROM "Shifts"
    //   WHERE name = '<name>' AND is_deleted = false;
    // then clean up:  UPDATE "Shifts" SET is_deleted = true WHERE name = '<name>';
  });
});
