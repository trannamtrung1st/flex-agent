import { expect, test } from "@playwright/test";
import {
  captureAuthorizationRequest,
  scanStorageForProviderTokens,
  sessionProjection,
  signInThroughKeycloak,
  storageSnapshot,
  syntheticUsers,
} from "../helpers/oidc";

test("OIDC-CANDIDATE-01 Wave 8.1 transition regression candidate/non-Production [OIDC-CANDIDATE-01]", async ({
  page,
}) => {
  test.info().annotations.push({ type: "target", description: "candidate/non-Production" });
  await page.goto("/");
  await expect(page.getByRole("button", { name: "Continue to sign in" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Sign in required" })).toBeVisible();
  const authorization = captureAuthorizationRequest(page);
  await signInThroughKeycloak(
    page,
    syntheticUsers.administrator.username,
    syntheticUsers.administrator.password,
  );
  const authorizeUrl = await authorization;
  expect(authorizeUrl.searchParams.get("code_challenge_method")).toBe("S256");
  await expect(page.getByRole("heading", { name: "Home" })).toBeVisible({ timeout: 30_000 });
  expect(scanStorageForProviderTokens(await storageSnapshot(page))).toEqual([]);
  await page.getByRole("link", { name: "Open Activities" }).click();
  await expect(page.getByRole("heading", { name: /Activities/i })).toBeVisible();
  await page.keyboard.press("Tab");
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.getByRole("button", { name: "Sign out" })).toBeVisible();
  await page.getByRole("button", { name: "Sign out" }).click();
  await expect(page.getByRole("button", { name: "Continue to sign in" })).toBeVisible({ timeout: 30_000 });
  const session = await sessionProjection(page);
  expect(session.authenticated).toBe(false);
});
