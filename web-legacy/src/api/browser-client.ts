import type {
  ActorContextV1,
  BrowserCommandEnvelopeV1,
  BrowserCommandReconciliationV1,
  BrowserCommandResultV1,
  NavigationProjectionV1,
} from "./browser-contracts";

export type ApiState = "idle" | "loading" | "protected" | "denied" | "error" | "ready";

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  headers.set("Accept", "application/json");

  const response = await fetch(path, {
    credentials: "include",
    ...init,
    headers,
  });

  if (response.status === 401) {
    throw new Error("unauthenticated");
  }

  if (response.status === 403) {
    const body = (await response.json()) as { safe_message?: string };
    throw new Error(body.safe_message ?? "Access denied");
  }

  if (response.status === 404) {
    throw new Error("protected");
  }

  if (!response.ok) {
    throw new Error(`Request failed: ${String(response.status)}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function loadBrowserContext(): Promise<{
  actor: ActorContextV1;
  navigation: NavigationProjectionV1;
}> {
  const [actor, navigation] = await Promise.all([
    apiFetch<ActorContextV1>("/browser/actor-context"),
    apiFetch<NavigationProjectionV1>("/browser/navigation"),
  ]);
  return { actor, navigation };
}

export async function executeBrowserCommand(
  command: Omit<BrowserCommandEnvelopeV1, "schema_version">,
): Promise<BrowserCommandResultV1> {
  const response = await fetch("/browser/commands", {
    method: "POST",
    credentials: "include",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ schema_version: "v1", ...command }),
  });

  if (response.status === 401) {
    throw new Error("unauthenticated");
  }

  const body = (await response.json()) as BrowserCommandResultV1;

  if (response.status === 403 || response.status === 409) {
    return body;
  }

  if (!response.ok) {
    throw new Error(body.safe_message ?? `Request failed: ${String(response.status)}`);
  }

  return body;
}

export async function reconcileBrowserCommand(
  command: Omit<BrowserCommandEnvelopeV1, "schema_version">,
): Promise<BrowserCommandReconciliationV1> {
  const response = await fetch("/browser/commands/reconcile", {
    method: "POST",
    credentials: "include",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ schema_version: "v1", ...command }),
  });

  if (response.status === 401) {
    throw new Error("unauthenticated");
  }

  const body = (await response.json()) as BrowserCommandReconciliationV1;

  if (response.status === 403) {
    return body;
  }

  if (!response.ok) {
    throw new Error(body.safe_message ?? `Request failed: ${String(response.status)}`);
  }

  return body;
}

export async function exchangeScenarioGrant(grantToken: string): Promise<void> {
  await apiFetch("/browser/auth/exchange", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ grant_token: grantToken }),
  });
}
