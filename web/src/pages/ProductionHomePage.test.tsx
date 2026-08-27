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

    expect(await screen.findByRole("link", { name: "Open Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.getByText("My work is not available for the current authorized relationship.")).toBeInTheDocument();
  });
});
