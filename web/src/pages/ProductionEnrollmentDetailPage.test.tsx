import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { FlexQueryProvider } from "../api/query-client";
import { ProductionEnrollmentDetailPage } from "./ProductionEnrollmentDetailPage";

describe("ProductionEnrollmentDetailPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("shows baseline and effective timing without using the browser clock for eligibility", async () => {
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
            actor_id: "admin",
            organization_id: "org",
            relationship: "administrator",
            navigation: [{ destination_id: "activities", is_available: true }],
            permitted_actions: ["assessment.enrollment.read"],
          }),
        });
      }
      if (url.includes("/timing")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v2",
            enrollment: {
              enrollment_id: "enr-1",
              status: "active",
              revision: 1,
              visibility: "current",
              permitted_actions: ["request_accommodation"],
            },
            baseline: {
              starts_at_utc: "2026-09-01T12:00:00Z",
              ends_at_utc: "2026-09-30T17:00:00Z",
              deadline_utc: "2026-09-20T17:00:00Z",
              time_zone_id: "America/New_York",
              attempt_limit: 2,
              per_attempt_duration_seconds: 3600,
            },
            effective: {
              submission_starts_at_utc: "2026-09-01T12:00:00Z",
              submission_exclusive_end_utc: "2026-09-22T17:00:00Z",
              attempt_start_utc: "2026-09-01T12:00:00Z",
              attempt_start_exclusive_end_utc: "2026-09-30T17:00:00Z",
              per_attempt_duration_seconds: 3600,
              evaluated_at_utc: "2026-08-24T02:00:00Z",
              eligibility_state: "open",
              is_authoritative: true,
              time_zone_id: "America/New_York",
              participant_consequence_code: "deadline_replacement",
            },
            current_accommodations: [{
              accommodation_id: "acc-1",
              dimension: "submission_deadline_utc",
              consequence_code: "deadline_replacement",
            }],
            policy_available: true,
            permitted_dimensions: ["submission_deadline_utc"],
            permitted_reason_categories: ["development.synthetic.timing"],
            history: [{
              accommodation_id: "acc-1",
              dimension: "submission_deadline_utc",
              status: "granted",
              normalized_value: "2026-09-22T17:00:00Z",
              reason_category: "development.synthetic.timing",
              fairness_exception: false,
              revision: 1,
              created_at_utc: "2026-08-22T06:00:00Z",
              decided_at_utc: "2026-08-22T06:00:00Z",
              expires_at_utc: null,
            }],
          }),
        });
      }
      if (url.includes("/enrollments/enr-1")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            enrollment: {
              enrollment_id: "enr-1",
              participant_actor_id: "part-1",
              display_label: "Synthetic Participant",
              status: "active",
              revision: 1,
              assigned_at: "2026-08-22T06:00:00Z",
              updated_at: "2026-08-22T06:00:00Z",
              visibility: "current",
              permitted_actions: [],
            },
            history: [],
          }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
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

    expect(await screen.findByRole("heading", { name: "Synthetic Participant" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Baseline timing" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Effective timing" })).toBeInTheDocument();
    expect(screen.getAllByText(/2026-09-20T17:00:00Z/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/2026-09-22T17:00:00Z/).length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: "Request accommodation" })).toBeInTheDocument();
  });

  it("revokes using the current accommodation revision instead of the enrollment revision", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      void init;
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
            actor_id: "admin",
            organization_id: "org",
            relationship: "administrator",
            navigation: [{ destination_id: "activities", is_available: true }],
            permitted_actions: ["assessment.enrollment.read"],
          }),
        });
      }
      if (url.includes("/revoke")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v2",
            succeeded: true,
            outcome_code: "accommodation.revoked",
            permitted_actions: [],
          }),
        });
      }
      if (url.includes("/timing")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v2",
            enrollment: {
              enrollment_id: "enr-1",
              status: "active",
              revision: 1,
              visibility: "current",
              permitted_actions: ["revoke_accommodation"],
            },
            baseline: {
              starts_at_utc: "2026-09-01T12:00:00Z",
              ends_at_utc: "2026-09-30T17:00:00Z",
              deadline_utc: "2026-09-20T17:00:00Z",
              time_zone_id: "America/New_York",
              attempt_limit: 2,
              per_attempt_duration_seconds: 3600,
            },
            effective: {
              submission_starts_at_utc: "2026-09-01T12:00:00Z",
              submission_exclusive_end_utc: "2026-09-22T17:00:00Z",
              attempt_start_utc: "2026-09-01T12:00:00Z",
              attempt_start_exclusive_end_utc: "2026-09-30T17:00:00Z",
              per_attempt_duration_seconds: 3600,
              evaluated_at_utc: "2026-08-24T02:00:00Z",
              eligibility_state: "open",
              is_authoritative: true,
              time_zone_id: "America/New_York",
              participant_consequence_code: "deadline_replacement",
            },
            current_accommodations: [{
              accommodation_id: "acc-1",
              dimension: "submission_deadline_utc",
              consequence_code: "deadline_replacement",
            }],
            policy_available: true,
            permitted_dimensions: ["submission_deadline_utc"],
            permitted_reason_categories: ["development.synthetic.timing"],
            history: [{
              accommodation_id: "acc-1",
              dimension: "submission_deadline_utc",
              status: "granted",
              normalized_value: "2026-09-22T17:00:00Z",
              reason_category: "development.synthetic.timing",
              fairness_exception: true,
              revision: 2,
              created_at_utc: "2026-08-22T06:00:00Z",
              decided_at_utc: "2026-08-22T06:30:00Z",
              expires_at_utc: null,
            }],
          }),
        });
      }
      if (url.includes("/enrollments/enr-1")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            enrollment: {
              enrollment_id: "enr-1",
              participant_actor_id: "part-1",
              display_label: "Synthetic Participant",
              status: "active",
              revision: 1,
              assigned_at: "2026-08-22T06:00:00Z",
              updated_at: "2026-08-22T06:00:00Z",
              visibility: "current",
              permitted_actions: [],
            },
            history: [],
          }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    });
    vi.stubGlobal("fetch", fetchMock);

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

    fireEvent.click(await screen.findByRole("button", { name: "Revoke submission_deadline_utc accommodation" }));

    const revokeCall = fetchMock.mock.calls.find(([input]) => {
      if (typeof input === "string") {
        return input.includes("/revoke");
      }
      if (input instanceof URL) {
        return input.href.includes("/revoke");
      }
      return input instanceof Request && input.url.includes("/revoke");
    });
    expect(revokeCall).toBeDefined();
    const requestInit = revokeCall?.[1];
    expect(requestInit).toBeDefined();
    const rawBody = requestInit && "body" in requestInit ? requestInit.body : undefined;
    expect(typeof rawBody).toBe("string");
    const body = JSON.parse(rawBody as string) as { expected_revision: number };
    expect(body.expected_revision).toBe(2);
  });
});
