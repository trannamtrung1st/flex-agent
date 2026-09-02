import type { ReactNode } from "react";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { FlexQueryProvider } from "../api/query-client";
import { ProductionTextSessionPage } from "./ProductionTextSessionPage";
import { ProductionSessionOperationsPage } from "./ProductionSessionOperationsPage";
import { ProductionSessionTranscriptPage } from "./ProductionSessionTranscriptPage";

const sessionId = "55555555-5555-4555-8555-555555555555";

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

function participantSnapshot(overrides: Record<string, unknown> = {}) {
  return {
    schema_version: "v1",
    projection_kind: "participant",
    session_id: sessionId,
    lifecycle_state: "active",
    session_version: 2,
    last_confirmed_sequence: "4",
    authoritative_observed_at: "2026-09-03T00:00:00Z",
    permitted_actions: ["send_message", "complete_session", "reconcile"],
    recovery_category: "none",
    agent: { display_name: "Assessment Agent" },
    timing: { policy: "disabled", remaining_seconds: null, warning_code: "none" },
    bound_submission: { summary: "Bound Submission", item_count: 1 },
    transcript: {
      items: [
        {
          item_id: "msg.participant1",
          author: "participant",
          status: "accepted",
          content: "Hello examiner",
          sequence_start: "1",
          sequence_end: "1",
        },
      ],
      older_available: false,
    },
    activity: { work_state: "idle" },
    ...overrides,
  };
}

class MockEventSource {
  onopen: (() => void) | null = null;
  onerror: (() => void) | null = null;
  onmessage: ((event: { data: string }) => void) | null = null;
  close() {}
  constructor() {
    queueMicrotask(() => this.onopen?.());
  }
}

function stubFetch(handler: (url: string, init?: RequestInit) => ReturnType<typeof jsonResponse>) {
  vi.stubGlobal("EventSource", MockEventSource);
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
        relationship: "participant",
        navigation: [{ destination_id: "my-work", is_available: true }],
        permitted_actions: [],
      });
    }
    return handler(url, init);
  });
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

function renderAt(path: string, page: ReactNode) {
  return render(
    <FlexQueryProvider>
      <ProductionApiProvider>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/sessions/:sessionId" element={page} />
            <Route path="/sessions/:sessionId/operations" element={page} />
            <Route path="/sessions/:sessionId/transcript" element={page} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>
    </FlexQueryProvider>,
  );
}

describe("hosted Session pages", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("restores the Participant transcript and send control from the snapshot", async () => {
    stubFetch((url) => {
      if (url.includes(`/v1/sessions/${sessionId}`) && !url.includes("/commands")) {
        return jsonResponse(participantSnapshot());
      }
      return jsonResponse({ error: "unexpected" }, 500);
    });

    renderAt(`/sessions/${sessionId}`, <ProductionTextSessionPage />);

    expect(await screen.findByText("Hello examiner")).toBeVisible();
    expect(screen.getByRole("textbox", { name: "Message" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Send" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Complete" })).toBeVisible();
  });

  it("conceals a denied Session without offering the composer", async () => {
    stubFetch((url) => {
      if (url.includes(`/v1/sessions/${sessionId}`)) {
        return jsonResponse({ error: { code: "session.denied" } }, 404);
      }
      return jsonResponse({ error: "unexpected" }, 500);
    });

    renderAt(`/sessions/${sessionId}`, <ProductionTextSessionPage />);

    expect(await screen.findByText("This Session cannot be opened with the current access.")).toBeVisible();
    expect(screen.queryByRole("textbox", { name: "Message" })).toBeNull();
  });

  it("closes the composer while the Session is paused", async () => {
    stubFetch((url) => {
      if (url.includes(`/v1/sessions/${sessionId}`)) {
        return jsonResponse(participantSnapshot({
          lifecycle_state: "paused",
          permitted_actions: ["reconcile", "return_to_my_work"],
        }));
      }
      return jsonResponse({ error: "unexpected" }, 500);
    });

    renderAt(`/sessions/${sessionId}`, <ProductionTextSessionPage />);

    expect(await screen.findByText("This Session is paused. Sending is closed until an administrator resumes it.")).toBeVisible();
    expect(screen.queryByRole("textbox", { name: "Message" })).toBeNull();
  });

  it("sends a Participant message through the hosted command contract", async () => {
    const fetchMock = stubFetch((url, init) => {
      if (url.includes("/commands")) {
        expect(init?.method).toBe("POST");
        const raw = init?.body;
        expect(typeof raw).toBe("string");
        const body = JSON.parse(raw as string);
        expect(body.command_type).toBe("session.message.send.v1");
        expect(body.session_locator.session_id).toBe(sessionId);
        expect(body.payload.message_text).toBe("Next answer");
        return jsonResponse({
          schema_version: "v1",
          succeeded: true,
          outcome_category: "accepted",
          outcome_code: "accepted",
          command_id: body.command_id,
          command_type: body.command_type,
          session_id: sessionId,
          permitted_recovery_action: "none",
          permitted_actions: ["send_message"],
        });
      }
      if (url.includes(`/v1/sessions/${sessionId}`)) {
        return jsonResponse(participantSnapshot());
      }
      return jsonResponse({ error: "unexpected" }, 500);
    });

    renderAt(`/sessions/${sessionId}`, <ProductionTextSessionPage />);
    const composer = await screen.findByRole("textbox", { name: "Message" });
    fireEvent.change(composer, { target: { value: "Next answer" } });
    fireEvent.click(screen.getByRole("button", { name: "Send" }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining("/commands"),
        expect.objectContaining({ method: "POST" }),
      );
    });
  });

  it("keeps administrator operations free of transcript content", async () => {
    stubFetch((url) => {
      if (url.includes(`/v1/sessions/${sessionId}`)) {
        return jsonResponse({
          ...participantSnapshot({
            projection_kind: "administrator",
            permitted_actions: ["pause_session", "terminate_session"],
            transcript: undefined,
            bound_submission: undefined,
            agent: undefined,
          }),
        });
      }
      return jsonResponse({ error: "unexpected" }, 500);
    });

    renderAt(`/sessions/${sessionId}/operations`, <ProductionSessionOperationsPage />);

    expect(await screen.findByRole("button", { name: "Pause" })).toBeVisible();
    expect(screen.getByRole("button", { name: "Terminate" })).toBeVisible();
    expect(screen.queryByLabelText("Historical transcript")).toBeNull();
    expect(screen.getByText(/Transcript and Submission content are not loaded/)).toBeVisible();
  });

  it("renders a read-only historical transcript without live controls", async () => {
    stubFetch((url) => {
      if (url.includes(`/v1/sessions/${sessionId}`)) {
        return jsonResponse(participantSnapshot({
          projection_kind: "historical",
          lifecycle_state: "completed",
          permitted_actions: ["view_transcript"],
        }));
      }
      return jsonResponse({ error: "unexpected" }, 500);
    });

    renderAt(`/sessions/${sessionId}/transcript`, <ProductionSessionTranscriptPage />);

    expect(await screen.findByText("Hello examiner")).toBeVisible();
    expect(screen.getByLabelText("Historical transcript")).toBeVisible();
    expect(screen.queryByRole("button", { name: "Send" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Pause" })).toBeNull();
  });
});
