import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { BrowserApiProvider } from "../api/browser-api";
import type {
  ActorContextV1,
  NavigationProjectionV1,
  SessionProjectionV1,
} from "../api/browser-contracts";
import { SessionPage } from "./SessionPage";

const actorContext: ActorContextV1 = {
  schema_version: "v1",
  actor_id: "actor.synthetic.participant",
  display_name: "Synthetic Participant",
  organization_id: "org.synthetic.demo",
  organization_name: "Synthetic Demo Organization",
  capabilities: [],
  actor_stage: "participant",
  is_synthetic: true,
};

const navigation: NavigationProjectionV1 = {
  schema_version: "v1",
  destinations: [
    {
      destination_id: "my-work",
      label: "My work",
      route: "/my-work",
      tier: "p0",
      is_available: true,
    },
  ],
};

const activeSession: SessionProjectionV1 = {
  schema_version: "v1",
  session_id: "sess.synthetic.0001",
  lifecycle_state: "active",
  remaining_time: "12 minutes",
  transcript: [
    {
      item_id: "msg.synthetic.participant.1",
      role: "participant",
      content: "Ready for the Session.",
      status: "accepted",
      occurred_at: "2026-08-16T00:00:00Z",
    },
  ],
  permitted_actions: [
    {
      action_id: "send_message",
      label: "Send message",
      is_destructive: false,
    },
  ],
  bound_submission_summary: "Bound submission v1",
  session_version: 3,
  last_sequence: "2",
};

class MockEventSource {
  static CONNECTING = 0;
  static OPEN = 1;
  static CLOSED = 2;
  static instances: MockEventSource[] = [];
  url: string;
  onmessage: ((event: MessageEvent<string>) => void) | null = null;
  onerror: ((event: Event) => void) | null = null;
  onopen: (() => void) | null = null;
  readyState = MockEventSource.OPEN;

  constructor(url: string) {
    this.url = url;
    MockEventSource.instances.push(this);
  }

  close(): void {
    this.readyState = MockEventSource.CLOSED;
  }

  emit(payload: unknown, lastEventId = "1"): void {
    this.onmessage?.({
      data: JSON.stringify(payload),
      lastEventId,
    } as MessageEvent<string>);
  }
}

function mockFetch(getSession: () => SessionProjectionV1 = () => activeSession, status = 200) {
  return vi.fn((input: RequestInfo | URL) => {
    const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;

    if (url.includes("/browser/actor-context")) {
      return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(actorContext) });
    }

    if (url.includes("/browser/navigation")) {
      return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(navigation) });
    }

    if (url.includes("/browser/sessions/")) {
      if (status === 404) {
        return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
      }
      if (status === 403) {
        return Promise.resolve({
          ok: false,
          status: 403,
          json: () => Promise.resolve({ safe_message: "Access denied" }),
        });
      }
      return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(getSession()) });
    }

    return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
  });
}

function renderSession() {
  return render(
    <MemoryRouter initialEntries={["/sessions/sess.synthetic.0001"]}>
      <BrowserApiProvider>
        <Routes>
          <Route path="/sessions/:sessionId" element={<SessionPage />} />
        </Routes>
      </BrowserApiProvider>
    </MemoryRouter>,
  );
}

async function openSessionStream(minimumInstances = 1): Promise<MockEventSource> {
  await waitFor(() => {
    expect(MockEventSource.instances.length).toBeGreaterThanOrEqual(minimumInstances);
  });
  const source = MockEventSource.instances[MockEventSource.instances.length - 1];
  act(() => {
    source.onopen?.();
  });
  return source;
}

describe("SessionPage Decision presentation", () => {
  beforeEach(() => {
    MockEventSource.instances = [];
    vi.stubGlobal("EventSource", MockEventSource);
    vi.stubGlobal("fetch", mockFetch());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("stops Agent work on no-action without a synthetic Agent transcript item or internal labels", async () => {
    renderSession();

    expect(await screen.findByRole("heading", { name: /^session$/i })).toBeInTheDocument();
    const composer = await screen.findByLabelText(/your message/i);
    composer.focus();

    const source = await openSessionStream();
    act(() => {
      source.emit({
      schema_version: "v1",
      event_type: "session.agent.work.v1",
      session_id: "sess.synthetic.0001",
      session_sequence: "3",
      occurred_at: "2026-08-16T00:00:01Z",
      payload: {
        summary: "The Agent is preparing a response.",
        turn_id: "turn.synthetic.0001",
        work_state: "queued",
      },
      }, "3");
    });

    expect(await screen.findByText(/^processing$/i)).toBeInTheDocument();
    expect(screen.getAllByText(/the agent is preparing a response/i).length).toBeGreaterThan(0);

    act(() => {
      source.emit({
      schema_version: "v1",
      event_type: "session.agent.work.v1",
      session_id: "sess.synthetic.0001",
      session_sequence: "4",
      occurred_at: "2026-08-16T00:00:02Z",
      payload: {
        summary: "The Agent is preparing a response.",
        turn_id: "turn.synthetic.0001",
        work_state: "working",
      },
    }, "4");
    });

    act(() => {
      source.emit({
      schema_version: "v1",
      event_type: "session.agent.work.v1",
      session_id: "sess.synthetic.0001",
      session_sequence: "5",
      occurred_at: "2026-08-16T00:00:03Z",
      payload: {
        summary: "Turn resolved without Agent reply.",
        turn_id: "turn.synthetic.0001",
        work_state: "resolved",
        resolution_category: "no_action",
        show_persistent_turn_status: true,
      },
    }, "5");
    });

    expect(await screen.findByText(/no agent reply for this turn/i)).toBeInTheDocument();
    expect(screen.queryByText(/the agent is preparing a response/i)).not.toBeInTheDocument();
    expect(screen.getByText(/^ready$/i)).toBeInTheDocument();
    expect(screen.getByText(/ready for the session/i)).toBeInTheDocument();
    expect(within(screen.getByRole("log")).queryByText(/^agent$/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/no_action/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/work_state/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/resolution_category/i)).not.toBeInTheDocument();
    expect(screen.getByRole("status", { name: /session updates/i })).toHaveTextContent(
      /turn resolved without agent reply/i,
    );
    expect(document.activeElement).toBe(composer);
  });

  it("renders timer-triggered Agent work without inventing a Participant message", async () => {
    vi.stubGlobal("fetch", mockFetch(() => ({ ...activeSession, transcript: [] })));
    renderSession();

    await openSessionStream();

    act(() => {
      MockEventSource.instances[0].emit({
        schema_version: "v1",
        event_type: "session.agent.fragment.v1",
        session_id: "sess.synthetic.0001",
        session_sequence: "8",
        occurred_at: "2026-08-16T00:00:08Z",
        payload: {
          summary: "Agent response fragment.",
          agent_message_id: "msg.synthetic.agent.1",
          text_delta: "Checking in on your progress. ",
          turn_id: "turn.synthetic.0002",
        },
      }, "8");
    });

    expect(await screen.findByText(/checking in on your progress/i)).toBeInTheDocument();
    expect(screen.getByText(/^agent$/i)).toBeInTheDocument();
    expect(screen.queryByText(/^you$/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/timer/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/revision/i)).not.toBeInTheDocument();
  });

  it("does not label a policy-rejected Decision as no-action or provider failure", async () => {
    renderSession();
    const source = await openSessionStream();

    act(() => {
      source.emit({
        schema_version: "v1",
        event_type: "session.agent.work.v1",
        session_id: "sess.synthetic.0001",
        session_sequence: "9",
        occurred_at: "2026-08-16T00:00:09Z",
        payload: {
          summary: "This turn could not be completed.",
          turn_id: "turn.synthetic.0003",
          work_state: "resolved",
          resolution_category: "suppressed_failure",
          show_persistent_turn_status: false,
        },
      }, "9");
    });

    expect(await screen.findAllByText(/this turn could not be completed/i)).not.toHaveLength(0);
    expect(screen.queryByText(/no agent reply for this turn/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/provider/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/no_action/i)).not.toBeInTheDocument();
  });

  it("disables sending while EventSource is reconnecting and reconciles before enabling again", async () => {
    vi.stubGlobal(
      "fetch",
      mockFetch(() => ({
        ...activeSession,
        permitted_actions: [
          ...activeSession.permitted_actions,
          { action_id: "complete_session", label: "Complete session", is_destructive: true },
        ],
      })),
    );
    renderSession();
    const composer = await screen.findByLabelText(/your message/i);
    fireEvent.change(composer, { target: { value: "Draft that must be kept." } });
    composer.focus();
    const source = await openSessionStream();

    expect(screen.getByRole("button", { name: /send message/i })).toBeEnabled();
    expect(screen.getByRole("button", { name: /complete session/i })).toBeEnabled();

    source.readyState = MockEventSource.CONNECTING;
    act(() => {
      source.onerror?.(new Event("error"));
    });

    expect(await screen.findByRole("button", { name: /send message/i })).toBeDisabled();
    expect(screen.getByRole("button", { name: /complete session/i })).toBeDisabled();
    expect(screen.getAllByText(/your session and time have not been paused/i).length).toBeGreaterThan(0);
    expect(screen.getByText(/draft is kept locally/i)).toBeInTheDocument();
    expect(composer).not.toBeDisabled();
    expect(document.activeElement).toBe(composer);

    act(() => {
      source.readyState = MockEventSource.OPEN;
      source.onopen?.();
    });

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /send message/i })).toBeEnabled();
    });
    expect(screen.getByRole("button", { name: /complete session/i })).toBeEnabled();
    expect(document.activeElement).toBe(composer);
  });

  it("treats a closed EventSource as offline until an explicit retry reconnects", async () => {
    renderSession();
    const composer = await screen.findByLabelText(/your message/i);
    fireEvent.change(composer, { target: { value: "Draft that must be kept." } });
    const source = await openSessionStream();

    source.readyState = MockEventSource.CLOSED;
    act(() => {
      source.onerror?.(new Event("error"));
    });

    expect(await screen.findByRole("button", { name: /try reconnecting/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /send message/i })).toBeDisabled();
    expect(composer).not.toBeDisabled();

    act(() => {
      screen.getByRole("button", { name: /try reconnecting/i }).click();
    });

    const retried = await openSessionStream(2);
    expect(retried).not.toBe(source);
    await waitFor(() => {
      expect(screen.getByRole("button", { name: /send message/i })).toBeEnabled();
    });
    expect(composer).toHaveValue("Draft that must be kept.");
  });

  it("reconciles permitted actions after a Session state-changed SSE event", async () => {
    let current: SessionProjectionV1 = activeSession;
    vi.stubGlobal("fetch", mockFetch(() => current));
    renderSession();
    const source = await openSessionStream();

    current = {
      ...activeSession,
      lifecycle_state: "paused",
      remaining_time: "12 minutes (paused)",
      permitted_actions: [],
      session_version: 4,
    };

    act(() => {
      source.emit({
        schema_version: "v1",
        event_type: "session.state.changed.v1",
        session_id: "sess.synthetic.0001",
        session_sequence: "19",
        occurred_at: "2026-08-16T00:00:19Z",
        payload: { summary: "Session paused." },
      }, "19");
    });

    expect(await screen.findByText(/^session paused$/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /send message/i })).not.toBeInTheDocument();
    expect(screen.getByText(/sending is unavailable until the session resumes/i)).toBeInTheDocument();
  });

  it("reconciles the Session projection after a terminal SSE event and removes live commands", async () => {
    let current: SessionProjectionV1 = activeSession;
    vi.stubGlobal("fetch", mockFetch(() => current));
    renderSession();
    const source = await openSessionStream();

    current = {
      ...activeSession,
      lifecycle_state: "completed",
      remaining_time: null,
      permitted_actions: [],
      session_version: 4,
    };

    act(() => {
      source.emit({
        schema_version: "v1",
        event_type: "session.terminal.v1",
        session_id: "sess.synthetic.0001",
        session_sequence: "20",
        occurred_at: "2026-08-16T00:00:20Z",
        payload: { summary: "Session completed." },
      }, "20");
    });

    expect(await screen.findByText(/^session completed$/i)).toBeInTheDocument();
    expect(screen.getByText(/^dormant$/i)).toBeInTheDocument();
    expect(screen.queryByText(/^active$/i)).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /send message/i })).not.toBeInTheDocument();
    expect(screen.queryByText(/the agent is preparing a response/i)).not.toBeInTheDocument();
    expect(screen.getByRole("alert")).toHaveTextContent(/session completed/i);
  });
});
