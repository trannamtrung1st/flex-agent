import { ProductionApiError } from "./production-api-error";

export const SignOutFailedCopy = "Sign out could not be completed.";
export const SignOutUnconfirmedCopy = "Sign out status could not be confirmed. Try again.";

export function isKnownPreLogoutRejection(error: unknown): error is ProductionApiError {
  return error instanceof ProductionApiError && error.status === 400;
}

export function productionLogoutNextLocation(endSessionUrl: unknown): string {
  if (typeof endSessionUrl !== "string" || endSessionUrl.length === 0) {
    return "/";
  }

  try {
    const uri = new URL(endSessionUrl);
    if (uri.protocol === "https:") {
      return endSessionUrl;
    }
  } catch {
    return "/";
  }

  return "/";
}

export async function completeProductionLogout(
  csrfToken: string | null,
  fetchImpl: typeof fetch = fetch,
): Promise<string> {
  const headers = new Headers();
  if (csrfToken) {
    headers.set("X-Flex-CSRF", csrfToken);
  }

  const response = await fetchImpl("/auth/logout", {
    method: "POST",
    credentials: "same-origin",
    headers,
  });
  if (!response.ok) {
    throw new ProductionApiError(response.status, SignOutFailedCopy);
  }

  const body = await response.json() as { logged_out?: boolean; end_session_url?: string | null };
  if (body.logged_out !== true) {
    throw new ProductionApiError(response.status, SignOutFailedCopy);
  }

  return productionLogoutNextLocation(body.end_session_url);
}
