import { expect, test } from "@playwright/test";

const API_BASE = process.env.E2E_API_BASE ?? "http://localhost:18080";
const HARNESS_API_KEY = process.env.SYNTHETIC_BROWSER_HARNESS_KEY ?? "flex-agent-synthetic-harness-dev";

async function createGrant(
  request: {
    post: (
      url: string,
      options?: { data?: unknown; headers?: Record<string, string> },
    ) => Promise<{ json: () => Promise<unknown> }>;
  },
  scenarioId: string,
  actorStage: string,
): Promise<string> {
  const response = await request.post(`${API_BASE}/browser/harness/scenario-grants`, {
    data: { scenario_id: scenarioId, actor_stage: actorStage },
    headers: { "X-Synthetic-Harness-Key": HARNESS_API_KEY },
  });
  const body = (await response.json()) as { grant_token: string };
  return body.grant_token;
}

async function exchangeGrant(page: import("@playwright/test").Page, grantToken: string): Promise<void> {
  await page.goto("/");
  await page.getByLabel(/grant token/i).fill(grantToken);
  await page.getByRole("button", { name: /exchange grant/i }).click();
  await expect(page.getByRole("heading", { name: /^home$/i })).toBeVisible({ timeout: 15000 });
}

async function exchangeGrantExpectDenied(page: import("@playwright/test").Page, grantToken: string): Promise<void> {
  await page.goto("/");
  await page.getByLabel(/grant token/i).fill(grantToken);
  await page.getByRole("button", { name: /exchange grant/i }).click();
  await expect(page.getByRole("heading", { name: /access denied/i })).toBeVisible({ timeout: 15000 });
}

test.describe("Synthetic P0 activity journey", () => {
  test("administrator grant opens home with navigation", async ({ page, request }) => {
    const grantToken = await createGrant(request, "campaign-full-journey", "administrator");
    await exchangeGrant(page, grantToken);

    await expect(page.getByRole("heading", { name: /^home$/i })).toBeVisible();
    await expect(page.getByRole("navigation", { name: /primary navigation/i })).toBeVisible();
    await expect(page.getByRole("link", { name: /^activities$/i })).toBeVisible();
    await expect(page.getByRole("link", { name: /governance/i })).toBeVisible();
  });

  test("unauthenticated visitor sees auth gate", async ({ page }) => {
    await page.goto("/");

    await expect(page.getByRole("heading", { name: /sign in required/i })).toBeVisible();
    await expect(page.getByLabel(/grant token/i)).toBeVisible();
  });

  test("administrator activates cohort through activities UI", async ({ page, request }) => {
    const grantToken = await createGrant(request, "campaign-full-journey", "administrator");
    await exchangeGrant(page, grantToken);

    await page.getByRole("link", { name: /^activities$/i }).click();
    await page.getByRole("link", { name: /synthetic assessment campaign/i }).click();

    await page.getByRole("button", { name: /save draft/i }).click();
    await page.getByRole("button", { name: /activate cohort/i }).click();

    await expect(page.getByText(/activated/i).first()).toBeVisible();
  });

  test("participant is denied activities administration", async ({ page, request }) => {
    const grantToken = await createGrant(request, "campaign-full-journey", "participant");
    await exchangeGrant(page, grantToken);

    await page.goto("/activities");

    await expect(page.getByRole("heading", { name: /access denied/i })).toBeVisible();
  });

  test("denied scenario shows access changed", async ({ page, request }) => {
    const grantToken = await createGrant(request, "denied-access", "administrator");
    await exchangeGrantExpectDenied(page, grantToken);
  });
});
