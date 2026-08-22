import { useEffect, useState } from "react";
import { render, screen, waitFor } from "@testing-library/react";
import { App } from "../App";
import { ProductionApiProvider, useProductionApi } from "./production-api";

function ShellProbe() {
  const { apiState, shell, csrfToken } = useProductionApi();
  return (
    <div>
      <p>state:{apiState}</p>
      <p>csrf:{csrfToken ?? "none"}</p>
      <p>org:{shell?.organization_id ?? "none"}</p>
      <p>nav:{shell?.navigation.map((item) => item.destination_id).join(",") || "none"}</p>
    </div>
  );
}

describe("production application session", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("bootstraps CSRF in memory and consumes the versioned shell context", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({ authenticated: true, csrf_token: "csrf-1", mfa_present: true }),
        });
      }

      if (url.includes("/v1/assessment/shell")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            actor_id: "actor-1",
            organization_id: "org-1",
            relationship: "administrator",
            navigation: [{ destination_id: "activities", is_available: true }],
            permitted_actions: ["assessment.activity.create"],
          }),
        });
      }

      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <ProductionApiProvider>
        <ShellProbe />
      </ProductionApiProvider>,
    );

    await waitFor(() => {
      expect(screen.getByText("state:ready")).toBeInTheDocument();
    });
    expect(screen.getByText("csrf:csrf-1")).toBeInTheDocument();
    expect(screen.getByText("org:org-1")).toBeInTheDocument();
    expect(localStorage.getItem("csrf_token")).toBeNull();
    expect(sessionStorage.getItem("csrf_token")).toBeNull();
  });

  it("treats a 403 shell as access loss rather than synthetic navigation authority", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({ authenticated: true, csrf_token: "csrf-1", mfa_present: true }),
        });
      }

      return Promise.resolve({
        ok: false,
        status: 403,
        json: () => Promise.resolve({ error: "authn.insufficient_strength" }),
      });
    }));

    render(
      <ProductionApiProvider>
        <ShellProbe />
      </ProductionApiProvider>,
    );

    await waitFor(() => {
      expect(screen.getByText("state:denied")).toBeInTheDocument();
    });
    expect(screen.getByText("org:none")).toBeInTheDocument();
  });

  it("clears protected session state when a resource request is forbidden", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({ authenticated: true, csrf_token: "csrf-1" }),
        });
      }

      if (url.includes("/v1/assessment/shell")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            actor_id: "actor-1",
            organization_id: "org-1",
            relationship: "reviewer",
            navigation: [{ destination_id: "activities", is_available: true }],
            permitted_actions: ["assessment.activity.read"],
          }),
        });
      }

      const denied = {
        ok: false,
        status: 403,
        json: () => Promise.resolve({ error: "assessment.denied" }),
        clone() {
          return this;
        },
      };
      return Promise.resolve(denied);
    }));

    function ResourceProbe() {
      const { apiState, csrfToken, fetchJson } = useProductionApi();
      const [resource, setResource] = useState("pending");
      useEffect(() => {
        if (apiState !== "ready") {
          return;
        }

        void fetchJson("/v1/assessment/source-options").catch(() => {
          setResource("denied");
        });
      }, [apiState, fetchJson]);

      return (
        <div>
          <p>state:{apiState}</p>
          <p>csrf:{csrfToken ?? "none"}</p>
          <p>resource:{resource}</p>
        </div>
      );
    }

    render(
      <ProductionApiProvider>
        <ResourceProbe />
      </ProductionApiProvider>,
    );

    await waitFor(() => {
      expect(screen.getByText("resource:denied")).toBeInTheDocument();
    });
    expect(screen.getByText("state:denied")).toBeInTheDocument();
    expect(screen.getByText("csrf:none")).toBeInTheDocument();
  });

  it("keeps the default App on the synthetic provider when production mode is unset", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/browser/actor-context")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            actor_id: "actor.synthetic.admin",
            display_name: "Synthetic Administrator",
            organization_id: "org.synthetic.demo",
            organization_name: "Synthetic Demo Organization",
            capabilities: ["activity_admin"],
            actor_stage: "administrator",
            is_synthetic: true,
          }),
        });
      }

      if (url.includes("/browser/navigation")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            destinations: [
              { destination_id: "home", label: "Home", route: "/", tier: "p0", is_available: true },
              { destination_id: "activities", label: "Activities", route: "/activities", tier: "p0", is_available: true },
            ],
          }),
        });
      }

      if (url.includes("/browser/home")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            greeting: "Welcome, Synthetic Administrator",
            work_items: [],
            permitted_actions: [],
          }),
        });
      }

      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(<App />);

    expect(await screen.findByRole("heading", { name: /^home$/i })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Continue to sign in" })).not.toBeInTheDocument();
  });
});
