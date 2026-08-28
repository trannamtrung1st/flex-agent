import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { App } from "./App";

function json(status: number, body: unknown) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  });
}

describe("App production shell", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    window.history.pushState({}, "", "/");
  });

  it("asks the operator to sign in when the application session is idle", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return json(200, { authenticated: false });
      }
      return json(404, {});
    }));

    render(<App />);
    expect(await screen.findByRole("heading", { name: "Sign in required" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Continue to sign in" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Sign in required" })).toHaveClass("work-plane--ceremony");
  });

  it("renders Home and capability navigation when the trusted context is ready", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return json(200, { authenticated: true, csrf_token: "csrf" });
      }
      if (url.includes("/v1/assessment/shell")) {
        return json(200, {
          schema_version: "v1",
          actor_id: "actor-1",
          organization_id: "org-1",
          relationship: "administrator",
          navigation: [
            { destination_id: "home", is_available: true },
            { destination_id: "activities", is_available: true },
          ],
          permitted_actions: [],
        });
      }
      return json(404, {});
    }));

    render(<App />);
    expect(await screen.findByRole("heading", { name: /^home$/i })).toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: /primary navigation/i })).toBeInTheDocument();
    expect(screen.getAllByText("Workspace").length).toBeGreaterThan(0);
    expect(screen.getByRole("article", { name: "Activities" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Open Activities" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Sign out" })).toBeInTheDocument();
    expect(screen.getByText("Organization")).toBeInTheDocument();
    expect(screen.queryByText(/Organization org/)).not.toBeInTheDocument();
  });

  it("does not render protected destinations when workspace access is denied", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return json(200, { authenticated: true, csrf_token: "csrf" });
      }
      if (url.includes("/v1/assessment/shell")) {
        return json(403, { error: "authz.denied" });
      }
      return json(404, {});
    }));

    render(<App />);
    expect(await screen.findByRole("heading", { name: "Your access changed" })).toBeInTheDocument();
    expect(screen.getByText("This destination is not available for the current authorized relationship.")).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "This destination is not available for the current authorized relationship." })).not.toBeInTheDocument();
    expect(screen.queryByRole("navigation", { name: /primary navigation/i })).not.toBeInTheDocument();
    await waitFor(() => {
    expect(screen.queryByRole("heading", { name: /^home$/i })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Continue to sign in" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Your access changed" })).toHaveClass("work-plane--ceremony");
  });
  });

  it("keeps an authorized Results destination on an honest unavailable page", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return json(200, { authenticated: true, csrf_token: "csrf" });
      }
      if (url.includes("/v1/assessment/shell")) {
        return json(200, {
          schema_version: "v1",
          actor_id: "rev-1",
          organization_id: "org-1",
          relationship: "reviewer",
          navigation: [
            { destination_id: "home", is_available: true },
            { destination_id: "review", is_available: true },
            { destination_id: "results", is_available: true },
          ],
          permitted_actions: [],
        });
      }
      return json(404, {});
    }));

    render(<App />);
    fireEvent.click((await screen.findAllByRole("link", { name: "Results" }))[0]);
    expect(await screen.findByRole("region", { name: "Results" })).toHaveClass("work-plane--ceremony");
    expect(screen.getByRole("heading", { name: "Results" })).toBeInTheDocument();
    expect(screen.queryByRole("region", { name: "Home" })).not.toBeInTheDocument();
    expect(screen.getByText(/no Result list contract yet/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /start|open session|begin attempt/i })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Return to Home" })).toHaveAttribute("href", "/");
  });

  it("keeps an unknown locator on a non-disclosing ceremony instead of substituting Home", async () => {
    window.history.pushState({}, "", "/not-a-destination");
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return json(200, { authenticated: true, csrf_token: "csrf" });
      }
      if (url.includes("/v1/assessment/shell")) {
        return json(200, {
          schema_version: "v1",
          actor_id: "actor-1",
          organization_id: "org-1",
          relationship: "administrator",
          navigation: [
            { destination_id: "home", is_available: true },
            { destination_id: "activities", is_available: true },
          ],
          permitted_actions: [],
        });
      }
      return json(404, {});
    }));

    render(<App />);
    expect(await screen.findByRole("heading", { name: "This destination is not available" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "This destination is not available" })).toHaveClass("work-plane--ceremony");
    expect(screen.queryByText("not-a-destination")).not.toBeInTheDocument();
    expect(screen.queryByRole("navigation", { name: "Breadcrumb" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: /^home$/i })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Return to Home" })).toHaveAttribute("href", "/");
  });
});
