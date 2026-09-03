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
    headers: { get: () => "application/json" },
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
    authoritative_observed_at: new Date().toISOString(),
    permitted_actions: ["send_message", "complete_session", "reconcile"],
    recovery_category: "none",
    agent: { display_name: "Assessment Agent" },
    timing: {
      policy: "active_duration",
      remaining_seconds: 2400,
      warning_code: "none",
      budget_seconds: 2700,
    },
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
        display_name: "Demo Participant",
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
    expect(screen.getByRole("textbox", { name: "Compose reply" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Transmit" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Submit Session" })).toBeVisible();
    expect(screen.getByRole("heading", { name: "Time Remaining" })).toBeVisible();
    expect(screen.getByRole("timer")).toHaveTextContent("00:40:00");
    expect(screen.getByText("55555555…555555")).toBeVisible();
    expect(screen.getByText("Demo Participant")).toBeVisible();
    expect(document.querySelectorAll(".stage-bars span")).toHaveLength(2);
    expect(document.querySelector(".turn")).toHaveClass("is-active");
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
    expect(screen.queryByRole("textbox", { name: "Compose reply" })).toBeNull();
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
    expect(screen.queryByRole("textbox", { name: "Compose reply" })).toBeNull();
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
    const composer = await screen.findByRole("textbox", { name: "Compose reply" });
    fireEvent.change(composer, { target: { value: "Next answer" } });
    fireEvent.keyDown(composer, { key: "Enter" });

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining("/commands"),
        expect.objectContaining({ method: "POST" }),
      );
    });
  });

  it("does not send when Shift+Enter inserts a composer line break", async () => {
    const fetchMock = stubFetch((url) => {
      if (url.includes(`/v1/sessions/${sessionId}`)) {
        return jsonResponse(participantSnapshot());
      }
      return jsonResponse({ error: "unexpected" }, 500);
    });

    renderAt(`/sessions/${sessionId}`, <ProductionTextSessionPage />);
    const composer = await screen.findByRole("textbox", { name: "Compose reply" });
    fireEvent.change(composer, { target: { value: "Keep drafting" } });
    fireEvent.keyDown(composer, { key: "Enter", shiftKey: true });

    expect(fetchMock).not.toHaveBeenCalledWith(
      expect.stringContaining("/commands"),
      expect.anything(),
    );
  });

  it("holds Transmit while the Agent turn is still working on this Session", async () => {
    stubFetch((url) => {
      if (url.includes(`/v1/sessions/${sessionId}`)) {
        return jsonResponse(participantSnapshot({
          activity: { work_state: "working" },
        }));
      }
      return jsonResponse({ error: "unexpected" }, 500);
    });

    renderAt(`/sessions/${sessionId}`, <ProductionTextSessionPage />);
    const composer = await screen.findByRole("textbox", { name: "Compose reply" });
    fireEvent.change(composer, { target: { value: "too soon" } });

    expect(composer).toBeEnabled();
    expect(screen.getByRole("button", { name: "Transmit" })).toBeDisabled();
    expect(screen.getByText("Considering your reply…")).toBeVisible();
  });

  it("retries a stale-version send on the same Session after refreshing the snapshot", async () => {
    let commandCalls = 0;
    const fetchMock = stubFetch((url, init) => {
      if (url.includes("/commands")) {
        commandCalls += 1;
        const body = JSON.parse(String(init?.body));
        if (commandCalls === 1) {
          expect(body.expected_session_version).toBe(2);
          return jsonResponse({
            schema_version: "v1",
            succeeded: false,
            outcome_category: "conflict",
            outcome_code: "trigger.admission.stale.version",
            command_id: body.command_id,
            command_type: "session.message.send.v1",
            session_id: sessionId,
            permitted_recovery_action: "reconcile_snapshot",
            permitted_actions: ["send_message", "reconcile"],
          }, 409);
        }

        expect(body.expected_session_version).toBe(4);
        expect(body.payload.message_text).toBe("Stale send");
        expect(body.session_locator.session_id).toBe(sessionId);
        return jsonResponse({
          schema_version: "v1",
          succeeded: true,
          outcome_category: "accepted",
          outcome_code: "accepted",
          command_id: body.command_id,
          command_type: body.command_type,
          session_id: sessionId,
          session_version: 5,
          permitted_recovery_action: "none",
          permitted_actions: ["send_message"],
        });
      }
      if (url.includes(`/v1/sessions/${sessionId}`)) {
        return jsonResponse(participantSnapshot({ session_version: commandCalls === 0 ? 2 : 4 }));
      }
      return jsonResponse({ error: "unexpected" }, 500);
    });

    renderAt(`/sessions/${sessionId}`, <ProductionTextSessionPage />);
    const composer = await screen.findByRole("textbox", { name: "Compose reply" });
    fireEvent.change(composer, { target: { value: "Stale send" } });
    fireEvent.click(screen.getByRole("button", { name: "Transmit" }));

    await waitFor(() => {
      expect(commandCalls).toBe(2);
    });
    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining("/commands"),
      expect.objectContaining({ method: "POST" }),
    );
    expect(screen.queryByText("This Session record was updated. Send again.")).toBeNull();
    expect(screen.queryByText("The command outcome is uncertain. Reconcile before sending again.")).toBeNull();
    await waitFor(() => {
      expect(screen.getByRole("textbox", { name: "Compose reply" })).toHaveValue("");
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
    expect(screen.queryByRole("button", { name: "Transmit" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Pause" })).toBeNull();
  });

  it("closes send at remaining zero and reconciles to the time-ended confirmation", async () => {
    let expired = false;
    const fetchMock = stubFetch((url, init) => {
      if (url.includes("/commands")) {
        const body = JSON.parse(String(init?.body));
        expect(body.command_type).toBe("session.reconcile.v1");
        expired = true;
        return jsonResponse({
          schema_version: "v1",
          succeeded: true,
          outcome_category: "accepted",
          outcome_code: "session.reconcile.succeeded",
          command_id: body.command_id,
          command_type: body.command_type,
          session_id: sessionId,
          permitted_recovery_action: "none",
          permitted_actions: ["view_transcript", "return_to_my_work"],
          session_version: 8,
        });
      }
      if (url.includes(`/v1/sessions/${sessionId}`)) {
        return jsonResponse(participantSnapshot(expired
          ? {
              lifecycle_state: "completed",
              session_version: 8,
              permitted_actions: ["view_transcript", "return_to_my_work"],
              timing: {
                policy: "active_duration",
                remaining_seconds: 0,
                warning_code: "none",
                budget_seconds: 2700,
              },
            }
          : {
              permitted_actions: ["reconcile", "return_to_my_work"],
              timing: {
                policy: "active_duration",
                remaining_seconds: 0,
                warning_code: "none",
                budget_seconds: 2700,
              },
            }));
      }
      return jsonResponse({ error: "unexpected" }, 500);
    });

    renderAt(`/sessions/${sessionId}`, <ProductionTextSessionPage />);

    expect(await screen.findByRole("heading", { name: "Checking Session end" })).toBeVisible();
    expect(screen.queryByRole("textbox", { name: "Compose reply" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Submit Session" })).toBeNull();
    expect(screen.getByText("2 of 2")).toBeVisible();

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining("/commands"),
        expect.objectContaining({ method: "POST" }),
      );
    });
    expect(await screen.findByRole("heading", { name: "Time ended. Session completed" })).toBeVisible();
    expect(screen.getByText(/Only content accepted before the Session cutoff/)).toBeVisible();
    expect(screen.getAllByText(/score or Result/i).length).toBeGreaterThan(0);
    expect(screen.getByRole("link", { name: "Return to Assignment" })).toBeVisible();
  });

  it("shows completion confirmation and return without a score", async () => {
    stubFetch((url) => {
      if (url.includes(`/v1/sessions/${sessionId}`)) {
        return jsonResponse(participantSnapshot({
          lifecycle_state: "completed",
          permitted_actions: ["view_transcript", "return_to_my_work"],
        }));
      }
      return jsonResponse({ error: "unexpected" }, 500);
    });

    renderAt(`/sessions/${sessionId}`, <ProductionTextSessionPage />);

    expect(await screen.findByRole("heading", { name: "Session Complete" })).toBeVisible();
    expect(screen.getByRole("link", { name: "Return to Assignment" })).toBeVisible();
    expect(document.getElementById("completeToAssignment")).toBeTruthy();
    expect(screen.getByText(/Sealed/)).toBeVisible();
    expect(document.querySelector(".complete-plate")).toBeTruthy();
    expect(screen.queryByRole("textbox", { name: "Compose reply" })).toBeNull();
    expect(document.querySelector(".turn.is-active")).toBeNull();
  });

  it("seals a stuck completing Session with the complete command", async () => {
    const fetchMock = stubFetch((url, init) => {
      if (url.includes("/commands")) {
        expect(init?.method).toBe("POST");
        const body = JSON.parse(init?.body as string);
        expect(body.command_type).toBe("session.complete.v1");
        return jsonResponse({
          schema_version: "v1",
          succeeded: true,
          outcome_category: "accepted",
          outcome_code: "accepted",
          command_id: body.command_id,
          command_type: body.command_type,
          session_id: sessionId,
          permitted_recovery_action: "none",
          permitted_actions: ["view_transcript"],
        });
      }
      if (url.includes(`/v1/sessions/${sessionId}`)) {
        return jsonResponse(participantSnapshot({
          lifecycle_state: "completing",
          permitted_actions: ["complete_session", "reconcile", "return_to_my_work"],
        }));
      }
      return jsonResponse({ error: "unexpected" }, 500);
    });

    renderAt(`/sessions/${sessionId}`, <ProductionTextSessionPage />);
    expect(await screen.findByRole("heading", { name: "Session completing" })).toBeVisible();
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining("/commands"),
        expect.objectContaining({ method: "POST" }),
      );
    });
  });
});
