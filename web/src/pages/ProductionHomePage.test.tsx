import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
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
    expect(activitiesPlate).toHaveClass("assignment-plate");
    expect(activitiesPlate).not.toHaveAttribute("aria-live");
    expect(activitiesPlate).not.toHaveTextContent("Destination");
    expect(activities.closest("footer")).toHaveAttribute("data-arrangement", "end");
    expect(activities.closest(".destination-bays")).toHaveClass("destination-bays", "plate-bays--hug");
    expect(activities.closest(".frame-cut")).toBeNull();
    expect(screen.getByRole("region", { name: "Home" }).querySelector(".frame-cut")).toBeNull();
    expect(screen.getByRole("region", { name: "Home" }).querySelector(":scope > .operate-scroll")).toContainElement(
      activities.closest(".destination-bays"),
    );
    expect(screen.getByRole("article", { name: "My work" })).toHaveTextContent(
      "My work is not available for the current authorized relationship.",
    );
    expect(screen.getByRole("article", { name: "My work" }).querySelector("footer")).toHaveClass("assignment-plate-keys--empty");
    expect(screen.getByRole("article", { name: "My work" }).querySelector("footer")).toHaveAttribute("data-arrangement", "end");
  });

  it("uses shared unavailable copy on destination plates", async () => {
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
              { destination_id: "activities", is_available: false },
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
          <MemoryRouter>
            <ProductionHomePage />
          </MemoryRouter>
        </ProductionApiProvider>
      </FlexQueryProvider>,
    );

    expect(await screen.findByRole("article", { name: "Activities" })).toHaveTextContent(
      "Activities are not available for the current authorized relationship.",
    );
  });
});
