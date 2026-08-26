import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Fragment, useState, type ReactNode } from "react";

export const flexQueryAuthContextKey = ["flex-query", "auth-context"] as const;

export interface FlexQueryAuthContext {
  actorId: string;
  organizationId: string;
  epoch: number;
}

export function authSubtreeKey(
  identity: FlexQueryAuthContext | null | undefined,
  fallback = "unauthenticated",
) {
  if (!identity?.actorId || !identity.organizationId) {
    return fallback;
  }

  return `${identity.actorId}:${identity.organizationId}:${String(identity.epoch)}`;
}

export function AuthScopedSubtree({
  scopeKey,
  children,
}: {
  scopeKey: string;
  children: ReactNode;
}) {
  return <Fragment key={scopeKey}>{children}</Fragment>;
}

export function createFlexQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        refetchOnWindowFocus: false,
      },
      mutations: {
        retry: false,
      },
    },
  });
}

export function purgeProtectedQueryCache(queryClient: QueryClient) {
  void queryClient.cancelQueries();
  queryClient.clear();
}

export function replaceTrustedAuthorizationContext(
  queryClient: QueryClient,
  identity: FlexQueryAuthContext,
) {
  purgeProtectedQueryCache(queryClient);
  queryClient.setQueryData(flexQueryAuthContextKey, identity);
}

export function FlexQueryProvider({
  children,
  client,
}: {
  children: ReactNode;
  client?: QueryClient;
}) {
  const [ownedClient] = useState(() => client ?? createFlexQueryClient());
  return <QueryClientProvider client={client ?? ownedClient}>{children}</QueryClientProvider>;
}
