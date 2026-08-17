import { expect, test } from "@playwright/test";
import { randomBytes } from "node:crypto";

const API_BASE = process.env.E2E_API_BASE ?? "http://localhost:18080";
const HARNESS_API_KEY = process.env.SYNTHETIC_BROWSER_HARNESS_KEY ?? "";

function newInstanceId(): string {
  return randomBytes(16).toString("hex");
}

async function createGrant(
  request: {
    post: (
      url: string,
      options?: { data?: unknown; headers?: Record<string, string> },
    ) => Promise<{ json: () => Promise<unknown> }>;
  },
  scenarioId: string,
  actorStage: string,
  scenarioInstanceId: string,
): Promise<string> {
  const response = await request.post(`${API_BASE}/browser/harness/scenario-grants`, {
    data: {
      scenario_id: scenarioId,
      actor_stage: actorStage,
      scenario_instance_id: scenarioInstanceId,
    },
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

async function logout(page: import("@playwright/test").Page): Promise<void> {
  await page.request.post(`${API_BASE}/browser/auth/logout`);
}

test.describe("Synthetic P0 activity journey", () => {
  test("administrator grant opens home with navigation", async ({ page, request }) => {
    const instanceId = newInstanceId();
    const grantToken = await createGrant(request, "campaign-full-journey", "administrator", instanceId);
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
    const instanceId = newInstanceId();
    const grantToken = await createGrant(request, "campaign-full-journey", "administrator", instanceId);
    await exchangeGrant(page, grantToken);

    await page.getByRole("link", { name: /^activities$/i }).click();
    await page.getByRole("link", { name: /synthetic assessment campaign/i }).click();

    await page.getByRole("button", { name: /save draft/i }).click();
    await page.getByRole("button", { name: /activate cohort/i }).click();

    await expect(page.getByText(/activated/i).first()).toBeVisible();
  });

  test("participant is denied activities administration", async ({ page, request }) => {
    const instanceId = newInstanceId();
    const grantToken = await createGrant(request, "campaign-full-journey", "participant", instanceId);
    await exchangeGrant(page, grantToken);

    await page.goto("/activities");

    await expect(page.getByRole("heading", { name: /access denied/i })).toBeVisible();
  });

  test("denied scenario shows access changed", async ({ page, request }) => {
    const instanceId = newInstanceId();
    const grantToken = await createGrant(request, "denied-access", "administrator", instanceId);
    await exchangeGrantExpectDenied(page, grantToken);
  });

  test("full campaign journey reaches released participant result", async ({ page, request }) => {
    test.setTimeout(120000);
    const instanceId = newInstanceId();
    const scenarioId = "campaign-full-journey";

    const adminGrant = await createGrant(request, scenarioId, "administrator", instanceId);
    await exchangeGrant(page, adminGrant);
    await page.getByRole("link", { name: /^activities$/i }).click();
    await page.getByRole("link", { name: /synthetic assessment campaign/i }).click();
    await page.getByRole("button", { name: /save draft/i }).click();
    await page.getByRole("button", { name: /activate cohort/i }).click();
    await expect(page.getByText(/activated/i).first()).toBeVisible();
    await page.getByRole("button", { name: /assign participants/i }).click();
    await page.getByRole("button", { name: /assign participant/i }).click();
    await logout(page);

    const participantGrant = await createGrant(request, scenarioId, "participant", instanceId);
    await exchangeGrant(page, participantGrant);
    await page.getByRole("link", { name: /^my work$/i }).click();
    await page.getByLabel(/submission text/i).fill("Synthetic browser journey answer.");
    await page.getByRole("button", { name: /submit text/i }).click();
    await page.getByRole("button", { name: /start attempt/i }).click();
    await page.getByRole("link", { name: /open session/i }).click();
    await page.getByLabel(/your message/i).fill("Ready for synthetic session.");
    await page.getByRole("button", { name: /^send$/i }).click();
    await page.getByRole("button", { name: /complete session/i }).click();
    await page.getByRole("dialog").getByRole("button", { name: /^complete session$/i }).click();
    await logout(page);

    const reviewerGrant = await createGrant(request, scenarioId, "reviewer", instanceId);
    await exchangeGrant(page, reviewerGrant);
    await page.getByRole("link", { name: /review work/i }).click();
    await page.getByRole("link", { name: /synthetic review case/i }).click();
    await page.getByRole("button", { name: /^approve$/i }).click();
    await logout(page);

    const releaseGrant = await createGrant(request, scenarioId, "release_actor", instanceId);
    await exchangeGrant(page, releaseGrant);
    await page.getByRole("link", { name: /release work/i }).click();
    await page.getByRole("link", { name: /synthetic result/i }).click();
    await page.getByRole("button", { name: /release result/i }).first().click();
    await page.getByRole("button", { name: /release result/i }).last().click();
    await logout(page);

    const participantResultGrant = await createGrant(request, scenarioId, "participant", instanceId);
    await exchangeGrant(page, participantResultGrant);
    await page.getByRole("link", { name: /^results$/i }).click();
    await page.getByRole("link", { name: /synthetic assessment campaign/i }).click();
    await expect(page.getByText(/released/i)).toBeVisible();
    await expect(page.getByText(/synthetic released result content/i)).toBeVisible();
  });
});
