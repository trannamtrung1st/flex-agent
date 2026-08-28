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

  it("presents each assignment as a readout plate with an Open assignment key", async () => {
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
    expect(plate).toHaveClass("assignment-plate");
    expect(plate).not.toHaveAttribute("aria-live");
    expect(plate.closest(".frame-cut")).toHaveClass("destination-board", "assignment-board", "frame-cut--flush");
    expect(screen.getByRole("link", { name: "Open assignment" })).toHaveAttribute("href", "/my-work/enr-1");
    expect(plate).toHaveTextContent("Case study");
    expect(plate).toHaveTextContent("active");
    expect(screen.getByRole("heading", { name: "Current assignments" })).toBeInTheDocument();
    expect(document.querySelector(".assignment-bays")).toHaveClass("assignment-bays--hug");
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

  it("uses a dense bay when more than one assignment is present", async () => {
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
    expect(document.querySelector(".assignment-bays")).toHaveClass("assignment-bays--dense");
    expect(document.querySelector(".assignment-bays")).not.toHaveClass("assignment-bays--hug");
  });

  it("centers an empty-board plate when there is no assigned work", async () => {
    stubSession((url) => {
      if (url.includes("/v1/assessment/my-work")) {
        return jsonResponse({ schema_version: "v1", items: [], has_more: false });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    expect(await screen.findByText("No current assignments")).toBeInTheDocument();
    expect(document.querySelector(".assignment-board-empty")).toBeTruthy();
    expect(screen.queryByRole("link", { name: "Open assignment" })).not.toBeInTheDocument();
  });
});
