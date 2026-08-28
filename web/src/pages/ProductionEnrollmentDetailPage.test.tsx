import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { FlexQueryProvider } from "../api/query-client";
import { ProductionEnrollmentDetailPage } from "./ProductionEnrollmentDetailPage";

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  });
}

describe("ProductionEnrollmentDetailPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("loads Enrollment history and offers a bounded accommodation when permitted", async () => {
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
          relationship: "administrator",
          navigation: [{ destination_id: "activities", is_available: true }],
          permitted_actions: [],
        });
      }
      if (url.includes("/enrollments/enr-1/timing")) {
        return jsonResponse({
          schema_version: "v2",
          enrollment: {
            enrollment_id: "enr-1",
            status: "active",
            revision: 1,
            visibility: "administrator",
            permitted_actions: [],
          },
          baseline: {
            starts_at_utc: "2026-08-01T00:00:00Z",
            ends_at_utc: "2026-09-01T00:00:00Z",
            deadline_utc: "2026-09-01T12:00:00Z",
            time_zone_id: "UTC",
            attempt_limit: 1,
          },
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
          current_accommodations: [],
          policy_available: true,
          permitted_dimensions: ["submission_deadline_utc"],
          permitted_reason_categories: ["documented_need"],
          history: [],
        });
      }
      if (url.includes("/enrollments/enr-1")) {
        return jsonResponse({
          schema_version: "v1",
          enrollment: {
            enrollment_id: "enr-1",
            participant_actor_id: "p-1",
            display_label: "Pat Participant",
            status: "active",
            revision: 1,
            assigned_at: "2026-08-01T00:00:00Z",
            updated_at: "2026-08-01T00:00:00Z",
            visibility: "administrator",
            permitted_actions: ["suspend"],
          },
          history: [
            {
              sequence: 1,
              prior_status: "none",
              new_status: "active",
              reason_code: "assigned",
              occurred_at: "2026-08-01T00:00:00Z",
            },
          ],
        });
      }
      return jsonResponse({}, 404);
    }));

    render(
      <FlexQueryProvider>
        <ProductionApiProvider>
          <MemoryRouter initialEntries={["/activities/act-1/cohorts/coh-1/enrollments/enr-1"]}>
            <Routes>
              <Route
                path="/activities/:activityId/cohorts/:cohortId/enrollments/:enrollmentId"
                element={<ProductionEnrollmentDetailPage />}
              />
            </Routes>
          </MemoryRouter>
        </ProductionApiProvider>
      </FlexQueryProvider>,
    );

    expect(await screen.findByRole("heading", { name: "Pat Participant" })).toBeInTheDocument();
    const timing = screen.getByText(/Exclusive submission end/);
    expect(timing).not.toHaveTextContent("2026-09-01T12:00:00Z");
    expect(timing).not.toHaveTextContent(/conversion unavailable/i);
    expect(timing).toHaveTextContent(/2026/);
    expect(screen.getByRole("link", { name: "Participants" })).toHaveAttribute(
      "href",
      "/activities/act-1/cohorts/coh-1/enrollments",
    );
    expect(screen.getByRole("heading", { name: "Enrollment actions" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Suspend" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Request accommodation" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Request accommodation" }));
    expect(await screen.findByRole("dialog", { name: "Request a bounded accommodation?" })).toBeInTheDocument();
    expect(screen.getByRole("checkbox", { name: "Requires a distinct fairness-exception approver" })).toBeInTheDocument();
  });

  it("offers Approve exception for a pending fairness request", async () => {
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
          relationship: "administrator",
          navigation: [{ destination_id: "activities", is_available: true }],
          permitted_actions: [],
        });
      }
      if (url.includes("/enrollments/enr-1/timing")) {
        return jsonResponse({
          schema_version: "v2",
          enrollment: {
            enrollment_id: "enr-1",
            status: "active",
            revision: 1,
            visibility: "administrator",
            permitted_actions: [],
          },
          baseline: {
            starts_at_utc: "2026-08-01T00:00:00Z",
            ends_at_utc: "2026-09-01T00:00:00Z",
            deadline_utc: "2026-09-01T12:00:00Z",
            time_zone_id: "UTC",
            attempt_limit: 1,
          },
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
          current_accommodations: [],
          policy_available: true,
          permitted_dimensions: ["submission_deadline_utc"],
          permitted_reason_categories: ["documented_need"],
          history: [
            {
              accommodation_id: "acc-1",
              dimension: "submission_deadline_utc",
              status: "pending_approval",
              normalized_value: "2026-09-08T12:00:00Z",
              reason_category: "documented_need",
              fairness_exception: true,
              revision: 1,
              created_at_utc: "2026-08-28T00:00:00Z",
            },
          ],
        });
      }
      if (url.includes("/enrollments/enr-1")) {
        return jsonResponse({
          schema_version: "v1",
          enrollment: {
            enrollment_id: "enr-1",
            participant_actor_id: "p-1",
            display_label: "Pat Participant",
            status: "active",
            revision: 1,
            assigned_at: "2026-08-01T00:00:00Z",
            updated_at: "2026-08-01T00:00:00Z",
            visibility: "administrator",
            permitted_actions: ["suspend"],
          },
          history: [],
        });
      }
      return jsonResponse({}, 404);
    }));

    render(
      <FlexQueryProvider>
        <ProductionApiProvider>
          <MemoryRouter initialEntries={["/activities/act-1/cohorts/coh-1/enrollments/enr-1"]}>
            <Routes>
              <Route
                path="/activities/:activityId/cohorts/:cohortId/enrollments/:enrollmentId"
                element={<ProductionEnrollmentDetailPage />}
              />
            </Routes>
          </MemoryRouter>
        </ProductionApiProvider>
      </FlexQueryProvider>,
    );

    expect(await screen.findByRole("button", { name: "Approve exception" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reject exception" })).toBeInTheDocument();
  });
});
