import { test as base, expect, type Page } from '@playwright/test';

/**
 * Base test fixture with request/console diagnostics wired in.
 *
 * Every test automatically collects:
 *  - browser console errors
 *  - failed responses (status >= 400) hitting the API
 *
 * These are exposed via the `diagnostics` fixture so specs can assert "no
 * silent API errors" — the same intent as the skill's aggressive Burp review,
 * applied at the test layer. The deep request-by-request review still happens
 * in Burp (see e2e/README.md).
 */

export type Diagnostics = {
  consoleErrors: string[];
  failedResponses: { url: string; status: number }[];
};

type Fixtures = {
  diagnostics: Diagnostics;
};

export const test = base.extend<Fixtures>({
  // auto: runs for every test so API-failure diagnostics are always reported,
  // even when a spec doesn't destructure `diagnostics`.
  diagnostics: [async ({ page }, use, testInfo) => {
    const consoleErrors: string[] = [];
    const failedResponses: { url: string; status: number }[] = [];

    page.on('console', (msg) => {
      if (msg.type() === 'error') consoleErrors.push(msg.text());
    });
    page.on('response', (res) => {
      const url = res.url();
      // Only flag API failures, not Next.js asset 404s/HMR noise.
      if (res.status() >= 400 && /:7080\//.test(url)) {
        failedResponses.push({ url, status: res.status() });
      }
    });

    await use({ consoleErrors, failedResponses });

    // Surface diagnostics in the report WITHOUT failing render/flow smoke tests.
    // Per-request verdicts (401/500 etc.) are gated in the Burp review phase
    // (see e2e/README.md) — the automated layer reports them for triage.
    if (failedResponses.length) {
      testInfo.annotations.push({
        type: 'api-failures',
        description: failedResponses
          .map((r) => `${r.status} ${r.url}`)
          .join('\n')
      });
      // eslint-disable-next-line no-console
      console.log(
        `\n[diagnostics] ${testInfo.title} — failed API responses:\n` +
          failedResponses.map((r) => `  ${r.status} ${r.url}`).join('\n')
      );
    }
  }, { auto: true }]
});

export { expect };
export type { Page };
