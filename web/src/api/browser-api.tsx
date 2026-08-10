import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import type {
  ActorContextV1,
  BrowserCommandEnvelopeV1,
  BrowserCommandResultV1,
  NavigationProjectionV1,
} from "./browser-contracts";
import { apiFetch, executeBrowserCommand, loadBrowserContext, type ApiState } from "./browser-client";

export type { ApiState };

interface BrowserApiContextValue {
  actor: ActorContextV1 | null;
  navigation: NavigationProjectionV1 | null;
  apiState: ApiState;
  errorMessage: string | null;
  refresh: () => Promise<void>;
  executeCommand: (command: Omit<BrowserCommandEnvelopeV1, "schema_version">) => Promise<BrowserCommandResultV1>;
  fetchJson: <T>(path: string) => Promise<T>;
}

const BrowserApiContext = createContext<BrowserApiContextValue | null>(null);

export function BrowserApiProvider({ children }: { children: ReactNode }) {
  const [actor, setActor] = useState<ActorContextV1 | null>(null);
  const [navigation, setNavigation] = useState<NavigationProjectionV1 | null>(null);
  const [apiState, setApiState] = useState<ApiState>("loading");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setApiState("loading");
    setErrorMessage(null);

    try {
      const context = await loadBrowserContext();
      setActor(context.actor);
      setNavigation(context.navigation);
      setApiState("ready");
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unknown error";
      if (message === "unauthenticated") {
        setActor(null);
        setNavigation(null);
        setApiState("idle");
        return;
      }

      if (message === "protected" || message.includes("Access")) {
        setApiState("denied");
        setErrorMessage(message);
        return;
      }

      setApiState("error");
      setErrorMessage(message);
    }
  }, []);

  useEffect(() => {
    queueMicrotask(() => {
      void refresh();
    });
  }, [refresh]);

  const fetchJson = useCallback(<T,>(path: string) => apiFetch<T>(path), []);

  const executeCommand = useCallback(
    async (command: Omit<BrowserCommandEnvelopeV1, "schema_version">) => {
      const result = await executeBrowserCommand(command);
      await refresh();
      return result;
    },
    [refresh],
  );

  const value = useMemo(
    () => ({ actor, navigation, apiState, errorMessage, refresh, executeCommand, fetchJson }),
    [actor, navigation, apiState, errorMessage, refresh, executeCommand, fetchJson],
  );

  return <BrowserApiContext.Provider value={value}>{children}</BrowserApiContext.Provider>;
}

export function useBrowserApi() {
  const context = useContext(BrowserApiContext);
  if (!context) {
    throw new Error("useBrowserApi must be used within BrowserApiProvider");
  }
  return context;
}