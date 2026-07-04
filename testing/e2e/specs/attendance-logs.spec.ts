import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { ROUTES } from '../fixtures/test-data';

test.describe('attendance-logs', () => {
  test('logs screen loads and renders the table', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.attendanceLogs);
    await shell.expectTableOrEmptyState();
  });
});
