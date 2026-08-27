import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e/design-lab",
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: "list",
  use: {
    baseURL: "http://127.0.0.1:5275",
    trace: "on-first-retry",
  },
  webServer: {
    command: "pnpm build:design-lab && pnpm preview:design-lab",
    url: "http://127.0.0.1:5275/design-lab/surfaces",
    reuseExistingServer: !process.env.CI,
    timeout: 180000,
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
