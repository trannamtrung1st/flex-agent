import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, RouterProvider, Routes, createMemoryRouter } from "react-router-dom";
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
  readyState = MockEventSource.CONNECTING;

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

function mockFetch(getSession: () => SessionProjectionV1 | Promise<SessionProjectionV1> = () => activeSession, status = 200) {
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
    source.readyState = MockEventSource.OPEN;
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
    expect(screen.getAllByText(/session time may continue/i).length).toBeGreaterThan(0);
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
    expect(MockEventSource.instances).toHaveLength(1);
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

  it("does not re-enable commands when a reconcile GET succeeds after the stream has closed", async () => {
    const pendingSession: { resolve: (value: SessionProjectionV1) => void } = {
      resolve: () => undefined,
    };
    let sessionLoads = 0;
    vi.stubGlobal(
      "fetch",
      mockFetch(() => {
        sessionLoads += 1;
        if (sessionLoads === 1) {
          return activeSession;
        }
        return new Promise<SessionProjectionV1>((resolve) => {
          pendingSession.resolve = resolve;
        });
      }),
    );

    renderSession();
    const composer = await screen.findByLabelText(/your message/i);
    fireEvent.change(composer, { target: { value: "Draft that must be kept." } });
    const source = await openSessionStream();

    source.readyState = MockEventSource.CONNECTING;
    act(() => {
      source.onerror?.(new Event("error"));
    });
    act(() => {
      source.readyState = MockEventSource.OPEN;
      source.onopen?.();
    });

    await waitFor(() => {
      expect(sessionLoads).toBeGreaterThanOrEqual(2);
    });

    source.readyState = MockEventSource.CLOSED;
    act(() => {
      source.onerror?.(new Event("error"));
    });

    await act(async () => {
      pendingSession.resolve(activeSession);
      await Promise.resolve();
    });

    expect(await screen.findByRole("button", { name: /try reconnecting/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /send message/i })).toBeDisabled();
    expect(composer).toHaveValue("Draft that must be kept.");
  });

  it("does not let a slower older projection overwrite a newer Session", async () => {
    const queued: Array<(value: SessionProjectionV1) => void> = [];
    let sessionLoads = 0;
    vi.stubGlobal(
      "fetch",
      mockFetch(() => {
        sessionLoads += 1;
        if (sessionLoads === 1) {
          return activeSession;
        }
        return new Promise<SessionProjectionV1>((resolve) => {
          queued.push(resolve);
        });
      }),
    );

    renderSession();
    const source = await openSessionStream();

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

    await waitFor(() => {
      expect(queued).toHaveLength(2);
    });

    await act(async () => {
      queued[1]({
        ...activeSession,
        lifecycle_state: "completed",
        remaining_time: null,
        permitted_actions: [],
        session_version: 5,
        last_sequence: "20",
      });
      await Promise.resolve();
    });

    expect(await screen.findByText(/^session completed$/i)).toBeInTheDocument();

    await act(async () => {
      queued[0]({
        ...activeSession,
        lifecycle_state: "active",
        permitted_actions: activeSession.permitted_actions,
        session_version: 4,
        last_sequence: "19",
      });
      await Promise.resolve();
    });

    expect(screen.getByText(/^session completed$/i)).toBeInTheDocument();
    expect(screen.queryByText(/^active$/i)).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /send message/i })).not.toBeInTheDocument();
  });

  it("keeps the Session after a transient reconcile failure", async () => {
    let sessionLoads = 0;
    vi.stubGlobal(
      "fetch",
      vi.fn((input: RequestInfo | URL) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
        if (url.includes("/browser/actor-context")) {
          return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(actorContext) });
        }
        if (url.includes("/browser/navigation")) {
          return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(navigation) });
        }
        if (url.includes("/browser/sessions/")) {
          sessionLoads += 1;
          if (sessionLoads === 1) {
            return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(activeSession) });
          }
          return Promise.resolve({ ok: false, status: 500, json: () => Promise.resolve({}) });
        }
        return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
      }),
    );

    renderSession();
    const composer = await screen.findByLabelText(/your message/i);
    fireEvent.change(composer, { target: { value: "Draft that must be kept." } });
    const source = await openSessionStream();

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

    expect(await screen.findByRole("button", { name: /try again/i })).toBeInTheDocument();
    expect(screen.getAllByText(/could not update session/i).length).toBeGreaterThan(0);
    expect(screen.getByText(/ready for the session/i)).toBeInTheDocument();
    expect(composer).toHaveValue("Draft that must be kept.");
    expect(screen.queryByText(/session unavailable/i)).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: /send message/i })).toBeDisabled();
  });

  it("retries the stream instead of staying reconciling when Try again runs while disconnected", async () => {
    let sessionLoads = 0;
    vi.stubGlobal(
      "fetch",
      vi.fn((input: RequestInfo | URL) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
        if (url.includes("/browser/actor-context")) {
          return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(actorContext) });
        }
        if (url.includes("/browser/navigation")) {
          return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(navigation) });
        }
        if (url.includes("/browser/sessions/")) {
          sessionLoads += 1;
          if (sessionLoads === 1) {
            return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(activeSession) });
          }
          if (sessionLoads === 2) {
            return Promise.resolve({ ok: false, status: 500, json: () => Promise.resolve({}) });
          }
          return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(activeSession) });
        }
        return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
      }),
    );

    renderSession();
    const composer = await screen.findByLabelText(/your message/i);
    fireEvent.change(composer, { target: { value: "Draft that must be kept." } });
    const source = await openSessionStream();

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

    expect(await screen.findByRole("button", { name: /try again/i })).toBeInTheDocument();

    source.readyState = MockEventSource.CLOSED;
    act(() => {
      source.onerror?.(new Event("error"));
    });

    act(() => {
      screen.getByRole("button", { name: /try again/i }).click();
    });

    const retried = await openSessionStream(2);
    expect(retried).not.toBe(source);
    await waitFor(() => {
      expect(screen.getByRole("button", { name: /send message/i })).toBeEnabled();
    });
    expect(composer).toHaveValue("Draft that must be kept.");
  });

  it("shows Incomplete when a streaming Agent message is cut off by a terminal event", async () => {
    let current: SessionProjectionV1 = activeSession;
    vi.stubGlobal("fetch", mockFetch(() => current));
    renderSession();
    const source = await openSessionStream();

    act(() => {
      source.emit({
        schema_version: "v1",
        event_type: "session.agent.fragment.v1",
        session_id: "sess.synthetic.0001",
        session_sequence: "8",
        occurred_at: "2026-08-16T00:00:08Z",
        payload: {
          summary: "Agent response fragment.",
          agent_message_id: "msg.synthetic.agent.cutoff",
          text_delta: "Visible prefix. ",
          turn_id: "turn.synthetic.0009",
        },
      }, "8");
    });

    expect(await screen.findByText(/visible prefix/i)).toBeInTheDocument();
    expect(screen.getByText(/^agent is responding$/i)).toBeInTheDocument();

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
        session_sequence: "9",
        occurred_at: "2026-08-16T00:00:09Z",
        payload: { summary: "Session completed." },
      }, "9");
    });

    expect(await screen.findByText(/^incomplete$/i)).toBeInTheDocument();
    expect(screen.queryByText(/^agent is responding$/i)).not.toBeInTheDocument();
    expect(screen.getByText(/visible prefix/i)).toBeInTheDocument();
  });

  it("keeps Send disabled until the EventSource reports OPEN", async () => {
    renderSession();
    const composer = await screen.findByLabelText(/your message/i);
    fireEvent.change(composer, { target: { value: "Draft that must be kept." } });

    expect(screen.getByRole("button", { name: /send message/i })).toBeDisabled();
    await waitFor(() => {
      expect(MockEventSource.instances[0]?.readyState).toBe(MockEventSource.CONNECTING);
    });

    await openSessionStream();
    expect(screen.getByRole("button", { name: /send message/i })).toBeEnabled();
  });

  it("does not commit a late projection from Session A onto Session B", async () => {
    const sessionB: SessionProjectionV1 = {
      ...activeSession,
      session_id: "sess.synthetic.0002",
      transcript: [
        {
          item_id: "msg.synthetic.participant.b",
          role: "participant",
          content: "Session B only.",
          status: "accepted",
          occurred_at: "2026-08-16T00:00:00Z",
        },
      ],
    };
    const pendingA: { resolve: (value: SessionProjectionV1) => void } = {
      resolve: () => undefined,
    };

    vi.stubGlobal(
      "fetch",
      vi.fn((input: RequestInfo | URL) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
        if (url.includes("/browser/actor-context")) {
          return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(actorContext) });
        }
        if (url.includes("/browser/navigation")) {
          return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(navigation) });
        }
        if (url.includes("/browser/sessions/sess.synthetic.0001")) {
          return Promise.resolve({
            ok: true,
            status: 200,
            json: () =>
              new Promise<SessionProjectionV1>((resolve) => {
                pendingA.resolve = resolve;
              }),
          });
        }
        if (url.includes("/browser/sessions/sess.synthetic.0002")) {
          return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(sessionB) });
        }
        return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
      }),
    );

    const router = createMemoryRouter(
      [{ path: "/sessions/:sessionId", element: <SessionPage /> }],
      { initialEntries: ["/sessions/sess.synthetic.0001"] },
    );
    render(
      <BrowserApiProvider>
        <RouterProvider router={router} />
      </BrowserApiProvider>,
    );

    expect(await screen.findByText(/loading session/i)).toBeInTheDocument();

    await act(async () => {
      await router.navigate("/sessions/sess.synthetic.0002");
    });

    expect(await screen.findByText(/session b only/i)).toBeInTheDocument();

    await act(async () => {
      pendingA.resolve(activeSession);
      await Promise.resolve();
    });

    expect(screen.getByText(/session b only/i)).toBeInTheDocument();
    expect(screen.queryByText(/ready for the session/i)).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /send message/i })).toBeInTheDocument();
  });

  it("exposes retry when the latest projection response is older than the committed Session", async () => {
    let sessionLoads = 0;
    vi.stubGlobal(
      "fetch",
      mockFetch(() => {
        sessionLoads += 1;
        if (sessionLoads === 1) {
          return activeSession;
        }
        return {
          ...activeSession,
          session_version: 2,
          last_sequence: "1",
        };
      }),
    );

    renderSession();
    const source = await openSessionStream();

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

    expect(await screen.findByRole("button", { name: /try again/i })).toBeInTheDocument();
    expect(screen.getByText(/^active$/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /send message/i })).toBeDisabled();
    expect(screen.queryByText(/^session paused$/i)).not.toBeInTheDocument();
  });
});
