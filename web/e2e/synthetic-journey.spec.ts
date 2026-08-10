import { expect, test } from "@playwright/test";

const API_BASE = "http://localhost:8080";

async function createGrant(
  request: {
    post: (url: string, options?: { data?: unknown }) => Promise<{ json: () => Promise<unknown> }>;
  },
  scenarioId: string,
  actorStage: string,
): Promise<string> {
  const response = await request.post(`${API_BASE}/browser/test/scenario-grants`, {
    data: { scenario_id: scenarioId, actor_stage: actorStage },
  });
  const body = (await response.json()) as { grant_token: string };
  return body.grant_token;
}

test.describe("Synthetic P0 activity journey", () => {
  test("administrator grant opens home with navigation", async ({ page, request }) => {
    const grantToken = await createGrant(request, "campaign-full-journey", "administrator");

    await page.goto(`/?grant=${encodeURIComponent(grantToken)}`);

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
});
