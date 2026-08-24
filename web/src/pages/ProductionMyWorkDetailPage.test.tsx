import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { ProductionMyWorkDetailPage } from "./ProductionMyWorkDetailPage";

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  });
}

describe("ProductionMyWorkDetailPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("keeps local preparation separate from accepted history and confirms submit version", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return jsonResponse({ authenticated: true, csrf_token: "csrf" });
      }
      if (url.includes("/v1/assessment/shell")) {
        return jsonResponse({
          schema_version: "v1",
          actor_id: "part",
          organization_id: "org",
          relationship: "",
          navigation: [{ destination_id: "my-work", is_available: true }],
          permitted_actions: ["assessment.assignment.discover"],
        });
      }
      if (url.includes("/v2/assessment/my-work/") && url.includes("/timing")) {
        return jsonResponse({
          schema_version: "v2",
          assignment: {
            enrollment_id: "enr-1",
            status: "active",
            visibility: "current",
            activity_title: "Campaign",
            task_title: "Task 1",
            time_zone_id: "UTC",
            deadline_utc: "2026-09-30T17:00:00Z",
            summary_available: true,
            permitted_actions: ["open_assignment"],
          },
          participant_consequence_code: "none",
        });
      }
      if (url.includes("/submission/intake") && init?.method === "POST" && !url.includes("/items") && !url.includes("/finalize")) {
        return jsonResponse({
          schema_version: "v2",
          succeeded: true,
          outcome_code: "receiving",
          intake_id: "11111111-1111-4111-8111-111111111111",
          submission_id: "22222222-2222-4222-8222-222222222222",
          status: "receiving",
          revision: 1,
          permitted_actions: ["complete_item", "cancel_intake", "return_to_my_work"],
        });
      }
      if (url.includes("/items") && init?.method === "POST") {
        return jsonResponse({
          schema_version: "v2",
          succeeded: true,
          outcome_code: "received",
          intake_id: "11111111-1111-4111-8111-111111111111",
          status: "received",
          revision: 2,
          permitted_actions: ["finalize_intake", "return_to_my_work"],
        });
      }
      if (url.includes("/finalize") && init?.method === "POST") {
        return jsonResponse({
          schema_version: "v2",
          succeeded: true,
          outcome_code: "accepted",
          status: "accepted",
          revision: 3,
          version_id: "33333333-3333-4333-8333-333333333333",
          version_number: 1,
          permitted_actions: ["preview_item", "return_to_my_work"],
        });
      }
      if (url.includes("/submission") && (!init?.method || init.method === "GET")) {
        return jsonResponse({
          schema_version: "v2",
          enrollment_id: "enr-1",
          enrollment_status: "active",
          intake_available: true,
          requirements: {
            contract_version: "submissions.material_policy.v1",
            max_attachment_count: 10,
            max_attachment_aggregate_bytes: 26214400,
            max_direct_text_bytes: 1048576,
            scanner_mode: "disabled_by_approved_policy",
            categories: [{ category: "direct_text", available: true, max_bytes: 1048576 }],
          },
          active_intake: null,
          version_history: [],
          permitted_actions: ["begin_intake", "return_to_my_work"],
        });
      }
      return jsonResponse({}, 404);
    }));

    render(
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/my-work/enr-1"]}>
          <Routes>
            <Route path="/my-work/:enrollmentId" element={<ProductionMyWorkDetailPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>,
    );

    expect(await screen.findByRole("heading", { name: "Campaign" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Submission" })).toBeInTheDocument();
    expect(screen.getByLabelText("Direct text")).toBeInTheDocument();
    expect(screen.getByLabelText("Attachments")).toBeInTheDocument();
    expect(screen.queryByText("Start Attempt")).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("Direct text"), { target: { value: "Direct text answer." } });
    fireEvent.click(screen.getByRole("button", { name: "Submit version" }));
    expect(await screen.findByRole("dialog", { name: "Submit this version?" })).toBeInTheDocument();
    fireEvent.click(screen.getAllByRole("button", { name: "Submit version" })[1]);
    await waitFor(() => {
      expect(screen.getByText(/Current intake state: accepted/)).toBeInTheDocument();
    });
  });

  it("cancels an active intake without submitting a new version", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return jsonResponse({ authenticated: true, csrf_token: "csrf" });
      }
      if (url.includes("/v1/assessment/shell")) {
        return jsonResponse({
          schema_version: "v1",
          actor_id: "part",
          organization_id: "org",
          relationship: "",
          navigation: [{ destination_id: "my-work", is_available: true }],
          permitted_actions: ["assessment.assignment.discover"],
        });
      }
      if (url.includes("/timing")) {
        return jsonResponse({
          schema_version: "v2",
          assignment: {
            enrollment_id: "enr-1",
            status: "active",
            visibility: "current",
            activity_title: "Campaign",
            task_title: "Task 1",
            time_zone_id: "UTC",
            deadline_utc: "2026-09-30T17:00:00Z",
            summary_available: true,
            permitted_actions: ["open_assignment"],
          },
          participant_consequence_code: "none",
        });
      }
      if (url.includes("/cancel") && init?.method === "POST") {
        return jsonResponse({
          schema_version: "v2",
          succeeded: true,
          outcome_code: "cancelled",
          status: "cancelled",
          revision: 2,
          permitted_actions: ["begin_intake", "return_to_my_work"],
        });
      }
      if (url.includes("/submission") && (!init?.method || init.method === "GET")) {
        return jsonResponse({
          schema_version: "v2",
          enrollment_id: "enr-1",
          enrollment_status: "active",
          intake_available: true,
          requirements: {
            contract_version: "submissions.material_policy.v1",
            max_attachment_count: 10,
            max_attachment_aggregate_bytes: 26214400,
            max_direct_text_bytes: 1048576,
            scanner_mode: "disabled_by_approved_policy",
            categories: [{ category: "direct_text", available: true, max_bytes: 1048576 }],
          },
          active_intake: {
            intake_id: "11111111-1111-4111-8111-111111111111",
            submission_id: "22222222-2222-4222-8222-222222222222",
            status: "received",
            revision: 1,
            created_at_utc: "2026-08-25T00:00:00Z",
            updated_at_utc: "2026-08-25T00:00:00Z",
            complete_receipt_at_utc: "2026-08-25T00:00:00Z",
            items: [],
            permitted_actions: ["cancel_intake", "finalize_intake", "return_to_my_work"],
          },
          version_history: [],
          permitted_actions: ["cancel_intake", "return_to_my_work"],
        });
      }
      return jsonResponse({}, 404);
    }));

    render(
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/my-work/enr-1"]}>
          <Routes>
            <Route path="/my-work/:enrollmentId" element={<ProductionMyWorkDetailPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>,
    );

    fireEvent.click(await screen.findByRole("button", { name: "Cancel intake" }));
    await waitFor(() => {
      expect(screen.getByText(/Current intake state: cancelled/)).toBeInTheDocument();
    });
  });

  it("clears preview content and focuses the unavailable message on permission loss", async () => {
    let previewDenied = false;
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return jsonResponse({ authenticated: true, csrf_token: "csrf" });
      }
      if (url.includes("/v1/assessment/shell")) {
        return jsonResponse({
          schema_version: "v1",
          actor_id: "part",
          organization_id: "org",
          relationship: "",
          navigation: [{ destination_id: "my-work", is_available: true }],
          permitted_actions: ["assessment.assignment.discover"],
        });
      }
      if (url.includes("/timing")) {
        return jsonResponse({
          schema_version: "v2",
          assignment: {
            enrollment_id: "enr-1",
            status: "active",
            visibility: "current",
            activity_title: "Campaign",
            task_title: "Task 1",
            time_zone_id: "UTC",
            deadline_utc: "2026-09-30T17:00:00Z",
            summary_available: true,
            permitted_actions: ["open_assignment"],
          },
          participant_consequence_code: "none",
        });
      }
      if (url.includes("/preview")) {
        previewDenied = true;
        return jsonResponse({ error: "not_found" }, 404);
      }
      if (url.includes("/versions/") && (!init?.method || init.method === "GET") && !url.includes("/preview")) {
        return jsonResponse({ items: [{ item_id: "44444444-4444-4444-8444-444444444444" }] });
      }
      if (url.includes("/submission") && (!init?.method || init.method === "GET")) {
        return jsonResponse({
          schema_version: "v2",
          enrollment_id: "enr-1",
          enrollment_status: "active",
          intake_available: true,
          requirements: {
            contract_version: "submissions.material_policy.v1",
            max_attachment_count: 10,
            max_attachment_aggregate_bytes: 26214400,
            max_direct_text_bytes: 1048576,
            scanner_mode: "disabled_by_approved_policy",
            categories: [{ category: "direct_text", available: true, max_bytes: 1048576 }],
          },
          active_intake: null,
          version_history: [{
            version_id: "33333333-3333-4333-8333-333333333333",
            version_number: 1,
            accepted_at_utc: "2026-08-25T00:00:00Z",
            item_count: 1,
          }],
          permitted_actions: ["begin_intake", "preview_item", "return_to_my_work"],
        });
      }
      return jsonResponse({}, 404);
    }));

    render(
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/my-work/enr-1"]}>
          <Routes>
            <Route path="/my-work/:enrollmentId" element={<ProductionMyWorkDetailPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>,
    );

    fireEvent.click(await screen.findByRole("button", { name: "Preview version 1" }));
    expect(await screen.findByText("This content is not available.")).toBeInTheDocument();
    expect(previewDenied).toBe(true);
    expect(screen.queryByText("Exact preview")).not.toBeInTheDocument();
  });

  it("refreshes durable intake after a failed submit so cancel remains available", async () => {
    let submissionGets = 0;
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return jsonResponse({ authenticated: true, csrf_token: "csrf" });
      }
      if (url.includes("/v1/assessment/shell")) {
        return jsonResponse({
          schema_version: "v1",
          actor_id: "part",
          organization_id: "org",
          relationship: "",
          navigation: [{ destination_id: "my-work", is_available: true }],
          permitted_actions: ["assessment.assignment.discover"],
        });
      }
      if (url.includes("/timing")) {
        return jsonResponse({
          schema_version: "v2",
          assignment: {
            enrollment_id: "enr-1",
            status: "active",
            visibility: "current",
            activity_title: "Campaign",
            task_title: "Task 1",
            time_zone_id: "UTC",
            deadline_utc: "2026-09-30T17:00:00Z",
            summary_available: true,
            permitted_actions: ["open_assignment"],
          },
          participant_consequence_code: "none",
        });
      }
      if (url.includes("/submission/intake") && init?.method === "POST" && !url.includes("/items") && !url.includes("/finalize")) {
        return jsonResponse({
          schema_version: "v2",
          succeeded: true,
          outcome_code: "receiving",
          intake_id: "11111111-1111-4111-8111-111111111111",
          status: "receiving",
          revision: 1,
          permitted_actions: ["complete_item", "cancel_intake", "return_to_my_work"],
        });
      }
      if (url.includes("/items") && init?.method === "POST") {
        return jsonResponse({
          schema_version: "v2",
          succeeded: true,
          outcome_code: "received",
          intake_id: "11111111-1111-4111-8111-111111111111",
          status: "received",
          revision: 2,
          permitted_actions: ["finalize_intake", "cancel_intake", "return_to_my_work"],
        });
      }
      if (url.includes("/finalize") && init?.method === "POST") {
        return jsonResponse({
          schema_version: "v2",
          succeeded: false,
          outcome_code: "cutoff_passed",
          permitted_actions: ["return_to_my_work"],
        }, 409);
      }
      if (url.includes("/submission") && (!init?.method || init.method === "GET")) {
        submissionGets += 1;
        return jsonResponse({
          schema_version: "v2",
          enrollment_id: "enr-1",
          enrollment_status: "active",
          intake_available: true,
          requirements: {
            contract_version: "submissions.material_policy.v1",
            max_attachment_count: 10,
            max_attachment_aggregate_bytes: 26214400,
            max_direct_text_bytes: 1048576,
            scanner_mode: "disabled_by_approved_policy",
            categories: [{ category: "direct_text", available: true, max_bytes: 1048576 }],
          },
          active_intake: submissionGets === 1
            ? null
            : {
              intake_id: "11111111-1111-4111-8111-111111111111",
              submission_id: "22222222-2222-4222-8222-222222222222",
              status: "received",
              revision: 2,
              created_at_utc: "2026-08-25T00:00:00Z",
              updated_at_utc: "2026-08-25T00:00:00Z",
              complete_receipt_at_utc: "2026-08-25T00:00:00Z",
              items: [],
              permitted_actions: ["cancel_intake", "finalize_intake", "return_to_my_work"],
            },
          version_history: [],
          permitted_actions: ["begin_intake", "return_to_my_work"],
        });
      }
      return jsonResponse({}, 404);
    }));

    render(
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/my-work/enr-1"]}>
          <Routes>
            <Route path="/my-work/:enrollmentId" element={<ProductionMyWorkDetailPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>,
    );

    fireEvent.change(await screen.findByLabelText("Direct text"), { target: { value: "Direct text answer." } });
    fireEvent.click(screen.getByRole("button", { name: "Submit version" }));
    fireEvent.click((await screen.findAllByRole("button", { name: "Submit version" }))[1]);
    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Cancel intake" })).toBeInTheDocument();
    });
    expect(screen.getByText(/Current intake state: received/)).toBeInTheDocument();
  });
});
