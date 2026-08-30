import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { FlexQueryProvider } from "../api/query-client";
import { ProductionMyWorkPage } from "./ProductionMyWorkPage";

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  });
}

function stubSession(handler: (url: string) => ReturnType<typeof jsonResponse>) {
  vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
    const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
    if (url.includes("/auth/session")) {
      return jsonResponse({ authenticated: true, csrf_token: "csrf" });
    }
    if (url.includes("/v1/assessment/shell")) {
      return jsonResponse({
        schema_version: "v1",
        actor_id: "actor-1",
        organization_id: "org-1",
        relationship: "participant",
        navigation: [{ destination_id: "my-work", is_available: true }],
        permitted_actions: [],
      });
    }
    return handler(url);
  }));
}

function renderPage() {
  return render(
    <FlexQueryProvider>
      <ProductionApiProvider>
        <MemoryRouter>
          <ProductionMyWorkPage />
        </MemoryRouter>
      </ProductionApiProvider>
    </FlexQueryProvider>,
  );
}

describe("ProductionMyWorkPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("presents each assignment as a readout plate with an Open key", async () => {
    stubSession((url) => {
      if (url.includes("/v1/assessment/my-work")) {
        return jsonResponse({
          schema_version: "v1",
          items: [
            {
              enrollment_id: "enr-1",
              status: "active",
              visibility: "participant",
              activity_title: "Campaign A",
              task_title: "Case study",
              time_zone_id: "UTC",
              deadline_utc: "2026-09-01T12:00:00Z",
              summary_available: true,
              permitted_actions: [],
            },
          ],
          has_more: false,
        });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    const plate = await screen.findByRole("article", { name: "Campaign A" });
    expect(plate).toHaveClass("assignment-plate", "frame-cut");
    expect(plate).not.toHaveAttribute("aria-live");
    expect(plate.querySelector(".frame-tick")).toBeNull();
    expect(screen.getByRole("region", { name: "My work" }).querySelector(".frame-cut")).toBe(plate);
    expect(screen.getByRole("link", { name: "Open Campaign A" })).toHaveAttribute("href", "/my-work/enr-1");
    expect(screen.getByRole("link", { name: "Open Campaign A" }).closest("footer")).toHaveAttribute("data-arrangement", "end");
    expect(plate).toHaveTextContent("Case study");
    expect(plate).toHaveTextContent("Active");
    expect(screen.getByRole("heading", { name: "Current assignments" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "My work" })).not.toHaveClass("assignment-board--hug");
    expect(screen.getByRole("region", { name: "My work" }).querySelector(":scope > .operate-scroll")).toContainElement(
      document.querySelector(".assignment-bays"),
    );
    expect(document.querySelector(".assignment-bays")).toHaveClass("assignment-bays");
    expect(document.querySelector(".assignment-bays")).not.toHaveClass("assignment-bays--dense");
    const plates = plate.closest(".composition-grid");
    expect(plates).toHaveAttribute("data-flow-fit", "fill");
    expect(plates).toHaveAttribute("data-flow-min", "control");
    expect(document.querySelector(".assignment-bay-plates")).toBeNull();
  });

  it("shows a readable UTC deadline when the campaign zone cannot be converted", async () => {
    stubSession((url) => {
      if (url.includes("/v1/assessment/my-work")) {
        return jsonResponse({
          schema_version: "v1",
          items: [
            {
              enrollment_id: "enr-1",
              status: "active",
              visibility: "participant",
              activity_title: "Campaign A",
              task_title: "Case study",
              time_zone_id: "Not/AZone",
              deadline_utc: "2026-09-30T17:00:00Z",
              summary_available: true,
              permitted_actions: [],
            },
          ],
          has_more: false,
        });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    const plate = await screen.findByRole("article", { name: "Campaign A" });
    expect(plate).toHaveTextContent(/conversion unavailable/i);
    expect(plate).not.toHaveTextContent("2026-09-30T17:00:00Z");
  });

  it("keeps hug plate geometry when more than one assignment is present", async () => {
    stubSession((url) => {
      if (url.includes("/v1/assessment/my-work")) {
        return jsonResponse({
          schema_version: "v1",
          items: [
            {
              enrollment_id: "enr-1",
              status: "active",
              visibility: "participant",
              activity_title: "Campaign A",
              task_title: "Case study",
              time_zone_id: "UTC",
              summary_available: true,
              permitted_actions: [],
            },
            {
              enrollment_id: "enr-2",
              status: "active",
              visibility: "participant",
              activity_title: "Campaign B",
              task_title: "Essay",
              time_zone_id: "UTC",
              summary_available: true,
              permitted_actions: [],
            },
          ],
          has_more: false,
        });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    await screen.findByRole("article", { name: "Campaign A" });
    expect(screen.getByRole("article", { name: "Campaign B" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "My work" })).not.toHaveClass("assignment-board--hug");
    expect(document.querySelector(".assignment-bays")).toHaveClass("assignment-bays");
    expect(document.querySelector(".assignment-bays")).not.toHaveClass("assignment-bays--dense");
    expect(document.querySelectorAll(".assignment-bays .composition-grid[data-flow-fit='fill']")).toHaveLength(1);
  });

  it("seats an inset empty plate in the operate well when there is no assigned work", async () => {
    stubSession((url) => {
      if (url.includes("/v1/assessment/my-work")) {
        return jsonResponse({ schema_version: "v1", items: [], has_more: false });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    expect(await screen.findByText("No current assignments")).toBeInTheDocument();
    const region = screen.getByRole("region", { name: "My work" });
    const empty = document.querySelector(".empty-plate");
    expect(empty).toHaveClass("empty-plate--inset");
    expect(empty?.closest(".frame-cut")).toBe(region.querySelector(".frame-cut"));
    expect(empty?.closest(".frame-cut")).not.toHaveClass("frame-cut--flush");
    expect(region).toHaveClass("assignment-board--hug");
    expect(document.querySelector(".assignment-board-empty")).toBeNull();
    expect(screen.queryByRole("link", { name: /Open / })).not.toBeInTheDocument();
  });
});
