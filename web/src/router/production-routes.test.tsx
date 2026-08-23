import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { ProductionAppShell } from "../components/shell/ProductionAppShell";
import { ProductionDestinationGuard } from "./production-routes";

describe("production destination guards", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("keeps Home and My work available when Activities is denied", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve({ authenticated: true, csrf_token: "csrf" }) });
      }
      if (url.includes("/v1/assessment/shell")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            actor_id: "part",
            organization_id: "org",
            relationship: "",
            navigation: [
              { destination_id: "home", is_available: true },
              { destination_id: "activities", is_available: false },
              { destination_id: "my-work", is_available: true },
            ],
            permitted_actions: ["assessment.assignment.discover"],
          }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/activities"]}>
          <Routes>
            <Route element={<ProductionAppShell />}>
              <Route
                path="/activities"
                element={(
                  <ProductionDestinationGuard
                    destinationId="activities"
                    unavailableCopy="Activities are not available for the current authorized relationship."
                  >
                    <p>Activities workspace</p>
                  </ProductionDestinationGuard>
                )}
              />
              <Route path="/" element={<p>Home</p>} />
              <Route path="/my-work" element={<p>My work</p>} />
            </Route>
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>,
    );

    expect(await screen.findByText("Activities are not available for the current authorized relationship.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Return to Home" })).toBeInTheDocument();
    expect(screen.getAllByRole("link", { name: "My work" }).length).toBeGreaterThan(0);
    expect(screen.queryByText("Activities workspace")).not.toBeInTheDocument();
    expect(screen.getByText("Organization")).toBeInTheDocument();
    expect(screen.queryByText(/Organization org/)).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Sign out" })).toBeInTheDocument();
  });

  it("signs out through the authenticated logout command", async () => {
    const assign = vi.fn();
    vi.stubGlobal("location", {
      href: "http://localhost/",
      origin: "http://localhost",
      pathname: "/",
      search: "",
      assign,
    });
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve({ authenticated: true, csrf_token: "csrf" }) });
      }
      if (url.includes("/v1/assessment/shell")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            actor_id: "part",
            organization_id: "11111111-1111-4111-8111-111111111111",
            relationship: "",
            navigation: [{ destination_id: "home", is_available: true }],
            permitted_actions: [],
          }),
        });
      }
      if (url.includes("/auth/logout")) {
        expect(init?.method).toBe("POST");
        return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve({ logged_out: true }) });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <ProductionApiProvider>
        <MemoryRouter>
          <Routes>
            <Route element={<ProductionAppShell />}>
              <Route path="/" element={<p>Home</p>} />
            </Route>
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>,
    );

    fireEvent.click(await screen.findByRole("button", { name: "Sign out" }));
    await waitFor(() => {
      expect(assign).toHaveBeenCalledWith("/");
    });
    const fetchMock = vi.mocked(fetch);
    expect(fetchMock.mock.calls.some(([input, init]) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      return url.includes("/auth/logout") && init?.method === "POST";
    })).toBe(true);
  });

  it("does not treat a failed logout as completed", async () => {
    const assign = vi.fn();
    vi.stubGlobal("location", {
      href: "http://localhost/",
      origin: "http://localhost",
      pathname: "/",
      search: "",
      assign,
    });
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve({ authenticated: true, csrf_token: "csrf" }) });
      }
      if (url.includes("/v1/assessment/shell")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            actor_id: "part",
            organization_id: "11111111-1111-4111-8111-111111111111",
            relationship: "",
            navigation: [{ destination_id: "home", is_available: true }],
            permitted_actions: [],
          }),
        });
      }
      if (url.includes("/auth/logout")) {
        return Promise.resolve({ ok: false, status: 400, json: () => Promise.resolve({}) });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <ProductionApiProvider>
        <MemoryRouter>
          <Routes>
            <Route element={<ProductionAppShell />}>
              <Route path="/" element={<p>Home</p>} />
            </Route>
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>,
    );

    fireEvent.click(await screen.findByRole("button", { name: "Sign out" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Sign out could not be completed.");
    expect(assign).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "Sign out" })).toBeInTheDocument();
  });

  it("navigates to the provider end-session URL after a successful revoke", async () => {
    const assign = vi.fn();
    const endSession = "https://issuer.example/realms/flex/protocol/openid-connect/logout?client_id=flex-agent-api";
    vi.stubGlobal("location", {
      href: "http://localhost/",
      origin: "http://localhost",
      pathname: "/",
      search: "",
      assign,
    });
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve({ authenticated: true, csrf_token: "csrf" }) });
      }
      if (url.includes("/v1/assessment/shell")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            actor_id: "part",
            organization_id: "11111111-1111-4111-8111-111111111111",
            relationship: "",
            navigation: [{ destination_id: "home", is_available: true }],
            permitted_actions: [],
          }),
        });
      }
      if (url.includes("/auth/logout")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({ logged_out: true, end_session_url: endSession }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <ProductionApiProvider>
        <MemoryRouter>
          <Routes>
            <Route element={<ProductionAppShell />}>
              <Route path="/" element={<p>Home</p>} />
            </Route>
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>,
    );

    fireEvent.click(await screen.findByRole("button", { name: "Sign out" }));
    await waitFor(() => {
      expect(assign).toHaveBeenCalledWith(endSession);
    });
  });
});
