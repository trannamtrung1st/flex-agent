import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";

export const flexQueryAuthContextKey = ["flex-query", "auth-context"] as const;

export interface FlexQueryAuthContext {
  actorId: string;
  organizationId: string;
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

export function rememberQueryAuthContext(
  queryClient: QueryClient,
  identity: FlexQueryAuthContext,
): boolean {
  const previous = queryClient.getQueryData<FlexQueryAuthContext>(flexQueryAuthContextKey);
  const replaced = Boolean(
    previous
    && (previous.actorId !== identity.actorId || previous.organizationId !== identity.organizationId),
  );
  if (replaced) {
    purgeProtectedQueryCache(queryClient);
  }

  queryClient.setQueryData(flexQueryAuthContextKey, identity);
  return replaced;
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
