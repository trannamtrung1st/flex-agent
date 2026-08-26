import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import type {
  ActorContextV1,
  BrowserCommandEnvelopeV1,
  BrowserCommandReconciliationV1,
  BrowserCommandResultV1,
  NavigationProjectionV1,
} from "./browser-contracts";
import { apiFetch, executeBrowserCommand, loadBrowserContext, reconcileBrowserCommand, type ApiState } from "./browser-client";
import { purgeProtectedQueryCache, replaceTrustedAuthorizationContext, authSubtreeKey, AuthScopedSubtree, flexQueryAuthContextKey, type FlexQueryAuthContext } from "./query-client";

export type { ApiState };

interface BrowserApiContextValue {
  actor: ActorContextV1 | null;
  navigation: NavigationProjectionV1 | null;
  apiState: ApiState;
  errorMessage: string | null;
  refresh: (options?: { replaceAuthorizationContext?: boolean }) => Promise<void>;
  executeCommand: (command: Omit<BrowserCommandEnvelopeV1, "schema_version">) => Promise<BrowserCommandResultV1>;
  reconcileCommand: (
    command: Omit<BrowserCommandEnvelopeV1, "schema_version">,
  ) => Promise<BrowserCommandReconciliationV1>;
  fetchJson: <T>(path: string, init?: RequestInit) => Promise<T>;
  authContextEpoch: number;
}

const BrowserApiContext = createContext<BrowserApiContextValue | null>(null);

export function BrowserApiProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const authContextEpochRef = useRef(0);
  const [actor, setActor] = useState<ActorContextV1 | null>(null);
  const [navigation, setNavigation] = useState<NavigationProjectionV1 | null>(null);
  const [apiState, setApiState] = useState<ApiState>("loading");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [authContextEpoch, setAuthContextEpoch] = useState(0);

  const refresh = useCallback(async (options?: { replaceAuthorizationContext?: boolean }) => {
    const replaceRequested = options?.replaceAuthorizationContext === true;
    if (replaceRequested || authContextEpochRef.current === 0) {
      setApiState("loading");
      setErrorMessage(null);
    }

    try {
      const context = await loadBrowserContext();
      const previous = queryClient.getQueryData<FlexQueryAuthContext>(flexQueryAuthContextKey);
      const identityChanged = !previous
        || previous.actorId !== context.actor.actor_id
        || previous.organizationId !== context.actor.organization_id;
      const replaceAuthorizationContext = replaceRequested || identityChanged;

      if (replaceAuthorizationContext) {
        authContextEpochRef.current += 1;
        replaceTrustedAuthorizationContext(queryClient, {
          actorId: context.actor.actor_id,
          organizationId: context.actor.organization_id,
          epoch: authContextEpochRef.current,
        });
        setAuthContextEpoch(authContextEpochRef.current);
      }

      setActor(context.actor);
      setNavigation(context.navigation);
      setErrorMessage(null);
      setApiState("ready");
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unknown error";
      if (message === "unauthenticated") {
        purgeProtectedQueryCache(queryClient);
        setActor(null);
        setNavigation(null);
        setApiState("idle");
        return;
      }

      if (message === "protected" || message.includes("Access")) {
        purgeProtectedQueryCache(queryClient);
        setApiState("denied");
        setErrorMessage(message);
        return;
      }

      purgeProtectedQueryCache(queryClient);
      setApiState("error");
      setErrorMessage(message);
    }
  }, [queryClient]);

  useEffect(() => {
    queueMicrotask(() => {
      void refresh({ replaceAuthorizationContext: true });
    });
  }, [refresh]);

  const fetchJson = useCallback(<T,>(path: string, init?: RequestInit) => apiFetch<T>(path, init), []);

  const executeCommand = useCallback(
    async (command: Omit<BrowserCommandEnvelopeV1, "schema_version">) => {
      const result = await executeBrowserCommand(command);
      await refresh({ replaceAuthorizationContext: false });
      return result;
    },
    [refresh],
  );

  const reconcileCommand = useCallback(
    (command: Omit<BrowserCommandEnvelopeV1, "schema_version">) => reconcileBrowserCommand(command),
    [],
  );

  const value = useMemo(
    () => ({ actor, navigation, apiState, errorMessage, refresh, executeCommand, reconcileCommand, fetchJson, authContextEpoch }),
    [actor, navigation, apiState, errorMessage, refresh, executeCommand, reconcileCommand, fetchJson, authContextEpoch],
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

export function ProtectedBrowserAuthSubtree({ children }: { children: ReactNode }) {
  const { actor, apiState, authContextEpoch } = useBrowserApi();
  return (
    <AuthScopedSubtree
      scopeKey={authSubtreeKey(
        actor
          ? { actorId: actor.actor_id, organizationId: actor.organization_id, epoch: authContextEpoch }
          : undefined,
        apiState,
      )}
    >
      {children}
    </AuthScopedSubtree>
  );
}