import { expect, test } from "@playwright/test";
import {
  apiUrl,
  assertOpaqueCookieAttributes,
  captureAuthorizationRequest,
  keycloakAdminLogout,
  finishRpInitiatedLogout,
  scanStorageForProviderTokens,
  sessionProjection,
  signInThroughKeycloak,
  signOutThroughProductionChrome,
  sqlScalar,
  storageSnapshot,
  syntheticUsers,
} from "../helpers/oidc";

test.describe.configure({ mode: "serial" });

test("OIDC-E2E-01 PKCE login [OIDC-E2E-01]", async ({ page, context }) => {
  await page.goto("/");
  await expect(page.getByRole("button", { name: "Continue to sign in" })).toBeVisible();
  const authorization = captureAuthorizationRequest(page);
  await signInThroughKeycloak(
    page,
    syntheticUsers.administrator.username,
    syntheticUsers.administrator.password,
  );
  const authorizeUrl = await authorization;
  expect(authorizeUrl.searchParams.get("code_challenge_method")).toBe("S256");
  expect(authorizeUrl.searchParams.get("code_challenge")).toBeTruthy();
  await expect(page.getByRole("heading", { name: "Home" })).toBeVisible({ timeout: 30_000 });
  const storageHits = scanStorageForProviderTokens(await storageSnapshot(page));
  expect(storageHits, "provider tokens must not enter browser storage").toEqual([]);
  expect(page.url()).not.toMatch(/access_token|id_token|refresh_token/);
  const cookie = (await context.cookies()).find((item) => item.name === "flex_agent_application_session");
  assertOpaqueCookieAttributes(cookie, page.url());
  const session = await sessionProjection(page);
  expect(session.authenticated).toBe(true);
});

test("OIDC-E2E-02 cookie and protected authority [OIDC-E2E-02]", async ({ page, context, request }) => {
  await page.goto("/");
  await signInThroughKeycloak(
    page,
    syntheticUsers.administrator.username,
    syntheticUsers.administrator.password,
  );
  await expect(page.getByRole("heading", { name: "Home" })).toBeVisible({ timeout: 30_000 });
  const cookie = (await context.cookies()).find((item) => item.name === "flex_agent_application_session");
  assertOpaqueCookieAttributes(cookie, page.url());
  const shell = await page.evaluate(async () => {
    const response = await fetch("/v1/assessment/shell", { credentials: "same-origin" });
    return { status: response.status, body: await response.json() };
  });
  expect(shell.status).toBe(200);
  expect(shell.body).not.toHaveProperty("provider_roles");
  const activities = await page.evaluate(async () => {
    const response = await fetch("/v1/assessment/activities", { credentials: "same-origin" });
    return response.status;
  });
  expect(activities).toBe(200);
  const anonymous = await request.get(apiUrl("/v1/assessment/activities"));
  expect(anonymous.status()).toBe(401);
});

test("OIDC-E2E-03 local logout [OIDC-E2E-03]", async ({ page, context, request }) => {
  await page.goto("/");
  await signInThroughKeycloak(
    page,
    syntheticUsers.administrator.username,
    syntheticUsers.administrator.password,
  );
  await expect(page.getByRole("heading", { name: "Home" })).toBeVisible({ timeout: 30_000 });
  const cookieValue = (await context.cookies()).find((item) => item.name === "flex_agent_application_session")?.value;
  expect(cookieValue).toBeTruthy();
  await signOutThroughProductionChrome(page);
  await finishRpInitiatedLogout(page);
  const session = await sessionProjection(page);
  expect(session.authenticated).toBe(false);
  await page.getByRole("button", { name: "Continue to sign in" }).click();
  await expect(page.locator("#username")).toBeVisible({ timeout: 30_000 });
  const replay = await request.get(apiUrl("/v1/assessment/activities"), {
    headers: { Cookie: `flex_agent_application_session=${cookieValue}` },
  });
  expect(replay.status()).toBe(401);
});

test("OIDC-E2E-04 provider-forced logout [OIDC-E2E-04]", async ({ page }) => {
  await page.goto("/");
  await signInThroughKeycloak(
    page,
    syntheticUsers.administrator.username,
    syntheticUsers.administrator.password,
  );
  await expect(page.getByRole("heading", { name: "Home" })).toBeVisible({ timeout: 30_000 });
  keycloakAdminLogout(syntheticUsers.administrator.username);
  const deadline = Date.now() + 60_000;
  let authenticated = true;
  while (Date.now() < deadline) {
    const session = await sessionProjection(page);
    authenticated = session.authenticated;
    if (!authenticated) {
      break;
    }
    await page.waitForTimeout(1000);
  }
  expect(authenticated).toBe(false);
  const live = sqlScalar(
    `SELECT count(*) FROM application_sessions WHERE subject = '${syntheticUsers.administrator.subject}' AND revoked_at IS NULL AND rotated_at IS NULL;`,
  );
  expect(live).toBe("0");
});

test("OIDC-E2E-05A unbound identity [OIDC-E2E-05A]", async ({ page }) => {
  const bindingsBefore = sqlScalar(
    `SELECT count(*) FROM human_identity_bindings WHERE subject = '${syntheticUsers.unbound.subject}';`,
  );
  await page.goto("/");
  await signInThroughKeycloak(page, syntheticUsers.unbound.username, syntheticUsers.unbound.password);
  await finishRpInitiatedLogout(page);
  await expect(page.getByRole("heading", { name: "Sign-in could not be completed" })).toBeVisible();
  await expect(page).not.toHaveURL(/\/auth\/callback/);
  expect(page.url()).not.toMatch(/unknown_subject|authn\./);
  const session = await sessionProjection(page);
  expect(session.authenticated).toBe(false);
  expect(sqlScalar(
    `SELECT count(*) FROM human_identity_bindings WHERE subject = '${syntheticUsers.unbound.subject}';`,
  )).toBe(bindingsBefore);
  expect(sqlScalar(
    `SELECT count(*) FROM application_sessions WHERE subject = '${syntheticUsers.unbound.subject}';`,
  )).toBe("0");
  const denial = sqlScalar(
    "SELECT count(*) FROM authentication_security_events WHERE event_type = 'login_denied' AND reason_code = 'authn.unknown_subject';",
  );
  expect(Number(denial)).toBeGreaterThan(0);
  await page.getByRole("button", { name: "Continue to sign in" }).click();
  await expect(page.locator("#username")).toBeVisible({ timeout: 30_000 });
});

test("OIDC-E2E-05B ambiguous and zero organization [OIDC-E2E-05B]", async ({ page }) => {
  for (const user of [syntheticUsers.zeroOrg, syntheticUsers.ambiguous]) {
    const context = await page.context().browser()?.newContext();
    if (!context) {
      throw new Error("browser context was unavailable");
    }
    const isolated = await context.newPage();
    await isolated.goto("/");
    await signInThroughKeycloak(isolated, user.username, user.password);
    await finishRpInitiatedLogout(isolated);
    await expect(isolated.getByRole("heading", { name: "Sign-in could not be completed" })).toBeVisible();
    const session = await sessionProjection(isolated);
    expect(session.authenticated).toBe(false);
    expect(sqlScalar(
      `SELECT count(*) FROM application_sessions WHERE subject = '${user.subject}';`,
    )).toBe("0");
    await context.close();
  }
  const reasons = sqlScalar(
    "SELECT count(*) FROM authentication_security_events WHERE event_type = 'login_denied' AND reason_code IN ('authn.zero_organization_context','authn.ambiguous_organization_context');",
  );
  expect(Number(reasons)).toBeGreaterThan(0);
});

test("OIDC-E2E-06 public route boundary [OIDC-E2E-06]", async ({ request }) => {
  expect((await request.get(apiUrl("/realms/flex-agent"))).ok()).toBeTruthy();
  expect((await request.get(apiUrl("/resources/"))).status()).toBeLessThan(500);
  expect((await request.get(apiUrl("/admin"))).status()).toBe(404);
  expect((await request.get(apiUrl("/health"))).status()).toBe(404);
  expect((await request.get(apiUrl("/metrics"))).status()).toBe(404);
  expect((await request.get(apiUrl("/realms/master"))).status()).toBe(404);
  expect((await request.get(apiUrl("/browser"))).status()).toBe(404);
  expect((await request.get(apiUrl("/v1/assessment/activities"))).status()).toBe(401);
});
