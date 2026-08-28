import { execFileSync } from "node:child_process";
import type { Cookie, Page, Request } from "@playwright/test";
import { expect } from "@playwright/test";

const root = process.env.FLEXAGENT_ROOT ?? process.cwd();
const composeProject = process.env.FLEXAGENT_COMPOSE_PROJECT ?? "flex-agent-authenticated-browser";
const composeFile = `${root}/deploy/compose/authenticated-browser.compose.yaml`;

const demoPassword = process.env.FLEXAGENT_OIDC_DEMO_PASSWORD ?? "zaQ@123456!";

export const syntheticUsers = {
  administrator: {
    username: process.env.FLEXAGENT_OIDC_ADMIN_USERNAME ?? "demo.admin",
    password: process.env.FLEXAGENT_OIDC_ADMIN_PASSWORD ?? demoPassword,
    subject: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
  },
  participant: {
    username: process.env.FLEXAGENT_OIDC_PARTICIPANT_USERNAME ?? "demo.participant",
    password: process.env.FLEXAGENT_OIDC_PARTICIPANT_PASSWORD ?? demoPassword,
    subject: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
  },
  unbound: {
    username: process.env.FLEXAGENT_OIDC_UNBOUND_USERNAME ?? "demo.unbound",
    password: process.env.FLEXAGENT_OIDC_UNBOUND_PASSWORD ?? demoPassword,
    subject: "ffffffff-ffff-4fff-8fff-ffffffffffff",
  },
  zeroOrg: {
    username: process.env.FLEXAGENT_OIDC_ZEROORG_USERNAME ?? "demo.zeroorg",
    password: process.env.FLEXAGENT_OIDC_ZEROORG_PASSWORD ?? demoPassword,
    subject: "11111111-1111-4111-8111-111111111111",
  },
  ambiguous: {
    username: process.env.FLEXAGENT_OIDC_AMBIGUOUS_USERNAME ?? "demo.ambiguous",
    password: process.env.FLEXAGENT_OIDC_AMBIGUOUS_PASSWORD ?? demoPassword,
    subject: "22222222-2222-4222-8222-222222222222",
  },
};

export function scanStorageForProviderTokens(values: Record<string, string>): string[] {
  const hits: string[] = [];
  for (const [key, value] of Object.entries(values)) {
    const blob = `${key}=${value}`;
    if (/(access_token|id_token|refresh_token|eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]+\.)/i.test(blob)) {
      hits.push(key);
    }
  }
  return hits;
}

export async function storageSnapshot(page: Page): Promise<Record<string, string>> {
  return page.evaluate(() => {
    const bag: Record<string, string> = {};
    for (let index = 0; index < localStorage.length; index += 1) {
      const key = localStorage.key(index);
      if (key) {
        bag[`local:${key}`] = localStorage.getItem(key) ?? "";
      }
    }
    for (let index = 0; index < sessionStorage.length; index += 1) {
      const key = sessionStorage.key(index);
      if (key) {
        bag[`session:${key}`] = sessionStorage.getItem(key) ?? "";
      }
    }
    return bag;
  });
}

export function assertOpaqueCookieAttributes(cookie: Cookie | undefined, origin: string): void {
  expect(cookie, "opaque application cookie").toBeTruthy();
  expect(cookie?.httpOnly).toBe(true);
  expect(cookie?.sameSite.toLowerCase()).toBe("lax");
  expect(cookie?.name).toBe("flex_agent_application_session");
  const secureRequired = new URL(origin).protocol === "https:";
  expect(cookie?.secure, "Secure follows STACK-DEC-27 request scheme").toBe(secureRequired);
}

export async function captureAuthorizationRequest(page: Page): Promise<URL> {
  const pending = new Promise<URL>((resolve) => {
    const onRequest = (request: Request) => {
      const url = request.url();
      if (url.includes("/protocol/openid-connect/auth")) {
        page.off("request", onRequest);
        resolve(new URL(url));
      }
    };
    page.on("request", onRequest);
  });
  return pending;
}

export async function signInThroughKeycloak(page: Page, username: string, password: string): Promise<void> {
  await page.getByRole("button", { name: "Continue to sign in" }).click();
  await page.locator("#username").waitFor({ state: "visible" });
  await page.locator("#username").fill(username);
  await page.locator("#password").fill(password);
  await page.locator("#kc-login").click();
}

export async function finishRpInitiatedLogout(page: Page): Promise<void> {
  const signIn = page.getByRole("button", { name: "Continue to sign in" });
  const confirm = page.locator("#kc-logout");
  await Promise.race([
    signIn.waitFor({ state: "visible", timeout: 30_000 }),
    confirm.waitFor({ state: "visible", timeout: 30_000 }),
  ]);
  if (await confirm.isVisible()) {
    await confirm.click();
  }
  await expect(signIn).toBeVisible({ timeout: 30_000 });
}

export async function sessionProjection(page: Page): Promise<{ authenticated: boolean }> {
  return page.evaluate(async () => {
    const response = await fetch("/auth/session", { credentials: "same-origin" });
    return response.json();
  });
}

export function composeExec(service: string, command: string[]): string {
  return execFileSync(
    "docker",
    ["compose", "-f", composeFile, "--project-name", composeProject, "exec", "-T", service, ...command],
    { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] },
  );
}

export function sqlScalar(sql: string): string {
  return composeExec("postgres", ["psql", "-U", "flexagent", "-d", "flexagent", "-At", "-c", sql]).trim();
}

export function keycloakAdminLogout(username: string): void {
  execFileSync("bash", [`${root}/build/scripts/oidc-keycloak-admin-logout.sh`, username], {
    encoding: "utf8",
    stdio: ["ignore", "pipe", "pipe"],
    env: process.env,
  });
}
