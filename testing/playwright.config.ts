import { defineConfig, devices } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';

/**
 * Playwright E2E configuration for the Attendance frontend.
 *
 * Pattern mirrors the `e2e-test-review` skill: the browser is routed through a
 * Burp Suite proxy so EVERY HTTP request can be reviewed afterwards via the Burp
 * MCP, and DB state is verified via the Postgres MCP. See e2e/README.md.
 *
 * Proxy is OPT-IN: it is only enabled when BURP_PROXY is set (e.g.
 * `BURP_PROXY=http://127.0.0.1:8383 npm run test:e2e:leave`). Without it the
 * suite runs normally against http://localhost:3000 with no proxy.
 */

const BASE_URL = process.env.E2E_BASE_URL || 'http://localhost:3000';

// Burp proxy defaults to the user's listener (127.0.0.1:8383) so the suite is
// reviewable in Burp out of the box. Override with another URL via BURP_PROXY,
// or disable entirely with BURP_PROXY=off (e.g. when Burp isn't running).
const RAW_BURP = process.env.BURP_PROXY ?? 'http://127.0.0.1:8383';
const BURP_PROXY = RAW_BURP && RAW_BURP !== 'off' ? RAW_BURP : undefined;

// Frontend repo location, for auto-starting `next dev`. Override with
// E2E_FRONTEND_DIR; otherwise try the two known layouts relative to this file:
// in-repo (AttendanceAppV2/testing) and the local workspace (.local/testing).
const FRONTEND_DIR =
  process.env.E2E_FRONTEND_DIR ||
  [
    path.resolve(__dirname, '../../attendance-frontend'),
    path.resolve(__dirname, '../../original/attendance-frontend')
  ].find((p) => fs.existsSync(p)) ||
  path.resolve(__dirname, '../../attendance-frontend');

export default defineConfig({
  testDir: './e2e',
  // Serial: the app is stateful (login session reused across specs); also keeps
  // the Burp proxy history readable as a single ordered stream.
  workers: 1,
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  // Routing through Burp adds latency that can occasionally trip the app's 10s
  // axios timeout on a mutation; auto-retry absorbs that transient flakiness.
  retries: 2,
  timeout: 120_000,
  expect: { timeout: 15_000 },
  reporter: [['list'], ['html', { open: 'never' }]],

  use: {
    baseURL: BASE_URL,
    actionTimeout: 20_000,
    navigationTimeout: 45_000,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    // Burp uses a self-signed CA; accept it when proxying.
    ignoreHTTPSErrors: !!BURP_PROXY,
    ...(BURP_PROXY
      ? {
          proxy: { server: BURP_PROXY },
          // Chromium skips the proxy for loopback (localhost) by default, so API
          // calls to localhost:7080 never reach Burp. Disable that bypass so ALL
          // browser traffic (incl. localhost:7080 XHR) is captured for review.
          launchOptions: { args: ['--proxy-bypass-list=<-loopback>'] }
        }
      : {})
  },

  projects: [
    // 1) Logs in once per role and writes storageState to e2e/.auth/*.json
    { name: 'setup', testMatch: /.*\.setup\.ts/ },

    // 2) All specs run authenticated as admin by default (storageState).
    //    Specs that need the unauthenticated state override it locally with
    //    test.use({ storageState: { cookies: [], origins: [] } }).
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'e2e/.auth/admin.json'
      },
      dependencies: ['setup'],
      testMatch: /specs\/.*\.spec\.ts/
    }
  ],

  /**
   * Auto-starts `next dev` if it isn't already running. When BURP_PROXY is set
   * we also push HTTP(S)_PROXY into the dev-server env so NextAuth's SERVER-SIDE
   * /auth/login call is captured by Burp too (the browser proxy alone can't see
   * server-side fetches). If you already run `npm run dev`, it is reused and you
   * should start that process with the proxy env yourself to capture login.
   */
  webServer: {
    command: 'npm run dev',
    // The frontend repo lives outside this testing folder — see FRONTEND_DIR.
    cwd: FRONTEND_DIR,
    url: BASE_URL,
    timeout: 120_000,
    reuseExistingServer: true,
    env: BURP_PROXY
      ? {
          HTTP_PROXY: BURP_PROXY,
          HTTPS_PROXY: BURP_PROXY,
          NODE_TLS_REJECT_UNAUTHORIZED: '0'
        }
      : {}
  }
});
