import { test, expect } from '../fixtures/test-base';
import { ShellPage } from '../pages/shell.page';
import { ROUTES } from '../fixtures/test-data';

test.describe('system / devices', () => {
  test('devices screen loads', async ({ page }) => {
    const shell = new ShellPage(page);
    await shell.goto(ROUTES.devices);
    await shell.expectTableOrEmptyState();
  });

  // Device CRUD + "test connection" (HikvisionService). In local dev the device
  // IPs are unreachable, so a test-connection is expected to report failure
  // gracefully — assert the UI shows a failure result, not an unhandled error.
  test.fixme('create device then test connection reports a result', async () => {
    // TODO: create device -> verify "Devices" row -> click test-connection ->
    //       assert a status result is shown (success OR handled failure).
  });
});
