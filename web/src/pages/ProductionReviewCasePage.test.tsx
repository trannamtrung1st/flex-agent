import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { createFlexQueryClient, FlexQueryProvider } from "../api/query-client";
import { ProductionReviewCasePage } from "./ProductionReviewCasePage";
import { QUEUED_CRITERION_UNAVAILABLE, RUNNING_CRITERION_UNAVAILABLE } from "../features/review/presentation";
import { reviewKeys } from "../features/review/queryKeys";
import type { ReviewCaseReadV1, ReviewCriterionReadV1, ReviewEvidenceOpenV1 } from "../contracts/v1";

function jsonResponse(body: unknown, status = 200) {
  const payload = structuredClone(body);
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    clone() {
      return { json: () => Promise.resolve(structuredClone(payload)) };
    },
    json: () => Promise.resolve(structuredClone(payload)),
  });
}

const CASE_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1";

function runningCase(overrides: Partial<ReviewCaseReadV1> = {}): ReviewCaseReadV1 {
  return {
    schema_version: "v1",
    review_case_id: CASE_ID,
    evaluation_processing_state: "running",
    assignment_state: "assigned",
    integrity_state: "intact",
    internal_evaluation_notice: "Internal Evaluation · Not a released Result",
    processing_notice: "Evaluation is not yet available for inspection.",
    campaign_label: "Campaign A",
    task_label: "Case study",
    participant_label: "Participant One",
    criterion_summaries: [],
    updated_at: "2026-09-13T12:00:00.000Z",
    time_zone_id: "UTC",
    ...overrides,
  };
}

function completedCase(): ReviewCaseReadV1 {
  return {
    ...runningCase(),
    evaluation_id: "eval-1",
    evaluation_processing_state: "completed",
    processing_notice: null,
    criterion_summaries: [
      {
        criterion_id: "crit-1",
        display_label: "Clarity",
        evaluator_mode: "deterministic",
        evaluator_mode_label: "Rule-based",
        status: "satisfied",
      },
    ],
  };
}

function reviewRequiredCase(): ReviewCaseReadV1 {
  return {
    ...completedCase(),
    evaluation_processing_state: "review_required",
    criterion_summaries: [
      {
        criterion_id: "crit-1",
        display_label: "Clarity",
        evaluator_mode: "deterministic",
        evaluator_mode_label: "Rule-based",
        status: "conflict",
      },
    ],
  };
}

function criterion(): ReviewCriterionReadV1 {
  return {
    schema_version: "v1",
    review_case_id: CASE_ID,
    evaluation_id: "eval-1",
    criterion_id: "crit-1",
    criterion_version: "1",
    display_label: "Clarity",
    evaluator_mode: "deterministic",
    evaluator_mode_label: "Rule-based",
    status: "satisfied",
    score: 1,
    confidence: "high",
    uncertainty: [],
    rationale: "The accepted Submission states the required fact.",
    evidence_references: [
      {
        evidence_id: "ev-1",
        source_type: "submission",
        precision: "whole_item",
        verification_state: "verified",
      },
    ],
    internal_evaluation_notice: "Internal Evaluation · Not a released Result",
  };
}

function evidence(): ReviewEvidenceOpenV1 {
  return {
    schema_version: "v1",
    review_case_id: CASE_ID,
    evaluation_id: "eval-1",
    evidence_id: "ev-1",
    locator: {
      locator_schema: "evidence-locator.v1",
      source_type: "submission",
      source_ref: { source_id: "sub-1", source_version: "1" },
      ownership_ref: {
        organization_id: "org-1",
        activity_id: "act-1",
        participant_id: "part-1",
        attempt_id: "att-1",
        session_id: "sess-1",
        evaluation_id: "eval-1",
      },
      location: { location_type: "whole_item", item_id: "item-1" },
      precision: "whole_item",
      integrity: {
        source_digest: "a".repeat(64),
        adapter_version: "v1",
        verification_state: "verified",
      },
      created_by: { service_id: "evaluation", invocation_id: "inv-1" },
    },
    availability: "available",
    display_text: "<script>alert(1)</script> cited text",
    internal_evaluation_notice: "Internal Evaluation · Not a released Result",
  };
}

function stubAuthenticatedFetch(handler: (url: string) => ReturnType<typeof jsonResponse>) {
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

function renderCase(path: string, queryClient = createFlexQueryClient(), state?: { restoreEvidenceId?: string }) {
  return {
    queryClient,
    ...render(
      <FlexQueryProvider client={queryClient}>
        <ProductionApiProvider>
          <MemoryRouter initialEntries={[{ pathname: path, state }]}>
            <Routes>
              <Route path="/review/:reviewId" element={<ProductionReviewCasePage />} />
              <Route path="/review/:reviewId/criteria/:criterionId" element={<ProductionReviewCasePage />} />
              <Route path="/review/:reviewId/criteria/:criterionId/evidence/:evidenceId" element={<ProductionReviewCasePage />} />
            </Routes>
          </MemoryRouter>
        </ProductionApiProvider>
      </FlexQueryProvider>,
    ),
  };
}

describe("ProductionReviewCasePage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("keeps criterion navigation absent while Evaluation is running", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes(`/v1/review/cases/${CASE_ID}`) && !url.includes("criteria") && !url.includes("evidence")) {
        return jsonResponse(runningCase());
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}`);
    expect(await screen.findByText(RUNNING_CRITERION_UNAVAILABLE)).toBeVisible();
    expect(screen.queryByRole("navigation", { name: "Criteria" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Open Evidence" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /approve/i })).not.toBeInTheDocument();
  });

  it("keeps processing ownership of the well when Evidence is in the URL of a running case", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/evidence/ev-1")) {
        return jsonResponse(evidence());
      }
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return jsonResponse(runningCase());
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}/criteria/crit-1/evidence/ev-1`);
    expect(await screen.findByText(RUNNING_CRITERION_UNAVAILABLE)).toBeVisible();
    expect(screen.queryByText(/cited text/)).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Back to criterion" })).not.toBeInTheDocument();
    expect(screen.queryByRole("navigation", { name: "Criteria" })).not.toBeInTheDocument();
  });

  it("inspects a conflict Evaluation that requires review", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/criteria/crit-1") && !url.includes("evidence")) {
        return jsonResponse({ ...criterion(), status: "conflict" });
      }
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return jsonResponse(reviewRequiredCase());
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}/criteria/crit-1`);
    expect(await screen.findByRole("link", { name: "Open Evidence" })).toBeVisible();
    expect(screen.getByLabelText("Criterion judgment")).toHaveTextContent("Conflict");
    expect(screen.getByRole("navigation", { name: "Criteria" })).toHaveTextContent("Conflict");
    expect(screen.queryByText(/requires review before criterion judgments/i)).not.toBeInTheDocument();
  });

  it("inspects a completed criterion without executing Evidence markup", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/criteria/crit-1") && !url.includes("evidence")) {
        return jsonResponse(criterion());
      }
      if (url.includes("/evidence/ev-1")) {
        return jsonResponse(evidence());
      }
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return jsonResponse(completedCase());
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}/criteria/crit-1`);
    expect(await screen.findByRole("link", { name: "Open Evidence" })).toHaveAttribute(
      "href",
      `/review/${CASE_ID}/criteria/crit-1/evidence/ev-1`,
    );
    const judgment = screen.getByLabelText("Criterion judgment");
    expect(judgment).toHaveTextContent("Rule-based (deterministic)");
    expect(judgment).toHaveTextContent("Version");
    expect(judgment).toHaveTextContent("Satisfied");
    expect(judgment).toHaveTextContent("The accepted Submission states the required fact.");
    expect(judgment).toHaveTextContent("Open Evidence");
    expect(screen.queryByLabelText("Evidence references")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Clarity/ })).toHaveAttribute("aria-current", "page");
    expect(screen.getAllByText("Internal Evaluation · Not a released Result").length).toBeGreaterThan(0);
    expect(screen.queryByRole("button", { name: /human revision/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("combobox", { name: "Criterion" })).not.toBeInTheDocument();
  });

  it("opens Evidence in the subordinate route and offers Back to criterion", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/evidence/ev-1")) {
        return jsonResponse(evidence());
      }
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return jsonResponse(completedCase());
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}/criteria/crit-1/evidence/ev-1`);
    expect(await screen.findByText(/<script>alert\(1\)<\/script> cited text/)).toBeVisible();
    const provenance = screen.getByLabelText("Evidence provenance");
    expect(provenance).toHaveTextContent("Internal Evaluation · Not a released Result");
    expect(provenance).toHaveTextContent("Cited text");
    expect(screen.getByText("Whole item item-1")).toBeVisible();
    expect(screen.getByText("Verified")).toBeVisible();
    expect(document.querySelector("script")).toBeNull();
    const backLinks = screen.getAllByRole("link", { name: "Back to criterion" });
    expect(backLinks).toHaveLength(2);
    expect(backLinks[0]).toHaveAttribute("href", `/review/${CASE_ID}/criteria/crit-1`);
    expect(backLinks[1]).toHaveAttribute("href", `/review/${CASE_ID}/criteria/crit-1`);
    expect(document.querySelector(".work-well__head")).toHaveTextContent("Back to criterion");
  });

  it("restores focus to Open Evidence when returning from Evidence", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/criteria/crit-1") && !url.includes("evidence")) {
        return jsonResponse(criterion());
      }
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return jsonResponse(completedCase());
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}/criteria/crit-1`, createFlexQueryClient(), { restoreEvidenceId: "ev-1" });
    const openEvidence = await screen.findByRole("link", { name: "Open Evidence" });
    await waitFor(() => {
      expect(openEvidence).toHaveFocus();
    });
  });

  it.each([
    ["awaiting", "This Session is awaiting an eligible Evaluation. Criterion judgments are not available.", null],
    ["queued", QUEUED_CRITERION_UNAVAILABLE, null],
    ["retryable_failure", "Evaluation failed and can be retried. Criterion judgments are not available.", null],
  ] as const)("shows the processing well for %s Evaluation", async (state, copy, notice) => {
    stubAuthenticatedFetch((url) => {
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return jsonResponse(runningCase({
          evaluation_processing_state: state,
          processing_notice: notice,
        }));
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}`);
    const message = await screen.findByText(copy);
    expect(message).toHaveAttribute("aria-live", "polite");
    expect(screen.queryByRole("navigation", { name: "Criteria" })).not.toBeInTheDocument();
  });

  it("shows loading while the Review case is fetched", () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return new Promise(() => {});
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}`);
    expect(screen.getByText("Loading Review case…")).toBeVisible();
  });

  it("offers Retry when the Review case request fails", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return Promise.resolve({
          ok: false,
          status: 500,
          json: () => Promise.resolve({ error: "server.error" }),
          clone() {
            return { json: () => Promise.resolve({ error: "server.error" }) };
          },
        });
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}`);
    expect(await screen.findByText("Review case unavailable")).toBeVisible();
    expect(screen.getByRole("button", { name: "Retry" })).toBeVisible();
  });

  it("shows criterion load error after the case resolves", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/criteria/crit-1") && !url.includes("evidence")) {
        return Promise.resolve({
          ok: false,
          status: 500,
          json: () => Promise.resolve({ error: "server.error" }),
          clone() {
            return { json: () => Promise.resolve({ error: "server.error" }) };
          },
        });
      }
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return jsonResponse(completedCase());
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}/criteria/crit-1`);
    expect(await screen.findByText("Criterion could not be loaded")).toBeVisible();
  });

  it("shows empty Evidence copy on a criterion without references", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/criteria/crit-2") && !url.includes("evidence")) {
        return jsonResponse({ ...criterion(), criterion_id: "crit-2", display_label: "Coverage", evidence_references: [] });
      }
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return jsonResponse({
          ...completedCase(),
          criterion_summaries: [
            ...completedCase().criterion_summaries,
            {
              criterion_id: "crit-2",
              display_label: "Coverage",
              evaluator_mode: "deterministic",
              evaluator_mode_label: "Rule-based",
              status: "insufficient_evidence",
            },
          ],
        });
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}/criteria/crit-2`);
    expect(await screen.findByText("No Evidence references on this criterion.")).toBeVisible();
  });

  it.each([
    ["insufficient_evidence", "Insufficient evidence"],
    ["not_applicable", "Not applicable"],
    ["unavailable", "Unavailable"],
  ] as const)("renders criterion status %s", async (status, label) => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/criteria/crit-1") && !url.includes("evidence")) {
        return jsonResponse({ ...criterion(), status });
      }
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return jsonResponse(completedCase());
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}/criteria/crit-1`);
    expect(await screen.findByLabelText("Criterion judgment")).toHaveTextContent(label);
  });

  it.each([
    ["denied", "Source unavailable"],
    ["unavailable", "Source unavailable"],
    ["integrity_changed", "Integrity warning"],
    ["lower_precision", "Lower precision"],
  ] as const)("opens Evidence with availability %s", async (availability, label) => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/evidence/ev-1")) {
        return jsonResponse({
          ...evidence(),
          availability,
          unavailability_notice: availability === "denied" ? "Access denied by policy." : undefined,
          display_text: undefined,
        });
      }
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return jsonResponse(completedCase());
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}/criteria/crit-1/evidence/ev-1`);
    expect(await screen.findByLabelText("Evidence provenance")).toHaveTextContent(label);
    if (availability === "denied") {
      expect(screen.getByText("Access denied by policy.")).toBeVisible();
    }
  });

  it("opens locator-only Evidence without cited text", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/evidence/ev-1")) {
        return jsonResponse({ ...evidence(), display_text: undefined });
      }
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return jsonResponse(completedCase());
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}/criteria/crit-1/evidence/ev-1`);
    const provenance = await screen.findByLabelText("Evidence provenance");
    expect(provenance).toHaveTextContent("Whole item item-1");
    expect(screen.queryByText("Cited text")).not.toBeInTheDocument();
  });

  it("shows Evidence open errors", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/evidence/ev-1")) {
        return Promise.resolve({
          ok: false,
          status: 404,
          json: () => Promise.resolve({ error: "review.evidence_unavailable" }),
          clone() {
            return { json: () => Promise.resolve({ error: "review.evidence_unavailable" }) };
          },
        });
      }
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return jsonResponse(completedCase());
      }
      return jsonResponse({}, 404);
    });
    renderCase(`/review/${CASE_ID}/criteria/crit-1/evidence/ev-1`);
    expect(await screen.findByText("Evidence could not be opened")).toBeVisible();
  });

  it("removes protected content when assignment is lost", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes(`/v1/review/cases/${CASE_ID}`)) {
        return Promise.resolve({
          ok: false,
          status: 404,
          json: () => Promise.resolve({ error: "review.denied" }),
          clone() {
            return { json: () => Promise.resolve({ error: "review.denied" }) };
          },
        });
      }
      return jsonResponse({}, 404);
    });
    const queryClient = createFlexQueryClient();
    const scope = { actorId: "actor-1", organizationId: "org-1" };
    queryClient.setQueryData(
      reviewKeys.criterion(scope, CASE_ID, "eval-1", "crit-1"),
      criterion(),
    );
    queryClient.setQueryData(reviewKeys.work(scope), {
      items: [{ review_case_id: CASE_ID, task_label: "Case study" }],
    });
    renderCase(`/review/${CASE_ID}`, queryClient);
    expect(await screen.findByText(/no longer assigned to you/i)).toBeVisible();
    expect(screen.queryByText("The accepted Submission states the required fact.")).not.toBeInTheDocument();
    await waitFor(() => {
      expect(queryClient.getQueryData(reviewKeys.criterion(scope, CASE_ID, "eval-1", "crit-1"))).toBeUndefined();
      expect(queryClient.getQueryData(reviewKeys.work(scope))).toBeUndefined();
    });
  });
});
