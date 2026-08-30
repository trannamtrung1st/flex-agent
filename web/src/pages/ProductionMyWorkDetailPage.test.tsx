import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { FlexQueryProvider } from "../api/query-client";
import { ToastHost } from "../design-system";
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
    <ToastHost>
      <FlexQueryProvider>
        <ProductionApiProvider>
          <MemoryRouter initialEntries={["/my-work/enr-1"]}>
            <Routes>
              <Route path="/my-work/:enrollmentId" element={<ProductionMyWorkDetailPage />} />
            </Routes>
          </MemoryRouter>
        </ProductionApiProvider>
      </FlexQueryProvider>
    </ToastHost>,
  );
}

describe("ProductionMyWorkDetailPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("seats assignment loading as an inset wait-plate in the work well", () => {
    stubAuthenticatedFetch(() => new Promise(() => {}));
    renderDetail();
    const status = screen.getByRole("status");
    expect(status).toHaveClass("wait-plate", "wait-plate--inset");
    expect(status).not.toHaveClass("ceremony-wait");
    expect(screen.getByText("Loading assignment…")).toBeVisible();
    expect(status.closest(".work-well")).toBeTruthy();
    expect(document.querySelector(".operate-column--hug")).toBeNull();
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

    expect(await screen.findByRole("heading", { name: "Case study" })).toBeInTheDocument();
    expect(document.querySelector(".assignment-meta")).toHaveTextContent("Campaign A");
    fireEvent.mouseEnter(screen.getByText("enr…1").closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toHaveTextContent("enr-1");
    expect(screen.getByLabelText("Assignment status")).toHaveTextContent(/Begin intake/);
    expect(document.querySelector('[data-layout="guided-task"]')).toBeTruthy();
    expect(screen.queryByRole("navigation", { name: "Primary navigation" })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "My work" })).toHaveAttribute("href", "/my-work");
    expect(screen.getByRole("heading", { name: "Submission" })).toBeInTheDocument();
    expect(screen.getByText(/Direct text up to 4,000 bytes/)).toBeInTheDocument();
    expect(screen.getByText(/Eligibility: Open/)).toBeInTheDocument();
    expect(screen.getByLabelText("Assignment status")).toHaveTextContent("Active");
    expect(screen.getByRole("navigation", { name: "Assignment phases" })).toBeInTheDocument();
    const begin = screen.getByRole("button", { name: "Begin intake" });
    const beginFoot = begin.closest(".layout-guided__actions");
    expect(beginFoot).toBeTruthy();
    expect(beginFoot).toHaveAttribute("data-arrangement", "end");
    expect(begin.closest(".operate-head")).toBeNull();
    const submission = screen.getByRole("heading", { name: "Submission" });
    expect(submission.compareDocumentPosition(begin) & Node.DOCUMENT_POSITION_FOLLOWING).toBeGreaterThan(0);
    expect(screen.queryByText(/Start Attempt is not available/)).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /Attempt/ }));
    expect(screen.getByRole("heading", { name: "Attempt" })).toBeInTheDocument();
    expect(screen.getByLabelText("Assignment status")).toHaveTextContent(/Not available here/);
    expect(screen.getByText(/No production Attempt-start HTTP contract/)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Begin intake" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Start Attempt/i })).not.toBeInTheDocument();
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
      if (url.includes("/intake/") && url.includes("/items") && init?.method === "POST") {
        submission = submissionPayload({
          permitted_actions: [],
          active_intake: {
            intake_id: "in-1",
            submission_id: "sub-1",
            status: "receiving",
            revision: 2,
            created_at_utc: "2026-08-28T00:00:00Z",
            updated_at_utc: "2026-08-28T00:00:00Z",
            items: [{ item_id: "it-1", category: "direct_text", byte_count: 20 }],
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
    expect(await screen.findByText("Intake is open.")).toBeInTheDocument();
    expect(screen.getByText("Intake is open.").closest(".toast")).toHaveAttribute("role", "status");
    expect(await screen.findByLabelText("Direct text")).toHaveAttribute(
      "placeholder",
      "Write or paste the submission text",
    );
    expect(screen.getByLabelText("Assignment status")).toHaveTextContent("Intake receiving");
    const submit = await screen.findByRole("button", { name: "Submit version" });
    expect(submit).toBeDisabled();
    fireEvent.change(screen.getByLabelText("Direct text"), { target: { value: "Evidence paragraph." } });
    fireEvent.click(screen.getByRole("button", { name: "Add direct text" }));
    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Submit version" })).toBeEnabled();
    });
    expect(screen.getByLabelText("Assignment status")).toHaveTextContent("Submit version");
    const enabledSubmit = screen.getByRole("button", { name: "Submit version" });
    const cancel = screen.getByRole("button", { name: "Cancel intake" });
    const splitFoot = enabledSubmit.closest(".layout-guided__actions");
    expect(splitFoot).toHaveAttribute("data-arrangement", "split");
    expect(cancel.compareDocumentPosition(enabledSubmit) & Node.DOCUMENT_POSITION_FOLLOWING).toBeGreaterThan(0);
    fireEvent.click(enabledSubmit);
    const dialog = await screen.findByRole("dialog", { name: "Submit this version?" });
    fireEvent.click(within(dialog).getByRole("button", { name: "Submit version" }));
    await waitFor(() => {
      expect(screen.getByText(/Accepted version 1 remains immutable/)).toBeInTheDocument();
      expect(screen.getByText(/1 item/)).toBeInTheDocument();
    });
    expect(screen.getByText("This version is preserved. Earlier versions remain on record.")).toBeInTheDocument();
    const versions = screen.getByRole("list", { name: "Accepted submission versions" });
    expect(versions.tagName).toBe("OL");
    const versionItem = versions.querySelector(":scope > li");
    expect(versionItem).toHaveAttribute("data-sequence", "1");
    expect(versionItem).toHaveAttribute("value", "1");
    expect(versionItem?.querySelector(":scope > .composition-stack")).not.toBeNull();
    expect(versionItem?.querySelector("time")).toHaveAttribute("datetime", "2026-08-28T01:00:00.000Z");
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
            items: [{
              item_id: "it-1",
              category: "direct_text",
              byte_count: 48,
            }],
            permitted_actions: ["complete_item", "cancel_intake"],
          },
        }));
      }
      return jsonResponse({}, 404);
    });

    renderDetail();

    expect(await screen.findByRole("button", { name: "Choose files" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Attachments (.txt or .md)" })).not.toBeInTheDocument();
    expect(screen.getByText(/1 item received locally until the server accepts a version/)).toBeInTheDocument();
    expect(screen.getByRole("list", { name: "Received intake items" })).toHaveTextContent(/Direct text/);
    expect(screen.getByRole("list", { name: "Received intake items" })).toHaveTextContent(/48 bytes/);
    expect(screen.queryByRole("textbox", { name: "UTF-8 .txt or .md" })).not.toBeInTheDocument();
    const fileInput = document.querySelector('input[type="file"]');
    expect(fileInput).toHaveAttribute("accept", expect.stringContaining(".txt"));
    expect(fileInput).toHaveAttribute("multiple");
    expect(fileInput?.closest("[aria-hidden='true']")).toBeTruthy();
    expect(document.querySelector(".field-file")).toBeTruthy();
    expect(document.querySelector(".field-file-well")).toBeTruthy();
    expect(screen.getByText("Drop files onto this bay")).toBeInTheDocument();
    expect(screen.getByRole("group", { name: "Attachments (.txt or .md)" })).toBeInTheDocument();
    const blockedSubmit = screen.getByRole("button", { name: "Submit version" });
    expect(blockedSubmit).toBeDisabled();
    expect(blockedSubmit).toHaveAccessibleDescription(/not permitted for this intake/);
  });
});
