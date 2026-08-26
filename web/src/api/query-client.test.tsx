import { QueryClient, useQueryClient } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { useEffect, type ReactNode } from "react";
import { App } from "../App";
import { BrowserApiProvider, useBrowserApi } from "./browser-api";
import { ProductionApiProvider, ProtectedAuthSubtree, reloadTrustedContextForTests, useProductionApi } from "./production-api";
import { createFlexQueryClient, FlexQueryProvider } from "./query-client";

const protectedKey = ["assessment", "v1", "activities", "list"] as const;

function CacheProbe({ label }: { label: string }) {
  const queryClient = useQueryClient();
  return <p>{label}:{queryClient.getQueryData<string>(protectedKey) ?? "empty"}</p>;
}

function RenderCounter({ onClient }: { onClient: (client: QueryClient) => void }) {
  const queryClient = useQueryClient();
  onClient(queryClient);
  return <p>ready</p>;
}

function seedProtectedData(client: QueryClient, value: string) {
  client.setQueryData(protectedKey, value);
}

function jsonResponse(status: number, body: unknown) {
  const payload = {
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
    clone() {
      return this;
    },
  };
  return payload;
}

describe("createFlexQueryClient", () => {
  it("disables retries, window-focus refetch, and does not share instances", () => {
    const first = createFlexQueryClient();
    const second = createFlexQueryClient();
    expect(first).not.toBe(second);
    expect(first.getDefaultOptions().queries?.retry).toBe(false);
    expect(first.getDefaultOptions().queries?.refetchOnWindowFocus).toBe(false);
    expect(first.getDefaultOptions().mutations?.retry).toBe(false);
  });
});

describe("FlexQueryProvider isolation", () => {
  it("gives each mounted tree its own client and cache", () => {
    const left = createFlexQueryClient();
    const right = createFlexQueryClient();
    seedProtectedData(left, "actor-a");
    seedProtectedData(right, "actor-b");

    render(
      <>
        <FlexQueryProvider client={left}>
          <CacheProbe label="left" />
        </FlexQueryProvider>
        <FlexQueryProvider client={right}>
          <CacheProbe label="right" />
        </FlexQueryProvider>
      </>,
    );

    expect(screen.getByText("left:actor-a")).toBeInTheDocument();
    expect(screen.getByText("right:actor-b")).toBeInTheDocument();
  });

  it("constructs one owned client across rerenders", () => {
    const seen: QueryClient[] = [];
    function Harness({ marker }: { marker: string }) {
      return (
        <FlexQueryProvider>
          <p>{marker}</p>
          <RenderCounter onClient={(client) => seen.push(client)} />
        </FlexQueryProvider>
      );
    }

    const view = render(<Harness marker="one" />);
    view.rerender(<Harness marker="two" />);
    expect(new Set(seen).size).toBe(1);
  });
});

describe("App Query composition", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("mounts a Query client for the synthetic API branch", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/browser/actor-context")) {
        return Promise.resolve(jsonResponse(200, {
          schema_version: "v1",
          actor_id: "actor.synthetic.admin",
          display_name: "Synthetic Administrator",
          organization_id: "org.synthetic.demo",
          organization_name: "Synthetic Demo Organization",
          capabilities: ["activity_admin"],
          actor_stage: "administrator",
          is_synthetic: true,
        }));
      }

      if (url.includes("/browser/navigation")) {
        return Promise.resolve(jsonResponse(200, {
          schema_version: "v1",
          destinations: [{ destination_id: "home", label: "Home", route: "/", tier: "p0", is_available: true }],
        }));
      }

      if (url.includes("/browser/home")) {
        return Promise.resolve(jsonResponse(200, {
          schema_version: "v1",
          greeting: "Welcome",
          work_items: [],
          permitted_actions: [],
        }));
      }

      return Promise.resolve(jsonResponse(404, {}));
    }));

    render(<App />);
    expect(await screen.findByRole("heading", { name: /^home$/i })).toBeInTheDocument();
  });
});

function authenticatedSessionFetch(overrides?: {
  actorId?: string;
  organizationId?: string;
  resource?: (url: string) => unknown;
}) {
  return vi.fn((input: RequestInfo | URL) => {
    const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
    if (url.includes("/auth/session")) {
      return jsonResponse(200, { authenticated: true, csrf_token: "csrf-1" });
    }

    if (url.includes("/v1/assessment/shell")) {
      return jsonResponse(200, {
        schema_version: "v1",
        actor_id: overrides?.actorId ?? "actor-1",
        organization_id: overrides?.organizationId ?? "org-1",
        relationship: "administrator",
        navigation: [{ destination_id: "activities", is_available: true }],
        permitted_actions: ["assessment.activity.create"],
      });
    }

    if (overrides?.resource) {
      const result = overrides.resource(url);
      if (result) {
        return result;
      }
    }

    return jsonResponse(404, {});
  });
}

function ProductionTree({ children, client }: { children: ReactNode; client: QueryClient }) {
  return (
    <FlexQueryProvider client={client}>
      <ProductionApiProvider>
        {children}
      </ProductionApiProvider>
    </FlexQueryProvider>
  );
}

describe("protected Query cache lifecycle", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("clears cached queries and mutation state on a 403 resource response", async () => {
    const client = createFlexQueryClient();
    seedProtectedData(client, "secret-list");
    client.getMutationCache().build(client, { mutationKey: ["create"] });

    vi.stubGlobal("fetch", authenticatedSessionFetch({
      resource: (url) => {
        if (url.includes("/v1/assessment/source-options")) {
          return jsonResponse(403, { error: "assessment.denied" });
        }

        return undefined;
      },
    }));

    function Probe() {
      const { apiState, fetchJson } = useProductionApi();
      useEffect(() => {
        if (apiState !== "ready") {
          return;
        }

        void fetchJson("/v1/assessment/source-options").catch(() => undefined);
      }, [apiState, fetchJson]);
      return <p>state:{apiState}</p>;
    }

    render(
      <ProductionTree client={client}>
        <Probe />
      </ProductionTree>,
    );

    await waitFor(() => {
      expect(screen.getByText("state:denied")).toBeInTheDocument();
    });
    expect(client.getQueryData(protectedKey)).toBeUndefined();
    expect(client.getMutationCache().getAll()).toHaveLength(0);
  });

  it("clears cached queries on logout", async () => {
    const client = createFlexQueryClient();
    seedProtectedData(client, "secret-list");
    vi.stubGlobal("fetch", authenticatedSessionFetch({
      resource: (url) => {
        if (url.includes("/auth/logout")) {
          return jsonResponse(200, { logged_out: true, end_session_url: null });
        }

        return undefined;
      },
    }));
    vi.stubGlobal("location", { assign: vi.fn(), pathname: "/", search: "" });

    function LogoutProbe() {
      const { apiState, logout } = useProductionApi();
      useEffect(() => {
        if (apiState === "ready") {
          void logout();
        }
      }, [apiState, logout]);
      return <p>state:{apiState}</p>;
    }

    render(
      <ProductionTree client={client}>
        <LogoutProbe />
      </ProductionTree>,
    );

    await waitFor(() => {
      expect(client.getQueryData(protectedKey)).toBeUndefined();
    });
  });

  it("clears cached queries when bootstrap is unauthenticated", async () => {
    const client = createFlexQueryClient();
    seedProtectedData(client, "secret-list");
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return Promise.resolve(jsonResponse(200, { authenticated: false, csrf_token: "csrf-anon" }));
      }

      return Promise.resolve(jsonResponse(404, {}));
    }));

    render(
      <ProductionTree client={client}>
        <CacheProbe label="cache" />
      </ProductionTree>,
    );

    await waitFor(() => {
      expect(client.getQueryData(protectedKey)).toBeUndefined();
    });
  });

  it("does not let a stale generation response repopulate the cache after reset", async () => {
    const client = createFlexQueryClient();
    let releaseProtected: ((value: unknown) => void) | undefined;
    const protectedBody = new Promise((resolve) => {
      releaseProtected = resolve;
    });

    vi.stubGlobal("fetch", authenticatedSessionFetch({
      resource: (url) => {
        if (url.includes("/v1/assessment/activities")) {
          return protectedBody.then(() => jsonResponse(200, { activities: ["late"], permitted_actions: [] }));
        }

        if (url.includes("/v1/assessment/source-options")) {
          return jsonResponse(401, { error: "authn.expired" });
        }

        return undefined;
      },
    }));

    function Probe() {
      const { apiState, fetchJson } = useProductionApi();
      useEffect(() => {
        if (apiState !== "ready") {
          return;
        }

        void fetchJson("/v1/assessment/activities").then((data) => {
          client.setQueryData(protectedKey, data);
        }).catch(() => undefined);
        void fetchJson("/v1/assessment/source-options").catch(() => undefined);
      }, [apiState, fetchJson]);
      return <p>state:{apiState}</p>;
    }

    render(
      <ProductionTree client={client}>
        <Probe />
      </ProductionTree>,
    );

    await waitFor(() => {
      expect(screen.getByText("state:idle")).toBeInTheDocument();
    });
    releaseProtected?.({});
    await new Promise((resolve) => setTimeout(resolve, 30));
    expect(client.getQueryData(protectedKey)).toBeUndefined();
  });

  it("clears Query cache and protected local state on in-place actor/Organization replacement", async () => {
    const client = createFlexQueryClient();
    let organizationId = "org-1";
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return jsonResponse(200, { authenticated: true, csrf_token: "csrf-1" });
      }

      if (url.includes("/v1/assessment/shell")) {
        return jsonResponse(200, {
          schema_version: "v1",
          actor_id: "actor-1",
          organization_id: organizationId,
          relationship: "administrator",
          navigation: [{ destination_id: "activities", is_available: true }],
          permitted_actions: ["assessment.activity.create"],
        });
      }

      return jsonResponse(404, {});
    }));

    function LocalStateProbe() {
      const { shell } = useProductionApi();
      return (
        <div>
          <p>org:{shell?.organization_id ?? "none"}</p>
          <input aria-label="Campaign title" />
          <button type="button" onClick={() => {
            organizationId = "org-2";
            void reloadTrustedContextForTests();
          }}
          >
            Switch organization
          </button>
        </div>
      );
    }

    render(
      <FlexQueryProvider client={client}>
        <ProductionApiProvider>
          <ProtectedAuthSubtree>
            <LocalStateProbe />
          </ProtectedAuthSubtree>
        </ProductionApiProvider>
      </FlexQueryProvider>,
    );

    await waitFor(() => {
      expect(screen.getByText("org:org-1")).toBeInTheDocument();
    });
    seedProtectedData(client, "org-1-data");
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Actor A draft" } });
    expect(screen.getByLabelText("Campaign title")).toHaveValue("Actor A draft");
    fireEvent.click(screen.getByRole("button", { name: "Switch organization" }));

    await waitFor(() => {
      expect(screen.getByText("org:org-2")).toBeInTheDocument();
    });
    expect(client.getQueryData(protectedKey)).toBeUndefined();
    expect(screen.getByLabelText("Campaign title")).toHaveValue("");
  });

  it("purges Query cache and local state when authorization context is replaced for the same actor and Organization", async () => {
    const client = createFlexQueryClient();
    let relationship = "administrator";
    let permittedActions = ["assessment.activity.create"];
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return jsonResponse(200, { authenticated: true, csrf_token: "csrf-1" });
      }

      if (url.includes("/v1/assessment/shell")) {
        return jsonResponse(200, {
          schema_version: "v1",
          actor_id: "actor-1",
          organization_id: "org-1",
          relationship,
          navigation: [{ destination_id: "activities", is_available: true }],
          permitted_actions: permittedActions,
        });
      }

      return jsonResponse(404, {});
    }));

    function LocalStateProbe() {
      const { shell } = useProductionApi();
      return (
        <div>
          <p>relationship:{shell?.relationship ?? "none"}</p>
          <p>actions:{shell?.permitted_actions.join(",") || "none"}</p>
          <input aria-label="Campaign title" />
          <button type="button" onClick={() => {
            relationship = "reviewer";
            permittedActions = ["assessment.review.read"];
            void reloadTrustedContextForTests();
          }}
          >
            Reload trusted context
          </button>
        </div>
      );
    }

    render(
      <FlexQueryProvider client={client}>
        <ProductionApiProvider>
          <ProtectedAuthSubtree>
            <LocalStateProbe />
          </ProtectedAuthSubtree>
        </ProductionApiProvider>
      </FlexQueryProvider>,
    );

    await waitFor(() => {
      expect(screen.getByText("relationship:administrator")).toBeInTheDocument();
    });
    seedProtectedData(client, "administrator-list");
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Admin draft" } });
    expect(screen.getByLabelText("Campaign title")).toHaveValue("Admin draft");
    fireEvent.click(screen.getByRole("button", { name: "Reload trusted context" }));

    await waitFor(() => {
      expect(screen.getByText("relationship:reviewer")).toBeInTheDocument();
    });
    expect(screen.getByText("actions:assessment.review.read")).toBeInTheDocument();
    expect(client.getQueryData(protectedKey)).toBeUndefined();
    expect(screen.getByLabelText("Campaign title")).toHaveValue("");
  });
});

describe("synthetic actor replacement", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("clears cached queries when the synthetic actor identity changes", async () => {
    const client = createFlexQueryClient();
    let actorId = "actor.synthetic.admin";
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/browser/actor-context")) {
        return Promise.resolve(jsonResponse(200, {
          schema_version: "v1",
          actor_id: actorId,
          display_name: "Synthetic",
          organization_id: "org.synthetic.demo",
          organization_name: "Synthetic Demo Organization",
          capabilities: ["activity_admin"],
          actor_stage: "administrator",
          is_synthetic: true,
        }));
      }

      if (url.includes("/browser/navigation")) {
        return Promise.resolve(jsonResponse(200, {
          schema_version: "v1",
          destinations: [{ destination_id: "home", label: "Home", route: "/", tier: "p0", is_available: true }],
        }));
      }

      return Promise.resolve(jsonResponse(404, {}));
    }));

    function Probe() {
      const { apiState, refresh, actor } = useBrowserApi();
      useEffect(() => {
        if (apiState === "ready" && actor?.actor_id === "actor.synthetic.admin") {
          seedProtectedData(client, "actor-a-data");
          actorId = "actor.synthetic.other";
          void refresh();
        }
      }, [actor, apiState, refresh]);
      return <p>actor:{actor?.actor_id ?? "none"}</p>;
    }

    render(
      <FlexQueryProvider client={client}>
        <BrowserApiProvider>
          <Probe />
        </BrowserApiProvider>
      </FlexQueryProvider>,
    );

    await waitFor(() => {
      expect(screen.getByText("actor:actor.synthetic.other")).toBeInTheDocument();
    });
    expect(client.getQueryData(protectedKey)).toBeUndefined();
  });
});
