import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { FlexQueryProvider } from "../api/query-client";
import { ProductionEnrollmentPage } from "./ProductionEnrollmentPage";

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  });
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
        relationship: "administrator",
        navigation: [{ destination_id: "activities", is_available: true }],
        permitted_actions: [],
      });
    }
    return handler(url, init);
  }));
}

function renderPage() {
  return render(
    <FlexQueryProvider>
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/activities/act-1/cohorts/coh-1/enrollments"]}>
          <Routes>
            <Route
              path="/activities/:activityId/cohorts/:cohortId/enrollments"
              element={<ProductionEnrollmentPage />}
            />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>
    </FlexQueryProvider>,
  );
}

describe("ProductionEnrollmentPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("presents assigned Participants in a flush registry table", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/participant-options")) {
        return jsonResponse({ schema_version: "v1", items: [{ actor_id: "p-2", display_label: "Casey Candidate" }], has_more: false });
      }
      if (url.includes("/enrollments")) {
        return jsonResponse({
          schema_version: "v1",
          items: [{
            enrollment_id: "enr-1",
            participant_actor_id: "p-1",
            display_label: "Pat Participant",
            status: "active",
            revision: 1,
            assigned_at: "2026-08-01T00:00:00Z",
            updated_at: "2026-08-01T00:00:00Z",
            visibility: "administrator",
            permitted_actions: [],
          }],
          has_more: false,
        });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    const link = await screen.findByRole("link", { name: "Pat Participant" });
    expect(link).toHaveClass("datatable-id");
    expect(link).toHaveAttribute("href", "/activities/act-1/cohorts/coh-1/enrollments/enr-1");
    expect(screen.getByRole("table", { name: "Participants" })).toHaveClass("datatable-table");
    expect(screen.getByRole("columnheader", { name: "Enrollment" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Assigned" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Updated" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Rev" })).toBeInTheDocument();
    expect(screen.getByText("enr…1")).toBeInTheDocument();
    fireEvent.mouseEnter(screen.getByText("enr…1").closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toHaveTextContent("enr-1");
    expect(link.closest("tr")?.children.item(5)).toHaveTextContent("1");
    expect(link.closest(".work-plane")).toHaveClass("registry-wall--hug");
    expect(screen.getByRole("searchbox", { name: "Search participant, enrollment, or status" })).toHaveAttribute(
      "placeholder",
      "SEARCH NAME OR ID",
    );
    expect(link.closest(".frame-cut")).toHaveClass("datatable-frame", "frame-cut--flush");
    expect(screen.getByRole("link", { name: "Setup" })).toHaveAttribute("href", "/activities/act-1/setup");
    expect(screen.getByRole("button", { name: "Assign Casey Candidate" })).toBeInTheDocument();
  });

  it("filters assigned Participants by enrollment id and status", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/participant-options")) {
        return jsonResponse({ schema_version: "v1", items: [], has_more: false });
      }
      if (url.includes("/enrollments")) {
        return jsonResponse({
          schema_version: "v1",
          items: [
            {
              enrollment_id: "enr-active",
              participant_actor_id: "p-1",
              display_label: "Pat Participant",
              status: "active",
              revision: 1,
              assigned_at: "2026-08-01T00:00:00Z",
              updated_at: "2026-08-01T00:00:00Z",
              visibility: "administrator",
              permitted_actions: [],
            },
            {
              enrollment_id: "enr-suspended",
              participant_actor_id: "p-2",
              display_label: "Sam Suspended",
              status: "suspended",
              revision: 2,
              assigned_at: "2026-08-02T00:00:00Z",
              updated_at: "2026-08-02T00:00:00Z",
              visibility: "administrator",
              permitted_actions: [],
            },
          ],
          has_more: false,
        });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    await screen.findByRole("link", { name: "Pat Participant" });
    fireEvent.change(screen.getByRole("searchbox", { name: "Search participant, enrollment, or status" }), {
      target: { value: "enr-suspended" },
    });
    expect(screen.queryByRole("link", { name: "Pat Participant" })).not.toBeInTheDocument();
    expect(await screen.findByRole("link", { name: "Sam Suspended" })).toBeInTheDocument();

    fireEvent.change(screen.getByRole("searchbox", { name: "Search participant, enrollment, or status" }), {
      target: { value: "suspended" },
    });
    expect(screen.queryByRole("link", { name: "Pat Participant" })).not.toBeInTheDocument();
    expect(await screen.findByRole("link", { name: "Sam Suspended" })).toBeInTheDocument();
  });

  it("sorts assigned Participants by record status", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/participant-options")) {
        return jsonResponse({ schema_version: "v1", items: [], has_more: false });
      }
      if (url.includes("/enrollments")) {
        return jsonResponse({
          schema_version: "v1",
          items: [
            {
              enrollment_id: "enr-z",
              participant_actor_id: "p-2",
              display_label: "Zoe Zulu",
              status: "suspended",
              revision: 1,
              assigned_at: "2026-08-02T00:00:00Z",
              updated_at: "2026-08-02T00:00:00Z",
              visibility: "administrator",
              permitted_actions: [],
            },
            {
              enrollment_id: "enr-a",
              participant_actor_id: "p-1",
              display_label: "Alex Alpha",
              status: "active",
              revision: 1,
              assigned_at: "2026-08-01T00:00:00Z",
              updated_at: "2026-08-01T00:00:00Z",
              visibility: "administrator",
              permitted_actions: [],
            },
          ],
          has_more: false,
        });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    await screen.findByRole("link", { name: "Zoe Zulu" });
    fireEvent.click(screen.getByRole("button", { name: "Record" }));
    const rows = screen.getAllByRole("row").slice(1);
    expect(rows[0]).toHaveTextContent("Alex Alpha");
    expect(rows[1]).toHaveTextContent("Zoe Zulu");
  });

  it("keeps assign actions when the cohort has no Participants yet", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/participant-options")) {
        return jsonResponse({ schema_version: "v1", items: [{ actor_id: "p-2", display_label: "Casey Candidate" }], has_more: false });
      }
      if (url.includes("/enrollments")) {
        return jsonResponse({ schema_version: "v1", items: [], has_more: false });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    expect(await screen.findByText("No Participants assigned")).toBeInTheDocument();
    expect(document.querySelector(".datatable-empty")).toHaveClass("datatable-empty", "empty-plate--inset");
    expect(document.querySelector(".registry-wall--empty")).toBeNull();
    expect(screen.getByRole("button", { name: "Assign Casey Candidate" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Assign Casey Candidate" }).closest(".datatable-actions")).not.toBeNull();
    expect(document.querySelector(".datatable-empty")?.querySelector(".key")).toBeNull();
  });

  it("seats a Clear search key in the empty plate when no enrollments match the query", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/participant-options")) {
        return jsonResponse({ schema_version: "v1", items: [], has_more: false });
      }
      if (url.includes("/enrollments")) {
        return jsonResponse({
          schema_version: "v1",
          items: [{
            enrollment_id: "enr-1",
            participant_actor_id: "p-1",
            display_label: "Pat Participant",
            status: "active",
            revision: 1,
            assigned_at: "2026-08-01T00:00:00Z",
            updated_at: "2026-08-01T00:00:00Z",
            visibility: "administrator",
            permitted_actions: [],
          }],
          has_more: false,
        });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    await screen.findByRole("link", { name: "Pat Participant" });
    fireEvent.change(screen.getByRole("searchbox", { name: "Search participant, enrollment, or status" }), {
      target: { value: "zzz-no-match" },
    });
    expect(await screen.findByText("No matching enrollments")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Clear search" }).closest(".datatable-empty")).not.toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Clear search" }));
    expect(await screen.findByRole("link", { name: "Pat Participant" })).toBeInTheDocument();
  });

  it("keeps assigned Participants when assignable options fail", async () => {
    stubAuthenticatedFetch((url) => {
      if (url.includes("/participant-options")) {
        return jsonResponse({}, 404);
      }
      if (url.includes("/enrollments")) {
        return jsonResponse({
          schema_version: "v1",
          items: [{
            enrollment_id: "enr-1",
            participant_actor_id: "p-1",
            display_label: "Pat Participant",
            status: "active",
            revision: 1,
            assigned_at: "2026-08-01T00:00:00Z",
            updated_at: "2026-08-01T00:00:00Z",
            visibility: "administrator",
            permitted_actions: [],
          }],
          has_more: false,
        });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    expect(await screen.findByRole("link", { name: "Pat Participant" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Participants unavailable" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Assign / })).not.toBeInTheDocument();
    expect(screen.getByText("Assignable Participants are not available.")).toBeInTheDocument();
  });

  it("refreshes assignable Participants after a successful assignment", async () => {
    let candidates = [{ actor_id: "p-2", display_label: "Casey Candidate" }];
    stubAuthenticatedFetch((url, init) => {
      if (url.includes("/participant-options")) {
        return jsonResponse({ schema_version: "v1", items: candidates, has_more: false });
      }
      if (url.includes("/enrollments") && init?.method === "POST") {
        candidates = [];
        return jsonResponse({
          schema_version: "v1",
          succeeded: true,
          outcome_code: "enrollment.assigned",
          permitted_actions: [],
        });
      }
      if (url.includes("/enrollments")) {
        return jsonResponse({
          schema_version: "v1",
          items: [{
            enrollment_id: "enr-1",
            participant_actor_id: "p-2",
            display_label: "Casey Candidate",
            status: "active",
            revision: 1,
            assigned_at: "2026-08-01T00:00:00Z",
            updated_at: "2026-08-01T00:00:00Z",
            visibility: "administrator",
            permitted_actions: [],
          }],
          has_more: false,
        });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    fireEvent.click(await screen.findByRole("button", { name: "Assign Casey Candidate" }));
    await waitFor(() => {
      expect(screen.queryByRole("button", { name: "Assign Casey Candidate" })).not.toBeInTheDocument();
    });
    expect(screen.getByRole("link", { name: "Casey Candidate" })).toBeInTheDocument();
  });

  it("keeps a completed assignment when assignable options cannot refresh", async () => {
    let assigned = false;
    stubAuthenticatedFetch((url, init) => {
      if (url.includes("/participant-options")) {
        if (assigned) {
          return jsonResponse({}, 404);
        }
        return jsonResponse({ schema_version: "v1", items: [{ actor_id: "p-2", display_label: "Casey Candidate" }], has_more: false });
      }
      if (url.includes("/enrollments") && init?.method === "POST") {
        assigned = true;
        return jsonResponse({
          schema_version: "v1",
          succeeded: true,
          outcome_code: "enrollment.assigned",
          permitted_actions: [],
        });
      }
      if (url.includes("/enrollments")) {
        return jsonResponse({
          schema_version: "v1",
          items: assigned ? [{
            enrollment_id: "enr-1",
            participant_actor_id: "p-2",
            display_label: "Casey Candidate",
            status: "active",
            revision: 1,
            assigned_at: "2026-08-01T00:00:00Z",
            updated_at: "2026-08-01T00:00:00Z",
            visibility: "administrator",
            permitted_actions: [],
          }] : [],
          has_more: false,
        });
      }
      return jsonResponse({}, 404);
    });

    renderPage();

    fireEvent.click(await screen.findByRole("button", { name: "Assign Casey Candidate" }));
    expect(await screen.findByRole("link", { name: "Casey Candidate" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Assign Casey Candidate" })).not.toBeInTheDocument();
    expect(screen.queryByText("Assignment did not complete.")).not.toBeInTheDocument();
    expect(screen.getByText("Assignable Participants are not available.")).toBeInTheDocument();
  });
});
