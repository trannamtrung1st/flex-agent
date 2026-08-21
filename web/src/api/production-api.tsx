import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";

export type ProductionApiState = "loading" | "idle" | "ready" | "denied";

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
  fetchJson: <T>(path: string, init?: RequestInit) => Promise<T>;
  login: () => void;
}

const ProductionApiContext = createContext<ProductionApiValue | null>(null);

export function ProductionApiProvider({ children }: { children: ReactNode }) {
  const csrfRef = useRef<string | null>(null);
  const [apiState, setApiState] = useState<ProductionApiState>("loading");
  const [csrfToken, setCsrfToken] = useState<string | null>(null);
  const [shell, setShell] = useState<ProductionShellContextV1 | null>(null);

  const fetchJson = useCallback(async <T,>(path: string, init?: RequestInit): Promise<T> => {
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
    if (response.status === 401) {
      csrfRef.current = null;
      setCsrfToken(null);
      setShell(null);
      setApiState("idle");
      throw new Error("Session expired");
    }

    if (!response.ok) {
      throw new Error("Request failed");
    }

    return (await response.json()) as T;
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    void (async () => {
      try {
        const session = await fetch("/auth/session", {
          credentials: "same-origin",
          signal: controller.signal,
        }).then((response) => response.json()) as {
          authenticated: boolean;
          csrf_token?: string;
        };

        csrfRef.current = session.csrf_token ?? null;
        setCsrfToken(session.csrf_token ?? null);
        if (!session.authenticated) {
          setApiState("idle");
          return;
        }

        const nextShell = await fetch("/v1/assessment/shell", {
          credentials: "same-origin",
          signal: controller.signal,
        }).then((response) => {
          if (!response.ok) {
            throw new Error("shell");
          }

          return response.json() as Promise<ProductionShellContextV1>;
        });

        setShell(nextShell);
        setApiState("ready");
      } catch (error) {
        if (controller.signal.aborted) {
          return;
        }

        setApiState("idle");
        void error;
      }
    })();

    return () => {
      controller.abort();
    };
  }, []);

  const value = useMemo<ProductionApiValue>(
    () => ({
      apiState,
      csrfToken,
      shell,
      fetchJson,
      login: () => {
        window.location.assign("/auth/login?return_path=/activities");
      },
    }),
    [apiState, csrfToken, fetchJson, shell],
  );

  return <ProductionApiContext.Provider value={value}>{children}</ProductionApiContext.Provider>;
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
