import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { pgClient, scalar } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 02 (UI): the directorate report SCREEN must render the three figures
 * the feature requires, per directorate, in the unit header:
 *   • إجمالي الموظفين  — real headcount        (unit.totalEmployees)
 *   • حضور            — present / حاضرين       (unit.totalAttendances)
 *   • غير مبصم        — non-fingerprinted/غير مبصمين (unit.totalNotAttendances)
 *
 * This is the browser counterpart to directorate-report-api.spec.ts. The report
 * page (a server component) reads `organizationalUnitId`/`date` from the URL and
 * fetches server-side, but the backend scopes to the logged-in user's own org
 * unit — so the spec points seed.admin (the storageState session) at a real
 * directorate, computes ground truth from Postgres, deep-links to the screen,
 * and asserts the RENDERED numbers match. seed.admin is restored in afterAll.
 *
 * "غير مبصم" only appears in this table header (no summary card shows it), so it
 * is the unambiguous check that the new wiring renders correctly.
 */

const LOGIN = process.env.E2E_ADMIN_LOGIN || 'seed.admin';

type Truth = {
  unitId: string;
  unitName: string;
  date: string;
  employees: number;
  present: number;
  notAttendances: number;
};

test.describe.serial('Feature 02 — directorate report UI (browser)', () => {
  let db: Client;
  let origUnit: string | null = null;
  let truth: Truth;

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();

    // Same selection as the API spec: the (unit, date) with the most
    // non-fingerprinted rows, requiring some present too, levels 1-3.
    const pick = await db.query(`
      SELECT e.organizational_unit_id AS unit_id,
             to_char(a.date::date, 'YYYY-MM-DD') AS d
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id = a.employee_id AND e.is_deleted = false
      JOIN public."OrganizationalUnits" ou ON ou.id = e.organizational_unit_id
      WHERE a.is_deleted = false AND ou.is_deleted = false
            AND ou.unit_level BETWEEN 1 AND 3
      GROUP BY e.organizational_unit_id, a.date::date
      HAVING COUNT(*) FILTER (WHERE a.check_in_time IS NOT NULL OR a.check_out_time IS NOT NULL) > 0
         AND COUNT(*) FILTER (WHERE a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL) > 0
      ORDER BY COUNT(*) FILTER (WHERE a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL) DESC
      LIMIT 1;`);
    expect(pick.rows.length, 'a directorate+date with data must exist').toBeGreaterThan(0);
    const unitId: string = pick.rows[0].unit_id;
    const date: string = pick.rows[0].d;

    const unitName = (await scalar<string>(
      db, `SELECT unit_name FROM public."OrganizationalUnits" WHERE id=$1`, [unitId]
    )) as string;
    const employees = Number(await scalar(
      db, `SELECT COUNT(*) FROM public."Employees" WHERE organizational_unit_id=$1 AND is_deleted=false`, [unitId]
    ));
    const present = Number(await scalar(db, `
      SELECT COUNT(*) FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE e.organizational_unit_id=$1 AND a.is_deleted=false AND a.date::date=$2
            AND (a.check_in_time IS NOT NULL OR a.check_out_time IS NOT NULL)`, [unitId, date]));
    const notAttendances = Number(await scalar(db, `
      SELECT COUNT(*) FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE e.organizational_unit_id=$1 AND a.is_deleted=false AND a.date::date=$2
            AND a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL
            AND a.employee_id NOT IN (
              SELECT l.employee_id FROM public."Leaves" l
              JOIN public."Employees" e2 ON e2.id=l.employee_id AND e2.is_deleted=false
              WHERE e2.organizational_unit_id=$1 AND l.is_deleted=false
                    AND l.status='Approved'
                    AND l.start_date::date<=$2 AND l.end_date::date>=$2)`, [unitId, date]));

    truth = { unitId, unitName, date, employees, present, notAttendances };
    // eslint-disable-next-line no-console
    console.log(`[feature-02-ui] target=${unitName} date=${date} ` +
      `employees=${employees} present=${present} non-fingerprinted=${notAttendances}`);

    origUnit = await scalar<string>(
      db, `SELECT organizational_unit_id FROM public."Users" WHERE user_login=$1`, [LOGIN]
    );
    await db.query(
      `UPDATE public."Users" SET organizational_unit_id=$1 WHERE user_login=$2`, [unitId, LOGIN]
    );
  });

  test.afterAll(async () => {
    if (db) {
      await db.query(
        `UPDATE public."Users" SET organizational_unit_id=$1 WHERE user_login=$2`, [origUnit, LOGIN]
      ).catch(() => {});
      await db.end().catch(() => {});
    }
  });

  test('directorate report screen renders headcount, present, and non-fingerprinted', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(
      `/reports/organizational-report?organizationalUnitId=${truth.unitId}` +
      `&date=${truth.date}&includeSubUnits=false&pageNumber=1&pageSize=10`
    );

    // The unit header (desktop table view) carries the three figures as badges.
    await expect(page.getByRole('heading', { name: truth.unitName }).first())
      .toBeVisible({ timeout: 30_000 });
    const header = page.locator('div.bg-muted').filter({ hasText: truth.unitName }).first();
    await expect(header).toBeVisible();

    const metric = (label: string) =>
      header.locator('p, span').filter({ hasText: label }).first();

    // إجمالي الموظفين — real directorate headcount (the badge this feature added)
    await expect(metric('إجمالي الموظفين'), 'real headcount badge')
      .toContainText(String(truth.employees));
    // حضور — present / حاضرين
    await expect(metric('حضور'), 'present badge')
      .toContainText(String(truth.present));
    // غير مبصم — non-fingerprinted / غير مبصمين (only rendered here)
    await expect(metric('غير مبصم'), 'non-fingerprinted badge')
      .toContainText(String(truth.notAttendances));

    // Durable visual proof of the rendered report (attached to the HTML report too).
    await header.scrollIntoViewIfNeeded();
    await page.screenshot({ path: 'test-results/feature-02-directorate-report.png', fullPage: true });
    await test.info().attach('feature-02-directorate-report', {
      path: 'test-results/feature-02-directorate-report.png',
      contentType: 'image/png'
    });
  });
});
