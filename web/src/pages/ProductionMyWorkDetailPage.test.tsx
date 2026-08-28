import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { FlexQueryProvider } from "../api/query-client";
import { ProductionMyWorkDetailPage } from "./ProductionMyWorkDetailPage";

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  });
}

function assignmentPayload() {
  return {
    schema_version: "v1",
    assignment: {
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
  };
}

function submissionPayload(overrides: Record<string, unknown> = {}) {
  return {
    schema_version: "v2",
    enrollment_id: "enr-1",
    enrollment_status: "active",
    intake_available: true,
    requirements: {
      contract_version: "v2",
      max_attachment_count: 2,
      max_attachment_aggregate_bytes: 10_000,
      max_direct_text_bytes: 4_000,
      scanner_mode: "disabled_by_approved_policy",
      categories: [
        { category: "direct_text", available: true, max_bytes: 4_000 },
        { category: "text_plain_attachment", available: true, max_bytes: 4_000 },
        { category: "text_markdown_attachment", available: true, max_bytes: 4_000 },
      ],
    },
    active_intake: null,
    version_history: [],
    permitted_actions: ["begin_intake"],
    ...overrides,
  };
}

function stubAuthenticatedFetch(handler: (url: string, init?: RequestInit) => ReturnType<typeof jsonResponse>) {
  vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
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
    return handler(url, init);
  }));
}

function renderDetail() {
  return render(
    <FlexQueryProvider>
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/my-work/enr-1"]}>
          <Routes>
            <Route path="/my-work/:enrollmentId" element={<ProductionMyWorkDetailPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>
    </FlexQueryProvider>,
  );
}

describe("ProductionMyWorkDetailPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("loads assignment tracks and offers Begin intake when permitted", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/v1/assessment/my-work/enr-1") && !url.includes("submission") && !url.includes("timing")) {
        return jsonResponse(assignmentPayload());
      }
      if (url.includes("/timing")) {
        return jsonResponse({
          schema_version: "v2",
          assignment: assignmentPayload().assignment,
          participant_consequence_code: "none",
          effective: {
            submission_starts_at_utc: "2026-08-01T00:00:00Z",
            submission_exclusive_end_utc: "2026-09-01T12:00:00Z",
            attempt_start_utc: "2026-08-01T00:00:00Z",
            attempt_start_exclusive_end_utc: "2026-09-01T12:00:00Z",
            evaluated_at_utc: "2026-08-28T00:00:00Z",
            eligibility_state: "open",
            is_authoritative: true,
            time_zone_id: "UTC",
            participant_consequence_code: "none",
          },
        });
      }
      if (url.includes("/submission")) {
        return jsonResponse(submissionPayload());
      }
      return jsonResponse({}, 404);
    });

    renderDetail();

    expect(await screen.findByRole("heading", { name: "Campaign A" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Assignment" }).querySelector(".frame-cut")).toHaveClass(
      "destination-board",
      "assignment-station-board",
      "frame-cut--flush",
    );
    expect(screen.getByRole("link", { name: "My work" })).toHaveAttribute("href", "/my-work");
    expect(screen.getByRole("heading", { name: "Submission" })).toBeInTheDocument();
    const begin = screen.getByRole("button", { name: "Begin intake" });
    expect(begin.closest(".operate-head")).toBeTruthy();
    expect(begin.closest(".work-well__foot")).toBeNull();
    const submission = screen.getByRole("heading", { name: "Submission" });
    expect(begin.compareDocumentPosition(submission) & Node.DOCUMENT_POSITION_FOLLOWING).toBeGreaterThan(0);
    expect(screen.getByText(/Start Attempt is not available/)).toBeInTheDocument();
  });

  it("finalizes an intake only after confirmation", async () => {
    let submission = submissionPayload();
    stubAuthenticatedFetch((url, init) => {
      if (url.includes("/v1/assessment/my-work/enr-1") && !url.includes("submission") && !url.includes("timing")) {
        return jsonResponse(assignmentPayload());
      }
      if (url.includes("/timing")) {
        return jsonResponse({ schema_version: "v2", assignment: assignmentPayload().assignment, participant_consequence_code: "none" });
      }
      if (url.endsWith("/submission/intake") && init?.method === "POST") {
        submission = submissionPayload({
          permitted_actions: [],
          active_intake: {
            intake_id: "in-1",
            submission_id: "sub-1",
            status: "receiving",
            revision: 1,
            created_at_utc: "2026-08-28T00:00:00Z",
            updated_at_utc: "2026-08-28T00:00:00Z",
            items: [],
            permitted_actions: ["complete_item", "cancel_intake", "finalize_intake"],
          },
        });
        return jsonResponse({
          schema_version: "v2",
          succeeded: true,
          outcome_code: "ok",
          intake_id: "in-1",
          permitted_actions: ["complete_item", "cancel_intake", "finalize_intake"],
        });
      }
      if (url.includes("/finalize") && init?.method === "POST") {
        submission = submissionPayload({
          permitted_actions: [],
          active_intake: null,
          version_history: [{ version_id: "ver-1", version_number: 1, accepted_at_utc: "2026-08-28T01:00:00Z", item_count: 1 }],
        });
        return jsonResponse({
          schema_version: "v2",
          succeeded: true,
          outcome_code: "accepted",
          version_id: "ver-1",
          version_number: 1,
          permitted_actions: [],
        });
      }
      if (url.includes("/submission")) {
        return jsonResponse(submission);
      }
      return jsonResponse({}, 404);
    });

    renderDetail();
    fireEvent.click(await screen.findByRole("button", { name: "Begin intake" }));
    expect(await screen.findByRole("button", { name: "Submit version" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Submit version" }));
    const dialog = await screen.findByRole("dialog", { name: "Submit this version?" });
    fireEvent.click(within(dialog).getByRole("button", { name: "Submit version" }));
    await waitFor(() => {
      expect(screen.getByText(/Accepted version 1 remains immutable/)).toBeInTheDocument();
    });
  });

  it("offers a Shipboard Choose files key instead of a bare file control", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/v1/assessment/my-work/enr-1") && !url.includes("submission") && !url.includes("timing")) {
        return jsonResponse(assignmentPayload());
      }
      if (url.includes("/timing")) {
        return jsonResponse({ schema_version: "v2", assignment: assignmentPayload().assignment, participant_consequence_code: "none" });
      }
      if (url.includes("/submission")) {
        return jsonResponse(submissionPayload({
          permitted_actions: [],
          active_intake: {
            intake_id: "in-1",
            submission_id: "sub-1",
            status: "receiving",
            revision: 1,
            created_at_utc: "2026-08-28T00:00:00Z",
            updated_at_utc: "2026-08-28T00:00:00Z",
            items: [],
            permitted_actions: ["complete_item", "cancel_intake"],
          },
        }));
      }
      return jsonResponse({}, 404);
    });

    renderDetail();

    expect(await screen.findByRole("button", { name: "Choose files" })).toBeInTheDocument();
    const fileInput = document.querySelector('input[type="file"]');
    expect(fileInput).toHaveClass("visually-hidden");
    expect(fileInput).toHaveAttribute("accept", expect.stringContaining(".txt"));
  });
});
