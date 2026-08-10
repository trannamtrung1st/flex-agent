import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: "list",
  use: {
    baseURL: "http://localhost:5173",
    trace: "on-first-retry",
  },
  webServer: [
    {
      command:
        "ASPNETCORE_ENVIRONMENT=Development SyntheticBrowser__HarnessApiKey=flex-agent-synthetic-harness-dev dotnet run --project ../src/Hosts/FlexAgent.Api/FlexAgent.Api.csproj --no-launch-profile --urls http://localhost:18080",
      url: "http://localhost:18080/health/live",
      reuseExistingServer: false,
      timeout: 120000,
    },
    {
      command: "bash ../build/scripts/serve-e2e-spa.sh",
      url: "http://localhost:5173",
      reuseExistingServer: false,
      timeout: 180000,
    },
  ],
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
