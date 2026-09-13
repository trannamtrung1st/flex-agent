import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { FlexQueryProvider } from "../api/query-client";
import { ProductionReviewWorkPage } from "./ProductionReviewWorkPage";
import type { ReviewWorkItemV1 } from "../contracts/v1";

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
    clone() {
      return { json: () => Promise.resolve(body) };
    },
  });
}

function workItem(overrides: Partial<ReviewWorkItemV1> = {}): ReviewWorkItemV1 {
  return {
    schema_version: "v1",
    review_case_id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1",
    evaluation_processing_state: "completed",
    assignment_state: "assigned",
    integrity_state: "intact",
    campaign_label: "Campaign A",
    task_label: "Case study",
    participant_label: "Participant One",
    internal_evaluation_notice: "Internal Evaluation · Not a released Result",
    updated_at: "2026-09-13T12:00:00.000Z",
    time_zone_id: "UTC",
    next_action: "open_review",
    ...overrides,
  };
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
        relationship: "reviewer",
        navigation: [{ destination_id: "review", is_available: true }],
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
          <ProductionReviewWorkPage />
        </MemoryRouter>
      </ProductionApiProvider>
    </FlexQueryProvider>,
  );
}

describe("ProductionReviewWorkPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("shows loading Review work", () => {
    stubSession(() => new Promise(() => {}));
    renderPage();
    expect(screen.getByText("Loading Review work…")).toBeVisible();
  });

  it("shows the assigned empty state", async () => {
    stubSession((url) => {
      if (url.includes("/v1/review/work")) {
        return jsonResponse({ schema_version: "v1", items: [], has_more: false });
      }
      return jsonResponse({}, 404);
    });
    renderPage();
    expect(await screen.findByText("No Review work is assigned to you")).toBeVisible();
    expect(screen.queryByRole("searchbox")).not.toBeInTheDocument();
  });

  it("lists assigned cases without decision controls", async () => {
    stubSession((url) => {
      if (url.includes("/v1/review/work")) {
        return jsonResponse({ schema_version: "v1", items: [workItem()], has_more: false });
      }
      return jsonResponse({}, 404);
    });
    renderPage();
    expect(await screen.findByRole("link", { name: "Case study · Open review" })).toHaveAttribute(
      "href",
      "/review/aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1",
    );
    expect(screen.getByText("Evaluation completed")).toBeVisible();
    expect(screen.getByText("Internal Evaluation · Not a released Result")).toBeVisible();
    expect(screen.queryByRole("button", { name: /approve/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /release/i })).not.toBeInTheDocument();
  });
});
