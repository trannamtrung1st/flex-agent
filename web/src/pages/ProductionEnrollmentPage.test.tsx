import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { FlexQueryProvider } from "../api/query-client";
import { ProductionEnrollmentPage } from "./ProductionEnrollmentPage";

function jsonBody(init?: RequestInit): { idempotency_key?: string; participant_actor_id?: string } {
  if (typeof init?.body !== "string") {
    return {};
  }

  return JSON.parse(init.body) as { idempotency_key?: string; participant_actor_id?: string };
}

describe("ProductionEnrollmentPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("assigns a selected eligible participant", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
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
            permitted_actions: ["assessment.enrollment.assign"],
          }),
        });
      }
      if (url.includes("participant-options")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            items: [{ actor_id: "part-1", display_label: "Synthetic Participant" }],
            has_more: false,
          }),
        });
      }
      if (url.includes("/enrollments") && init?.method === "POST") {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            succeeded: true,
            outcome_code: "enrollment.assigned",
            enrollment_id: "enr-1",
            status: "active",
            revision: 1,
            visibility: "current",
            permitted_actions: ["suspend_enrollment"],
          }),
        });
      }
      if (url.includes("/enrollments")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({ schema_version: "v1", items: [], has_more: false }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <FlexQueryProvider>
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/activities/act-1/cohorts/coh-1/participants"]}>
          <Routes>
            <Route path="/activities/:activityId/cohorts/:cohortId/participants" element={<ProductionEnrollmentPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>
    </FlexQueryProvider>,
    );

    expect(await screen.findByRole("heading", { name: "Assign Participants" })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Participant"), { target: { value: "part-1" } });
    fireEvent.click(screen.getByRole("button", { name: "Assign Participant" }));
    expect(await screen.findByText("Participant assigned.")).toBeInTheDocument();
  });

  it("explains a live other-cohort conflict and confirms a suspend", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
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
            permitted_actions: ["assessment.enrollment.assign"],
          }),
        });
      }
      if (url.includes("participant-options")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            items: [{ actor_id: "part-1", display_label: "Synthetic Participant" }],
            has_more: false,
          }),
        });
      }
      if (url.includes("/suspend") && init?.method === "POST") {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            succeeded: true,
            outcome_code: "enrollment.suspended",
            enrollment_id: "enr-1",
            status: "suspended",
            revision: 2,
            visibility: "restricted",
            permitted_actions: ["restore_enrollment"],
          }),
        });
      }
      if (url.includes("/enrollments") && init?.method === "POST") {
        return Promise.resolve({
          ok: false,
          status: 409,
          json: () => Promise.resolve({
            schema_version: "v1",
            succeeded: false,
            outcome_code: "enrollment.conflict",
            permitted_actions: [],
          }),
          clone() {
            return this;
          },
        });
      }
      if (url.includes("/enrollments")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            items: [{
              enrollment_id: "enr-1",
              participant_actor_id: "part-1",
              display_label: "Synthetic Participant",
              status: "active",
              revision: 1,
              assigned_at: "2026-08-22T00:00:00Z",
              updated_at: "2026-08-22T00:00:00Z",
              visibility: "current",
              permitted_actions: ["suspend_enrollment"],
            }],
            has_more: false,
          }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <FlexQueryProvider>
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/activities/act-1/cohorts/coh-1/participants"]}>
          <Routes>
            <Route path="/activities/:activityId/cohorts/:cohortId/participants" element={<ProductionEnrollmentPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>
    </FlexQueryProvider>,
    );

    expect(await screen.findByRole("heading", { name: "Assign Participants" })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Participant"), { target: { value: "part-1" } });
    fireEvent.click(screen.getByRole("button", { name: "Assign Participant" }));
    expect(await screen.findByText("This Participant already has a live Enrollment in another Cohort.")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Suspend" }));
    expect(screen.getByRole("heading", { name: "Suspend this Enrollment?" })).toBeInTheDocument();
    expect(screen.getByText(/The Participant will still see this Assignment as Suspended/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Confirm suspend" }));
    expect(await screen.findByText("Enrollment updated.")).toBeInTheDocument();
  });

  it("reuses one lifecycle idempotency key after a lost response", async () => {
    const keys: string[] = [];
    vi.stubGlobal("crypto", { randomUUID: () => "fixed-key" });
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
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
            permitted_actions: ["assessment.enrollment.assign"],
          }),
        });
      }
      if (url.includes("participant-options")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({ schema_version: "v1", items: [], has_more: false }),
        });
      }
      if (url.includes("/suspend") && init?.method === "POST") {
        const body = jsonBody(init);
        keys.push(body.idempotency_key ?? "");
        if (keys.length === 1) {
          return Promise.reject(new TypeError("Failed to fetch"));
        }
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            succeeded: true,
            outcome_code: "enrollment.suspended",
            enrollment_id: "enr-1",
            status: "suspended",
            revision: 2,
            visibility: "restricted",
            permitted_actions: ["restore_enrollment"],
          }),
        });
      }
      if (url.includes("/enrollments")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            items: [{
              enrollment_id: "enr-1",
              participant_actor_id: "part-1",
              display_label: "Synthetic Participant",
              status: "active",
              revision: 1,
              assigned_at: "2026-08-22T00:00:00Z",
              updated_at: "2026-08-22T00:00:00Z",
              visibility: "current",
              permitted_actions: ["suspend_enrollment"],
            }],
            has_more: false,
          }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <FlexQueryProvider>
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/activities/act-1/cohorts/coh-1/participants"]}>
          <Routes>
            <Route path="/activities/:activityId/cohorts/:cohortId/participants" element={<ProductionEnrollmentPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>
    </FlexQueryProvider>,
    );

    expect(await screen.findByRole("heading", { name: "Assign Participants" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Suspend" }));
    fireEvent.click(screen.getByRole("button", { name: "Confirm suspend" }));
    expect(await screen.findByText("The Enrollment could not be updated.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Confirm suspend" }));
    expect(await screen.findByText("Enrollment updated.")).toBeInTheDocument();
    expect(keys).toEqual(["enr-fixed-key", "enr-fixed-key"]);
  });

  it("issues a new assign key when the selected Participant changes after a lost response", async () => {
    let uuid = 0;
    const keys: Array<{ participant: string; key: string }> = [];
    vi.stubGlobal("crypto", { randomUUID: () => `key-${String(++uuid)}` });
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
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
            permitted_actions: ["assessment.enrollment.assign"],
          }),
        });
      }
      if (url.includes("participant-options")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            items: [
              { actor_id: "part-1", display_label: "Synthetic Participant" },
              { actor_id: "part-2", display_label: "Second Participant" },
            ],
            has_more: false,
          }),
        });
      }
      if (url.includes("/enrollments") && init?.method === "POST") {
        const body = jsonBody(init);
        keys.push({ participant: body.participant_actor_id ?? "", key: body.idempotency_key ?? "" });
        if (keys.length === 1) {
          return Promise.reject(new TypeError("Failed to fetch"));
        }
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            succeeded: true,
            outcome_code: "enrollment.assigned",
            enrollment_id: "enr-2",
            status: "active",
            revision: 1,
            visibility: "current",
            permitted_actions: ["suspend_enrollment"],
          }),
        });
      }
      if (url.includes("/enrollments")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({ schema_version: "v1", items: [], has_more: false }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <FlexQueryProvider>
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/activities/act-1/cohorts/coh-1/participants"]}>
          <Routes>
            <Route path="/activities/:activityId/cohorts/:cohortId/participants" element={<ProductionEnrollmentPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>
    </FlexQueryProvider>,
    );

    expect(await screen.findByRole("heading", { name: "Assign Participants" })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Participant"), { target: { value: "part-1" } });
    fireEvent.click(screen.getByRole("button", { name: "Assign Participant" }));
    expect(await screen.findByText("The assignment could not be completed.")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Participant"), { target: { value: "part-2" } });
    fireEvent.click(screen.getByRole("button", { name: "Assign Participant" }));
    expect(await screen.findByText("Participant assigned.")).toBeInTheDocument();
    expect(keys).toEqual([
      { participant: "part-1", key: "enr-key-1" },
      { participant: "part-2", key: "enr-key-2" },
    ]);
  });

  it("explains a rate-limited assignment as a recoverable wait", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
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
            permitted_actions: ["assessment.enrollment.assign"],
          }),
        });
      }
      if (url.includes("participant-options")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            items: [{ actor_id: "part-1", display_label: "Synthetic Participant" }],
            has_more: false,
          }),
        });
      }
      if (url.includes("/enrollments") && init?.method === "POST") {
        return Promise.resolve({
          ok: false,
          status: 429,
          json: () => Promise.resolve({ error: "enrollment.rate_limited" }),
          clone() {
            return this;
          },
        });
      }
      if (url.includes("/enrollments")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({ schema_version: "v1", items: [], has_more: false }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <FlexQueryProvider>
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/activities/act-1/cohorts/coh-1/participants"]}>
          <Routes>
            <Route path="/activities/:activityId/cohorts/:cohortId/participants" element={<ProductionEnrollmentPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>
    </FlexQueryProvider>,
    );

    expect(await screen.findByRole("heading", { name: "Assign Participants" })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Participant"), { target: { value: "part-1" } });
    fireEvent.click(screen.getByRole("button", { name: "Assign Participant" }));
    expect(await screen.findByText("Too many requests. Wait a moment, then try again.")).toBeInTheDocument();
    expect(screen.getByLabelText("Participant")).toHaveValue("part-1");
  });
});
