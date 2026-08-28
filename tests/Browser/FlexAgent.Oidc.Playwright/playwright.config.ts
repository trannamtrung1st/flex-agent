import { defineConfig, devices } from "@playwright/test";

const origin = process.env.FLEXAGENT_OIDC_ORIGIN ?? "http://localhost:18080";
const candidateOrigin = process.env.FLEXAGENT_OIDC_CANDIDATE_ORIGIN ?? "http://localhost:5274";

export default defineConfig({
  testDir: "./specs",
  fullyParallel: false,
  workers: 1,
  forbidOnly: true,
  retries: 0,
  timeout: 120_000,
  reporter: [
    ["list"],
    ["json", { outputFile: process.env.FLEXAGENT_OIDC_REPORT ?? "playwright-report.json" }],
  ],
  use: {
    trace: "off",
    screenshot: "off",
    video: "off",
    ignoreHTTPSErrors: true,
  },
  projects: [
    {
      name: "canonical",
      testMatch: "canonical.spec.ts",
      use: { ...devices["Desktop Chrome"], baseURL: origin },
    },
    {
      name: "candidate-non-Production",
      testMatch: "candidate.spec.ts",
      use: { ...devices["Desktop Chrome"], baseURL: candidateOrigin },
    },
  ],
});
