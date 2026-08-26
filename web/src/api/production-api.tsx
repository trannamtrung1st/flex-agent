import { useQueryClient } from "@tanstack/react-query";
import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { ProductionApiError } from "./production-api-error";
import {
  completeProductionLogout,
  isKnownPreLogoutRejection,
  SignOutUnconfirmedCopy,
} from "./production-logout";
import { purgeProtectedQueryCache, replaceTrustedAuthorizationContext, authSubtreeKey, AuthScopedSubtree } from "./query-client";

export { ProductionApiError } from "./production-api-error";

export type ProductionApiState = "loading" | "idle" | "ready" | "denied" | "signing-out";

export interface ProductionShellContextV1 {
  schema_version: string;
  actor_id: string;
  organization_id: string;
  relationship: string;
  navigation: Array<{ destination_id: string; is_available: boolean }>;
  permitted_actions: string[];
}

interface ProductionApiValue {
  apiState: ProductionApiState;
  csrfToken: string | null;
  shell: ProductionShellContextV1 | null;
  errorMessage: string | null;
  fetchJson: <T>(path: string, init?: RequestInit) => Promise<T>;
  login: () => void;
  logout: () => Promise<void>;
  authContextEpoch: number;
}

const ProductionApiContext = createContext<ProductionApiValue | null>(null);

let reloadTrustedContextForTestsImpl: (() => Promise<void>) | null = null;

export function reloadTrustedContextForTests() {
  if (!reloadTrustedContextForTestsImpl) {
    throw new Error("ProductionApiProvider is not mounted");
  }

  return reloadTrustedContextForTestsImpl();
}

export function ProductionApiProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const csrfRef = useRef<string | null>(null);
  const generationRef = useRef(0);
  const authContextEpochRef = useRef(0);
  const [apiState, setApiState] = useState<ProductionApiState>("loading");
  const [csrfToken, setCsrfToken] = useState<string | null>(null);
  const [shell, setShell] = useState<ProductionShellContextV1 | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [authContextEpoch, setAuthContextEpoch] = useState(0);

  const clearProtectedState = useCallback((next: ProductionApiState, message: string | null = null) => {
    purgeProtectedQueryCache(queryClient);
    generationRef.current += 1;
    csrfRef.current = null;
    setCsrfToken(null);
    setShell(null);
    setErrorMessage(message);
    setApiState(next);
  }, [queryClient]);

  const fetchJson = useCallback(async <T,>(path: string, init?: RequestInit): Promise<T> => {
    const generation = generationRef.current;
    const headers = new Headers(init?.headers);
    headers.set("Accept", "application/json");
    if (init?.method && init.method !== "GET" && csrfRef.current) {
      headers.set("X-Flex-CSRF", csrfRef.current);
    }

    const response = await fetch(path, {
      ...init,
      headers,
      credentials: "same-origin",
    });
    if (generation !== generationRef.current) {
      throw new ProductionApiError(0, "Stale response");
    }

    const outcomeCode = response.ok ? undefined : await readOutcomeCode(response);

    if (response.status === 401) {
      clearProtectedState("idle");
      throw new ProductionApiError(401, "Session expired", outcomeCode);
    }

    if (response.status === 403) {
      clearProtectedState("denied", "Your access changed");
      throw new ProductionApiError(403, "Your access changed", outcomeCode);
    }

    if (!response.ok) {
      throw new ProductionApiError(response.status, "Request failed", outcomeCode);
    }

    if (response.status === 204) {
      return undefined as T;
    }

    return (await response.json()) as T;
  }, [clearProtectedState]);

  const bootstrap = useCallback(async (signal?: AbortSignal) => {
    try {
      const sessionResponse = await fetch("/auth/session", {
        credentials: "same-origin",
        signal,
      });
      if (!sessionResponse.ok) {
        purgeProtectedQueryCache(queryClient);
        setApiState("idle");
        return;
      }

      const session = await sessionResponse.json() as {
        authenticated: boolean;
        csrf_token?: string;
      };

      csrfRef.current = session.csrf_token ?? null;
      setCsrfToken(session.csrf_token ?? null);
      if (!session.authenticated) {
        purgeProtectedQueryCache(queryClient);
        setApiState("idle");
        return;
      }

      const shellResponse = await fetch("/v1/assessment/shell", {
        credentials: "same-origin",
        signal,
      });
      if (shellResponse.status === 401) {
        clearProtectedState("idle");
        return;
      }

      if (!shellResponse.ok) {
        clearProtectedState("denied", "Your access changed");
        return;
      }

      const nextShell = await shellResponse.json() as ProductionShellContextV1;
      authContextEpochRef.current += 1;
      generationRef.current += 1;
      replaceTrustedAuthorizationContext(queryClient, {
        actorId: nextShell.actor_id,
        organizationId: nextShell.organization_id,
        epoch: authContextEpochRef.current,
      });
      setAuthContextEpoch(authContextEpochRef.current);
      setShell(nextShell);
      setErrorMessage(null);
      setApiState("ready");
    } catch (error) {
      if (signal?.aborted) {
        return;
      }

      purgeProtectedQueryCache(queryClient);
      setApiState("idle");
      void error;
    }
  }, [clearProtectedState, queryClient]);

  useEffect(() => {
    const controller = new AbortController();
    void Promise.resolve().then(() => bootstrap(controller.signal));
    return () => {
      controller.abort();
    };
  }, [bootstrap]);

  useEffect(() => {
    reloadTrustedContextForTestsImpl = () => bootstrap();
    return () => {
      reloadTrustedContextForTestsImpl = null;
    };
  }, [bootstrap]);

  const value = useMemo<ProductionApiValue>(
    () => ({
      apiState,
      csrfToken,
      shell,
      errorMessage,
      fetchJson,
      authContextEpoch,
      login: () => {
        const path = `${window.location.pathname}${window.location.search}`;
        const safe = path.startsWith("/") && !path.startsWith("//") && !path.includes("://") ? path : "/";
        window.location.assign(`/auth/login?return_path=${encodeURIComponent(safe)}`);
      },
      logout: async () => {
        const csrf = csrfRef.current;
        clearProtectedState("signing-out");
        try {
          const nextLocation = await completeProductionLogout(csrf);
          window.location.assign(nextLocation);
        } catch (caught: unknown) {
          if (isKnownPreLogoutRejection(caught)) {
            await bootstrap();
            return;
          }

          setErrorMessage(SignOutUnconfirmedCopy);
          setApiState("signing-out");
        }
      },
    }),
    [apiState, authContextEpoch, bootstrap, clearProtectedState, csrfToken, errorMessage, fetchJson, shell],
  );

  return <ProductionApiContext.Provider value={value}>{children}</ProductionApiContext.Provider>;
}

export function ProtectedAuthSubtree({ children }: { children: ReactNode }) {
  const { apiState, authContextEpoch, shell } = useProductionApi();
  return (
    <AuthScopedSubtree
      scopeKey={authSubtreeKey(
        shell
          ? { actorId: shell.actor_id, organizationId: shell.organization_id, epoch: authContextEpoch }
          : undefined,
        apiState,
      )}
    >
      {children}
    </AuthScopedSubtree>
  );
}

export function useProductionApi() {
  const value = useContext(ProductionApiContext);
  if (!value) {
    throw new Error("ProductionApiProvider is required");
  }

  return value;
}

export function isProductionApiMode() {
  return import.meta.env.VITE_API_MODE === "production";
}

async function readOutcomeCode(response: Response) {
  try {
    const body = await response.clone().json() as { error?: string; outcome_code?: string };
    return body.outcome_code ?? body.error;
  } catch {
    return undefined;
  }
}
