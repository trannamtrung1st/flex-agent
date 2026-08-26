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
import { purgeProtectedQueryCache, replaceTrustedAuthorizationContext, authSubtreeKey, AuthScopedSubtree } from "./query-client";

export type { ApiState };

function isUnauthenticatedError(error: unknown): boolean {
  return error instanceof Error && error.message === "unauthenticated";
}

function actorAuthorizationChanged(previous: ActorContextV1 | null, next: ActorContextV1): boolean {
  if (!previous) {
    return true;
  }

  const previousCapabilities = [...previous.capabilities].sort().join("\0");
  const nextCapabilities = [...next.capabilities].sort().join("\0");
  return previous.actor_id !== next.actor_id
    || previous.organization_id !== next.organization_id
    || previous.actor_stage !== next.actor_stage
    || previousCapabilities !== nextCapabilities;
}

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
  const trustedActorRef = useRef<ActorContextV1 | null>(null);
  const [actor, setActor] = useState<ActorContextV1 | null>(null);
  const [navigation, setNavigation] = useState<NavigationProjectionV1 | null>(null);
  const [apiState, setApiState] = useState<ApiState>("loading");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [authContextEpoch, setAuthContextEpoch] = useState(0);

  const leaveReady = useCallback((nextState: "idle" | "denied" | "error", message: string | null = null) => {
    purgeProtectedQueryCache(queryClient);
    authContextEpochRef.current += 1;
    trustedActorRef.current = null;
    setAuthContextEpoch(authContextEpochRef.current);
    setActor(null);
    setNavigation(null);
    setErrorMessage(message);
    setApiState(nextState);
  }, [queryClient]);

  const withAuthenticationLoss = useCallback(async <T,>(operation: () => Promise<T>): Promise<T> => {
    try {
      return await operation();
    } catch (error) {
      if (isUnauthenticatedError(error)) {
        leaveReady("idle");
      }
      throw error;
    }
  }, [leaveReady]);

  const refresh = useCallback(async (options?: { replaceAuthorizationContext?: boolean }) => {
    const replaceRequested = options?.replaceAuthorizationContext === true;
    if (replaceRequested || authContextEpochRef.current === 0) {
      setApiState("loading");
      setErrorMessage(null);
    }

    try {
      const context = await loadBrowserContext();
      const replaceAuthorizationContext = replaceRequested || actorAuthorizationChanged(trustedActorRef.current, context.actor);

      if (replaceAuthorizationContext) {
        authContextEpochRef.current += 1;
        replaceTrustedAuthorizationContext(queryClient, {
          actorId: context.actor.actor_id,
          organizationId: context.actor.organization_id,
          epoch: authContextEpochRef.current,
        });
        setAuthContextEpoch(authContextEpochRef.current);
      }

      trustedActorRef.current = context.actor;
      setActor(context.actor);
      setNavigation(context.navigation);
      setErrorMessage(null);
      setApiState("ready");
    } catch (error) {
      if (isUnauthenticatedError(error)) {
        leaveReady("idle");
        return;
      }

      const message = error instanceof Error ? error.message : "Unknown error";
      if (message === "protected" || message.includes("Access")) {
        leaveReady("denied", message);
        return;
      }

      leaveReady("error", message);
    }
  }, [leaveReady, queryClient]);

  useEffect(() => {
    queueMicrotask(() => {
      void refresh({ replaceAuthorizationContext: true });
    });
  }, [refresh]);

  const fetchJson = useCallback(
    <T,>(path: string, init?: RequestInit) => withAuthenticationLoss(() => apiFetch<T>(path, init)),
    [withAuthenticationLoss],
  );

  const executeCommand = useCallback(
    async (command: Omit<BrowserCommandEnvelopeV1, "schema_version">) => {
      const result = await withAuthenticationLoss(() => executeBrowserCommand(command));
      await refresh({ replaceAuthorizationContext: false });
      return result;
    },
    [refresh, withAuthenticationLoss],
  );

  const reconcileCommand = useCallback(
    (command: Omit<BrowserCommandEnvelopeV1, "schema_version">) => (
      withAuthenticationLoss(() => reconcileBrowserCommand(command))
    ),
    [withAuthenticationLoss],
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