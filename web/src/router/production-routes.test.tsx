import { render, screen } from "@testing-library/react";
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
  });
});
