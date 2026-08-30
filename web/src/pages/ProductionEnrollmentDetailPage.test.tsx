import { fireEvent, render, screen, within } from "@testing-library/react";
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
            eligibility_state: "too_early",
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
    const identity = screen.getByLabelText("Enrollment identity");
    const operate = screen.getByRole("region", { name: "Enrollment" });
    expect(operate.querySelector(".frame-cut")).toBeNull();
    expect(identity.closest(".frame-cut")).toBeNull();
    expect(operate.querySelector(".record-frame")).toBeNull();
    expect(operate).toHaveClass("record-plane");
    expect(operate).not.toHaveClass("record-plane--setup");
    expect(operate.querySelector(":scope > .operate-scroll")).toContainElement(identity);
    expect(screen.getByText("Too early")).toBeInTheDocument();
    expect(screen.queryByText(/too_early/)).not.toBeInTheDocument();
    expect(screen.getByText("Active. History remains inspectable.")).toBeInTheDocument();
    const exclusiveEnd = screen.getByText("Effective exclusive end").closest("div");
    expect(exclusiveEnd).not.toHaveTextContent("2026-09-01T12:00:00Z");
    expect(exclusiveEnd).not.toHaveTextContent(/conversion unavailable/i);
    expect(exclusiveEnd).toHaveTextContent(/2026/);
    expect(screen.getByText("Baseline deadline")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Participants" })).toHaveAttribute(
      "href",
      "/activities/act-1/cohorts/coh-1/enrollments",
    );
    expect(screen.getByRole("heading", { name: "Enrollment actions" })).toBeInTheDocument();
    const operateScroll = operate.querySelector(":scope > .operate-scroll");
    expect(operateScroll).toBeTruthy();
    const wellBodies = [...document.querySelectorAll(".record-plane .work-well__body")];
    expect(wellBodies.length).toBeGreaterThan(1);
    for (const body of wellBodies) {
      expect(operateScroll as HTMLElement).toContainElement(body as HTMLElement);
    }
    expect(screen.getByRole("button", { name: "Suspend Enrollment" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Request accommodation" })).toBeInTheDocument();
    expect(screen.getByText("No accommodations")).toBeInTheDocument();
    expect(screen.queryByText("2026-08-01T00:00:00Z")).not.toBeInTheDocument();
    expect(screen.getByText(/Assigned/)).toBeInTheDocument();
    const history = screen.getByRole("article", { name: "History" });
    const historyList = within(history).getByRole("list");
    expect(historyList.tagName).toBe("OL");
    const historyItem = historyList.querySelector(":scope > li");
    expect(historyItem).toHaveAttribute("data-sequence", "1");
    expect(historyItem?.querySelector(":scope > .composition-stack")).not.toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Request accommodation" }));
    const dialog = await screen.findByRole("dialog", { name: "Request a bounded accommodation?" });
    expect(dialog).toBeInTheDocument();
    expect(document.querySelector(".dialog-plate--wide")).not.toBeNull();
    expect(document.querySelector(".dialog-plate--accommodation")).toBeNull();
    expect(screen.getByRole("button", { name: /Requested value/ })).toHaveTextContent("2026-09-01 12:00");
    expect(screen.getByText(/Campaign timezone UTC/)).toBeInTheDocument();
    expect(screen.queryByPlaceholderText("2026-09-01T12:00:00Z")).not.toBeInTheDocument();
    expect(screen.getByRole("checkbox", { name: "Requires a distinct fairness-exception approver" })).toBeInTheDocument();
    expect(screen.getByText("Submission deadline")).toBeInTheDocument();
    expect(screen.getByText("Current exclusive end")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /Requested value/ }));
    expect(screen.getByRole("button", { name: "Now" })).toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "Cancel" })).toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "Request accommodation" })).toBeInTheDocument();
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

  it("keeps the request dialog open and shows the failure in the plate", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
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
      if (url.includes("/accommodations") && init?.method === "POST") {
        return jsonResponse({ outcome_code: "accommodation.invalid_value" }, 400);
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
            eligibility_state: "too_early",
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
          history: [],
        });
      }
      return jsonResponse({}, 404);
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

    fireEvent.click(await screen.findByRole("button", { name: "Request accommodation" }));
    const dialog = await screen.findByRole("dialog", { name: "Request a bounded accommodation?" });
    fireEvent.click(within(dialog).getByRole("button", { name: "Request accommodation" }));
    const grantCall = fetchMock.mock.calls.find((call) => {
      const [input, init] = call;
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      return url.includes("/accommodations") && init?.method === "POST";
    });
    expect(grantCall).toBeDefined();
    const grantBody = grantCall?.[1]?.body;
    expect(typeof grantBody).toBe("string");
    expect(JSON.parse(grantBody as string).requested_value).toBe("2026-09-01T12:00:00Z");
    expect(await screen.findByText("The accommodation could not be recorded.")).toBeInTheDocument();
    expect(screen.getByText("Request did not complete")).toBeInTheDocument();
  });
});
