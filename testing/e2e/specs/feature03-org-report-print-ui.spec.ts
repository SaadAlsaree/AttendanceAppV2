import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { pgClient, scalar } from '../fixtures/db';
import type { Client } from 'pg';

/**
 * FEATURE 03 (UI) — part A: the organizational report's PRINTOUT must now include
 * the names of the non-fingerprinted (غير المبصمين) and of the action employees
 * (الإجراءات), not just the present (حاضرين).
 *
 * The print layout is rendered server-side into a hidden (`display:none`) container
 * next to the screen report (react-to-print clones it on print). So this browser
 * spec deep-links to the report, then asserts the RENDERED print DOM carries the
 * new "غير المبصمين (N)" subsection with the right count and a real employee name —
 * the exact content that lands on paper. It then unhides the container to attach a
 * visual screenshot. seed.admin is pointed at a real directorate and restored in
 * afterAll (the report scopes to the logged-in user's own org unit).
 *
 * Note: the report renders server-side; the filter's client-side /employees,
 * /organizational-units & /shifts calls may 401 (a pre-existing app auth quirk) and
 * show up in diagnostics, but they don't affect this server-rendered assertion.
 */

const LOGIN = process.env.E2E_ADMIN_LOGIN || 'seed.admin';

type Truth = {
  unitId: string;
  unitName: string;
  date: string;
  notAttendances: number;
  sampleName: string;
};

test.describe.serial('Feature 03 — org report print sections (browser)', () => {
  let db: Client;
  let origUnit: string | null = null;
  let truth: Truth;

  test.beforeAll(async () => {
    db = pgClient();
    await db.connect();

    // (unit, date) with the most non-fingerprinted rows, report levels 1-3.
    const pick = await db.query(`
      SELECT e.organizational_unit_id AS unit_id,
             to_char(a.date::date, 'YYYY-MM-DD') AS d
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id = a.employee_id AND e.is_deleted = false
      JOIN public."OrganizationalUnits" ou ON ou.id = e.organizational_unit_id
      WHERE a.is_deleted = false AND ou.is_deleted = false
            AND ou.unit_level BETWEEN 1 AND 3
      GROUP BY e.organizational_unit_id, a.date::date
      HAVING COUNT(*) FILTER (WHERE a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL) > 0
      ORDER BY COUNT(*) FILTER (WHERE a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL) DESC
      LIMIT 1;`);
    expect(pick.rows.length, 'a unit+date with non-fingerprinted data must exist').toBeGreaterThan(0);
    const unitId: string = pick.rows[0].unit_id;
    const date: string = pick.rows[0].d;

    const unitName = (await scalar<string>(
      db, `SELECT unit_name FROM public."OrganizationalUnits" WHERE id=$1`, [unitId]
    )) as string;

    const names = await db.query(
      `
      SELECT e.full_name AS name
      FROM public."Attendances" a
      JOIN public."Employees" e ON e.id=a.employee_id AND e.is_deleted=false
      WHERE e.organizational_unit_id=$1 AND a.is_deleted=false AND a.date::date=$2
            AND a.shift_id IS NOT NULL AND a.check_in_time IS NULL AND a.check_out_time IS NULL
            AND NOT EXISTS (
              SELECT 1 FROM public."Leaves" l
              WHERE l.employee_id=a.employee_id AND l.is_deleted=false
                    AND l.status='Approved'
                    AND l.start_date::date<=$2 AND l.end_date::date>=$2)
      ORDER BY e.full_name`,
      [unitId, date]
    );

    truth = {
      unitId,
      unitName,
      date,
      notAttendances: names.rows.length,
      sampleName: names.rows[0].name as string
    };
    // eslint-disable-next-line no-console
    console.log(`[feature-03A-ui] unit=${unitName} date=${date} non-fingerprinted=${truth.notAttendances}`);

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

  test('print layout renders the غير المبصمين section with names', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(
      `/reports/organizational-report?organizationalUnitId=${truth.unitId}` +
      `&date=${truth.date}&includeSubUnits=false&pageNumber=1&pageSize=10`
    );

    // Wait for the screen report (and thus the hidden print clone) to render.
    await expect(page.getByRole('heading', { name: truth.unitName }).first())
      .toBeVisible({ timeout: 30_000 });

    const printContainer = page.locator('.print-container');
    await expect(printContainer).toHaveCount(1);

    // The new non-fingerprinted subsection title carries the count. (Hidden via
    // display:none — toContainText reads textContent, no visibility needed.)
    const nfTitle = printContainer
      .locator('.print-subsection-title')
      .filter({ hasText: 'غير المبصمين' })
      .first();
    await expect(nfTitle, 'غير المبصمين subsection must exist in the print DOM')
      .toContainText(String(truth.notAttendances));

    // A real non-fingerprinted employee name must appear in the printout.
    await expect(printContainer, 'a non-fingerprinted name must be printed')
      .toContainText(truth.sampleName);

    // The الإجراءات subsection is conditional on having action data; assert it is
    // well-formed when present (no actions in this dump → section omitted).
    const actionTitle = printContainer
      .locator('.print-subsection-title')
      .filter({ hasText: 'الإجراءات' });
    if (await actionTitle.count()) {
      await expect(actionTitle.first()).toContainText(/\d+/);
    }

    // Visual proof: unhide the print container (its layout lives under @media print,
    // so force the screen styles we need) and screenshot the whole page.
    await page.evaluate(() => {
      const c = document.querySelector('.print-container') as HTMLElement | null;
      const wrapper = c?.parentElement as HTMLElement | null;
      if (wrapper) wrapper.style.display = 'block';
      document.querySelectorAll('.print-subsection-title').forEach((el) => {
        (el as HTMLElement).style.fontWeight = 'bold';
        (el as HTMLElement).style.background = '#f5f5f5';
      });
    });
    await page.screenshot({ path: 'test-results/feature-03A-print-sections.png', fullPage: true });
    await test.info().attach('feature-03A-print-sections', {
      path: 'test-results/feature-03A-print-sections.png',
      contentType: 'image/png'
    });
  });
});
