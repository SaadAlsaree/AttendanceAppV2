import { type Page, expect } from '@playwright/test';
import { ROUTES } from '../fixtures/test-data';

/**
 * Shifts module — the CRUD exemplar (src/features/shift/components/shift-form.tsx).
 * Arabic labels: name=اسم المناوبة, start=وقت البداية, end=وقت النهاية,
 * type select=نوع المناوبة. Create button="إنشاء", update button="تحديث".
 */
export class ShiftsPage {
  constructor(private readonly page: Page) {}

  async gotoList() {
    await this.page.goto(ROUTES.shifts, { waitUntil: 'domcontentloaded' });
    await this.page.locator('table, [role="table"]').first().waitFor({
      state: 'visible',
      timeout: 30_000
    });
  }

  async gotoNew() {
    await this.page.goto(ROUTES.shiftsNew);
    await expect(this.page.getByLabel('اسم المناوبة')).toBeVisible();
  }

  /**
   * Fill the create form and submit. Returns once the app redirects back to the
   * shifts list (router.push('/schedule/shifts') on success).
   */
  async create(opts: {
    name: string;
    startTime?: string;
    endTime?: string;
    shiftType?: string; // displayName as shown in the select
  }) {
    const { name, startTime = '08:00', endTime = '16:00', shiftType } = opts;

    await this.page.getByLabel('اسم المناوبة').fill(name);

    if (shiftType) {
      await this.page.getByLabel('نوع المناوبة').click();
      await this.page.getByRole('option', { name: shiftType }).click();
    }

    await this.page.getByLabel('وقت البداية').fill(startTime);
    await this.page.getByLabel('وقت النهاية').fill(endTime);

    await this.page.getByRole('button', { name: 'إنشاء' }).click();
    // Success path: sonner toast then router.push back to the list.
    await this.page.waitForURL('**/schedule/shifts', { timeout: 45_000 });
  }

  successToast() {
    return this.page.getByText('تم إنشاء المناوبة بنجاح');
  }

  /** Type into the listing search box (if present) and wait for the refetch. */
  async search(term: string) {
    const box = this.page
      .getByPlaceholder(/search|بحث/i)
      .or(this.page.locator('input[type="search"]'))
      .first();
    if (await box.count()) {
      await box.fill(term);
      // Allow the debounced refetch to settle without relying on networkidle.
      await this.page.waitForTimeout(1000);
    }
  }

  rowByName(name: string) {
    return this.page.getByRole('row', { name: new RegExp(name) });
  }
}
