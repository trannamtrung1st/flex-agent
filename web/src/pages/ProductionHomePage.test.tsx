import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { FlexQueryProvider } from "../api/query-client";
import { ProductionHomePage } from "./ProductionHomePage";

describe("ProductionHomePage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("offers Activities when that destination is available", async () => {
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
            actor_id: "actor-1",
            organization_id: "org-1",
            relationship: "administrator",
            navigation: [
              { destination_id: "home", is_available: true },
              { destination_id: "activities", is_available: true },
              { destination_id: "my-work", is_available: false },
            ],
            permitted_actions: [],
          }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <FlexQueryProvider>
        <ProductionApiProvider>
          <MemoryRouter>
            <ProductionHomePage />
          </MemoryRouter>
        </ProductionApiProvider>
      </FlexQueryProvider>,
    );

    const activities = await screen.findByRole("link", { name: "Open Activities" });
    expect(activities).toHaveAttribute("href", "/activities");
    expect(activities).toHaveTextContent("Open");
    const activitiesPlate = screen.getByRole("article", { name: "Activities" });
    expect(activitiesPlate).toHaveClass("assignment-plate", "frame-cut");
    expect(activitiesPlate).not.toHaveAttribute("aria-live");
    expect(activitiesPlate).not.toHaveTextContent("Destination");
    expect(activities.closest("footer")).toHaveAttribute("data-arrangement", "end");
    const bay = activities.closest(".composition-grid");
    expect(bay).toBeInstanceOf(HTMLElement);
    expect(bay).toHaveAttribute("data-flow-fit", "fill");
    expect(bay).toHaveAttribute("data-flow-min", "control");
    expect(activities.closest(".destination-bays")).toBeNull();
    expect(screen.getByRole("region", { name: "Home" }).querySelector(".operate-scroll")).toContainElement(
      bay as HTMLElement,
    );
    expect(screen.queryByRole("article", { name: "My work" })).not.toBeInTheDocument();
  });

  it("omits destination plates that are not available", async () => {
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
            actor_id: "actor-1",
            organization_id: "org-1",
            relationship: "administrator",
            navigation: [
              { destination_id: "home", is_available: true },
              { destination_id: "activities", is_available: true },
              { destination_id: "my-work", is_available: false },
              { destination_id: "review", is_available: false },
            ],
            permitted_actions: [],
          }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <FlexQueryProvider>
        <ProductionApiProvider>
          <MemoryRouter>
            <ProductionHomePage />
          </MemoryRouter>
        </ProductionApiProvider>
      </FlexQueryProvider>,
    );

    expect(await screen.findByRole("article", { name: "Activities" })).toBeInTheDocument();
    expect(screen.queryByRole("article", { name: "My work" })).not.toBeInTheDocument();
    expect(screen.queryByRole("article", { name: "Review work" })).not.toBeInTheDocument();
  });

  it("sends My work actors to the assignment index instead of a second roster", async () => {
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
            actor_id: "actor-1",
            organization_id: "org-1",
            relationship: "participant",
            navigation: [
              { destination_id: "home", is_available: true },
              { destination_id: "my-work", is_available: true },
            ],
            permitted_actions: [],
          }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <FlexQueryProvider>
        <ProductionApiProvider>
          <MemoryRouter initialEntries={["/"]}>
            <Routes>
              <Route path="/" element={<ProductionHomePage />} />
              <Route path="/my-work" element={<p>My work index</p>} />
            </Routes>
          </MemoryRouter>
        </ProductionApiProvider>
      </FlexQueryProvider>,
    );

    expect(await screen.findByText("My work index")).toBeInTheDocument();
    expect(screen.queryByRole("region", { name: "Home" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Home" })).not.toBeInTheDocument();
  });
});
