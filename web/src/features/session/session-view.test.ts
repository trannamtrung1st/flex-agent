import { emptySessionLiveView, sessionAgentTurnOpen, sessionCommandsBlocked, sessionLiveReducer, sessionPostSendReconciled } from "./session-view";
import type { SessionSnapshotV1 } from "../../contracts/v1";

const snapshot: SessionSnapshotV1 = {
  schema_version: "v1",
  projection_kind: "participant",
  session_id: "55555555-5555-4555-8555-555555555555",
  lifecycle_state: "active",
  session_version: 3,
  last_confirmed_sequence: "12",
  authoritative_observed_at: "2026-09-03T00:00:00Z",
  permitted_actions: ["send_message"],
  recovery_category: "none",
  transcript: { items: [], older_available: false },
};

describe("sessionLiveReducer", () => {
  it("replaces the live baseline from an authoritative snapshot", () => {
    const next = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    expect(next.snapshot?.session_version).toBe(3);
    expect(next.snapshot?.last_confirmed_sequence).toBe("12");
  });

  it("appends a newer hosted fragment without treating client time as authority", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const next = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.fragment.v1",
        session_id: snapshot.session_id,
        session_sequence: "13",
        session_version: 3,
        occurred_at: "2026-09-03T00:00:03Z",
        payload: {
          summary: "Durable Agent fragment.",
          agent_message_id: "amsg.synthetic.0001",
          text_delta: "Hello",
        },
      },
    });
    expect(next.snapshot?.last_confirmed_sequence).toBe("13");
    expect(next.snapshot?.transcript?.items[0]?.content).toBe("Hello");
  });

  it("ignores stale or duplicate sequences", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const next = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.fragment.v1",
        session_id: snapshot.session_id,
        session_sequence: "11",
        session_version: 3,
        occurred_at: "2026-09-03T00:00:03Z",
        payload: { summary: "Stale fragment.", text_delta: "No" },
      },
    });
    expect(next.snapshot?.last_confirmed_sequence).toBe("12");
    expect(next.snapshot?.transcript?.items).toHaveLength(0);
  });

  it("lets an authoritative unavailable snapshot erase stale local transcript content", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const streamed = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.fragment.v1",
        session_id: snapshot.session_id,
        session_sequence: "13",
        session_version: 3,
        occurred_at: "2026-09-03T00:00:03Z",
        payload: {
          summary: "Durable Agent fragment.",
          agent_message_id: "amsg.synthetic.0001",
          text_delta: "Hello examiner",
        },
      },
    });
    const next = sessionLiveReducer(streamed, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        last_confirmed_sequence: "14",
        transcript: {
          items: [
            {
              item_id: "amsg.synthetic.0001",
              author: "agent",
              status: "unavailable",
              content: null,
              sequence_start: "13",
              sequence_end: "14",
            },
          ],
          older_available: false,
        },
      },
    });

    expect(next.snapshot?.transcript?.items[0]?.content).toBeNull();
    expect(next.snapshot?.transcript?.items[0]?.status).toBe("unavailable");
  });

  it("keeps a richer local Agent prefix only while the server still authorizes that item", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const streamed = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.fragment.v1",
        session_id: snapshot.session_id,
        session_sequence: "13",
        session_version: 3,
        occurred_at: "2026-09-03T00:00:03Z",
        payload: {
          summary: "Durable Agent fragment.",
          agent_message_id: "amsg.synthetic.0001",
          text_delta: "Hello examiner",
        },
      },
    });
    const next = sessionLiveReducer(streamed, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        last_confirmed_sequence: "14",
        transcript: {
          items: [
            {
              item_id: "amsg.synthetic.0001",
              author: "agent",
              status: "streaming",
              content: "Hello",
              sequence_start: "13",
              sequence_end: "14",
            },
          ],
          older_available: false,
        },
      },
    });

    expect(next.snapshot?.transcript?.items[0]?.content).toBe("Hello examiner");
    expect(next.snapshot?.transcript?.items[0]?.status).toBe("streaming");
  });

  it("applies hosted timing updates to the snapshot chrono", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        timing: {
          policy: "active_duration",
          remaining_seconds: 1800,
          warning_code: "none",
          budget_seconds: 2700,
        },
      },
    });
    const next = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.timing.updated.v1",
        session_id: snapshot.session_id,
        session_sequence: "20",
        stream_cursor: "201",
        session_version: 6,
        occurred_at: "2026-09-03T00:15:00Z",
        payload: {
          summary: "Session timing updated.",
          lifecycle_state: "paused",
          remaining_seconds: 1200,
          warning_code: "approaching",
        },
      },
    });

    expect(next.snapshot?.timing?.remaining_seconds).toBe(1200);
    expect(next.snapshot?.timing?.warning_code).toBe("approaching");
    expect(next.snapshot?.timing?.pause_started_at).toBe("2026-09-03T00:15:00Z");
  });

  it("applies a durable hosted warning occurrence once by stream cursor", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        timing: {
          policy: "active_duration",
          remaining_seconds: 1000,
          warning_code: "none",
          budget_seconds: 2700,
        },
      },
    });
    const warning = {
      schema_version: "v1" as const,
      event_type: "session.hosted.warning.issued.v1" as const,
      session_id: snapshot.session_id,
      session_sequence: "21",
      stream_cursor: "219",
      session_version: 7,
      occurred_at: "2026-09-03T00:20:00Z",
      payload: {
        summary: "Session time warning issued.",
        remaining_seconds: 898,
        warning_code: "approaching" as const,
      },
    };

    const warned = sessionLiveReducer(withSnapshot, { type: "event", event: warning });
    const duplicate = sessionLiveReducer(warned, { type: "event", event: warning });

    expect(warned.snapshot?.timing?.remaining_seconds).toBe(898);
    expect(warned.snapshot?.timing?.warning_code).toBe("approaching");
    expect(warned.snapshot?.last_confirmed_stream_cursor).toBe("219");
    expect(duplicate).toBe(warned);
  });

  it("applies hosted lifecycle pause changes", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const next = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.lifecycle.changed.v1",
        session_id: snapshot.session_id,
        session_sequence: "18",
        stream_cursor: "181",
        session_version: 5,
        occurred_at: "2026-09-03T00:10:00Z",
        payload: {
          summary: "Session paused.",
          lifecycle_state: "paused",
        },
      },
    });

    expect(next.snapshot?.lifecycle_state).toBe("paused");
  });

  it("maps hosted access revocation to sign-in recovery", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const next = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.access.changed.v1",
        session_id: snapshot.session_id,
        session_sequence: "21",
        stream_cursor: "211",
        session_version: 6,
        occurred_at: "2026-09-03T00:20:00Z",
        payload: {
          summary: "Session access changed.",
          access_state: "revoked",
        },
      },
    });

    expect(next.snapshot?.recovery_category).toBe("sign_in");
  });

  it("maps hosted reconcile required to snapshot reconciliation", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const next = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.reconcile.required.v1",
        session_id: snapshot.session_id,
        session_sequence: "22",
        stream_cursor: "221",
        session_version: 6,
        occurred_at: "2026-09-03T00:21:00Z",
        payload: {
          summary: "Session snapshot reconciliation required.",
          recovery_category: "reconcile_snapshot",
        },
      },
    });

    expect(next.snapshot?.recovery_category).toBe("reconcile_snapshot");
  });

  it("records no-action without inventing Agent text", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const next = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.no_action.v1",
        session_id: snapshot.session_id,
        session_sequence: "13",
        session_version: 3,
        occurred_at: "2026-09-03T00:00:03Z",
        payload: { summary: "No further Agent output.", work_state: "no_action", resolution_category: "no_action" },
      },
    });
    expect(next.snapshot?.activity?.work_state).toBe("no_action");
    expect(next.snapshot?.transcript?.items).toHaveLength(0);
  });

  it("clears considering work when the Agent turn completes", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, {
      type: "snapshot",
      snapshot: { ...snapshot, activity: { work_state: "working", turn_id: "turn.1" } },
    });
    const streaming = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.fragment.v1",
        session_id: snapshot.session_id,
        session_sequence: "13",
        session_version: 5,
        occurred_at: "2026-09-03T00:00:03Z",
        payload: {
          summary: "Durable Agent fragment.",
          agent_message_id: "amsg.synthetic.0001",
          text_delta: "Hello",
          work_state: "working",
        },
      },
    });
    expect(streaming.snapshot?.activity?.work_state).toBe("working");
    expect(streaming.snapshot?.session_version).toBe(5);

    const next = sessionLiveReducer(streaming, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.complete.v1",
        session_id: snapshot.session_id,
        session_sequence: "14",
        session_version: 6,
        occurred_at: "2026-09-03T00:00:04Z",
        payload: { summary: "Agent response complete.", agent_message_id: "amsg.synthetic.0001" },
      },
    });
    expect(next.snapshot?.activity?.work_state).toBe("idle");
    expect(next.snapshot?.session_version).toBe(6);
  });

  it("advances Session version from hosted SSE above the snapshot baseline", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const next = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.complete.v1",
        session_id: snapshot.session_id,
        session_sequence: "14",
        session_version: 5,
        occurred_at: "2026-09-03T00:00:04Z",
        payload: { summary: "Agent response complete.", agent_message_id: "amsg.synthetic.0001" },
      },
    });
    expect(next.snapshot?.session_version).toBe(5);
  });

  it("preserves SSE transcript items when a refetched snapshot has not caught up", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const streamed = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.fragment.v1",
        session_id: snapshot.session_id,
        session_sequence: "13",
        session_version: 5,
        occurred_at: "2026-09-03T00:00:03Z",
        payload: {
          summary: "Durable Agent fragment.",
          agent_message_id: "amsg.synthetic.0001",
          text_delta: "Hello examiner",
          work_state: "working",
        },
      },
    });
    const next = sessionLiveReducer(streamed, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        session_version: 4,
        last_confirmed_sequence: "12",
        transcript: {
          items: [],
          older_available: false,
        },
      },
    });

    expect(next.snapshot?.transcript?.items[0]?.content).toBe("Hello examiner");
    expect(next.snapshot?.session_version).toBe(5);
  });

  it("preserves a streaming Agent item when refetch omits it at the same sequence", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const streamed = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.fragment.v1",
        session_id: snapshot.session_id,
        session_sequence: "13",
        session_version: 5,
        occurred_at: "2026-09-03T00:00:03Z",
        payload: {
          summary: "Durable Agent fragment.",
          agent_message_id: "amsg.synthetic.0001",
          text_delta: "Hello examiner",
          work_state: "working",
        },
      },
    });
    const next = sessionLiveReducer(streamed, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        session_version: 5,
        last_confirmed_sequence: "13",
        activity: { work_state: "idle", turn_id: "turn.1" },
        transcript: {
          items: [],
          older_available: false,
        },
      },
    });

    expect(next.snapshot?.transcript?.items[0]?.status).toBe("streaming");
    expect(next.snapshot?.activity?.work_state).toBe("working");
    expect(sessionAgentTurnOpen(next.snapshot)).toBe(true);
  });

  it("clears considering work when agent.work reports no_action", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, {
      type: "snapshot",
      snapshot: { ...snapshot, activity: { work_state: "working", turn_id: "turn.1" } },
    });
    const next = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.no_action.v1",
        session_id: snapshot.session_id,
        session_sequence: "13",
        session_version: 5,
        occurred_at: "2026-09-03T00:00:04Z",
        payload: {
          summary: "No further Agent output.",
          work_state: "no_action",
          resolution_category: "no_action",
          turn_id: "turn.1",
        },
      },
    });
    expect(next.snapshot?.activity?.work_state).toBe("no_action");
  });

  it("applies another tab participant message acceptance from hosted SSE", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const next = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.message.accepted.v1",
        session_id: snapshot.session_id,
        session_sequence: "13",
        session_version: 4,
        occurred_at: "2026-09-03T00:00:02Z",
        payload: {
          summary: "Participant message accepted.",
          message_id: "msg.other.tab",
          turn_id: "turn.other",
        },
      },
    });
    expect(next.snapshot?.transcript?.items).toHaveLength(1);
    expect(next.snapshot?.transcript?.items[0]?.item_id).toBe("msg.other.tab");
    expect(next.snapshot?.last_confirmed_sequence).toBe("13");
  });

  it("does not let a stale snapshot rewind Session version on the same Session", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, {
      type: "snapshot",
      snapshot: { ...snapshot, session_version: 8, last_confirmed_sequence: "20" },
    });
    const next = sessionLiveReducer(withSnapshot, {
      type: "snapshot",
      snapshot: { ...snapshot, session_version: 4, last_confirmed_sequence: "16" },
    });
    expect(next.snapshot?.session_version).toBe(8);
    expect(next.snapshot?.last_confirmed_sequence).toBe("20");
  });

  it("stamps occurred_at on Agent transcript items from hosted SSE", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const streamed = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.fragment.v1",
        session_id: snapshot.session_id,
        session_sequence: "13",
        session_version: 3,
        occurred_at: "2026-09-03T00:00:03Z",
        payload: {
          summary: "Durable Agent fragment.",
          agent_message_id: "amsg.synthetic.0001",
          text_delta: "Hello",
          work_state: "working",
        },
      },
    });
    expect(streamed.snapshot?.transcript?.items[0]?.occurred_at).toBe("2026-09-03T00:00:03Z");

    const next = sessionLiveReducer(streamed, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.complete.v1",
        session_id: snapshot.session_id,
        session_sequence: "14",
        session_version: 4,
        occurred_at: "2026-09-03T00:00:04Z",
        payload: { summary: "Agent response complete.", agent_message_id: "amsg.synthetic.0001" },
      },
    });
    expect(next.snapshot?.transcript?.items[0]?.occurred_at).toBe("2026-09-03T00:00:03Z");
    expect(next.snapshot?.transcript?.items[0]?.status).toBe("complete");
  });

  it("does not rewind resolved Agent activity from a stale refetched snapshot", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        session_version: 6,
        last_confirmed_sequence: "14",
        activity: { work_state: "idle", turn_id: "turn.1" },
      },
    });
    const next = sessionLiveReducer(withSnapshot, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        session_version: 4,
        last_confirmed_sequence: "12",
        activity: { work_state: "working", turn_id: "turn.1" },
      },
    });
    expect(next.snapshot?.activity?.work_state).toBe("idle");
    expect(next.snapshot?.session_version).toBe(6);
  });

  it("does not rewind working Agent activity when a refetch reports idle at the same sequence", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        session_version: 5,
        last_confirmed_sequence: "13",
        activity: { work_state: "working", turn_id: "turn.1" },
      },
    });
    const next = sessionLiveReducer(withSnapshot, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        session_version: 5,
        last_confirmed_sequence: "13",
        activity: { work_state: "idle", turn_id: "turn.1" },
      },
    });
    expect(next.snapshot?.activity?.work_state).toBe("working");
    expect(sessionAgentTurnOpen(next.snapshot)).toBe(true);
  });

  it("does not rewind Agent transcript status from complete to streaming on stale refetch", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const streamed = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.fragment.v1",
        session_id: snapshot.session_id,
        session_sequence: "13",
        session_version: 5,
        occurred_at: "2026-09-03T00:00:03Z",
        payload: {
          summary: "Durable Agent fragment.",
          agent_message_id: "amsg.synthetic.0001",
          text_delta: "Hello examiner",
          work_state: "working",
        },
      },
    });
    const withCompleteItem = sessionLiveReducer(streamed, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.complete.v1",
        session_id: snapshot.session_id,
        session_sequence: "14",
        session_version: 6,
        occurred_at: "2026-09-03T00:00:04Z",
        payload: {
          summary: "Agent response complete.",
          agent_message_id: "amsg.synthetic.0001",
          work_state: "idle",
        },
      },
    });
    const next = sessionLiveReducer(withCompleteItem, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        session_version: 4,
        last_confirmed_sequence: "12",
        activity: { work_state: "working", turn_id: "turn.1" },
        transcript: {
          items: [{
            item_id: "amsg.synthetic.0001",
            author: "agent",
            status: "streaming",
            content: "Hello",
            sequence_start: "13",
            sequence_end: "13",
          }],
          older_available: false,
        },
      },
    });
    expect(next.snapshot?.transcript?.items[0]?.status).toBe("complete");
    expect(next.snapshot?.transcript?.items[0]?.content).toBe("Hello examiner");
    expect(next.snapshot?.activity?.work_state).toBe("idle");
    expect(sessionCommandsBlocked(next.snapshot, "idle")).toBe(false);
  });

  it("marks Agent terminal SSE status from item_status", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const streamed = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.fragment.v1",
        session_id: snapshot.session_id,
        session_sequence: "13",
        session_version: 5,
        occurred_at: "2026-09-03T00:00:03Z",
        payload: {
          summary: "Durable Agent fragment.",
          agent_message_id: "amsg.synthetic.0001",
          text_delta: "Hello examiner",
          work_state: "working",
        },
      },
    });
    const next = sessionLiveReducer(streamed, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.complete.v1",
        session_id: snapshot.session_id,
        session_sequence: "14",
        session_version: 6,
        occurred_at: "2026-09-03T00:00:04Z",
        payload: {
          summary: "Agent response incomplete.",
          agent_message_id: "amsg.synthetic.0001",
          item_status: "incomplete",
        },
      },
    });
    expect(next.snapshot?.transcript?.items[0]?.status).toBe("incomplete");
  });

  it("keeps snapshot incomplete status over a prior erroneous complete merge", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const erroneous = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.complete.v1",
        session_id: snapshot.session_id,
        session_sequence: "14",
        session_version: 6,
        occurred_at: "2026-09-03T00:00:04Z",
        payload: {
          summary: "Agent response complete.",
          agent_message_id: "amsg.synthetic.0001",
        },
      },
    });
    const next = sessionLiveReducer(erroneous, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        last_confirmed_sequence: "14",
        transcript: {
          items: [{
            item_id: "amsg.synthetic.0001",
            author: "agent",
            status: "incomplete",
            content: "Hello examiner",
            sequence_start: "13",
            sequence_end: "14",
          }],
          older_available: false,
        },
      },
    });
    expect(next.snapshot?.transcript?.items[0]?.status).toBe("incomplete");
  });

  it("keeps send blocked while an Agent item is still streaming", () => {
    expect(sessionAgentTurnOpen({
      ...snapshot,
      activity: { work_state: "idle" },
      transcript: {
        items: [{
          item_id: "amsg.1",
          author: "agent",
          status: "streaming",
          content: "Partial",
          sequence_start: "13",
          sequence_end: "13",
        }],
        older_available: false,
      },
    })).toBe(true);
    expect(sessionCommandsBlocked({
      ...snapshot,
      activity: { work_state: "idle" },
      transcript: {
        items: [{
          item_id: "amsg.1",
          author: "agent",
          status: "complete",
          content: "Done",
          sequence_start: "13",
          sequence_end: "14",
        }],
        older_available: false,
      },
    }, "idle")).toBe(false);
  });

  it("applies two hosted events that share one Session sequence", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, { type: "snapshot", snapshot });
    const queued = sessionLiveReducer(withSnapshot, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.agent.work.v1",
        session_id: snapshot.session_id,
        session_sequence: "20",
        stream_cursor: "200",
        session_version: 8,
        occurred_at: "2026-09-03T00:00:20Z",
        payload: { summary: "Agent work queued.", work_state: "queued", turn_id: "turn.2" },
      },
    });
    const accepted = sessionLiveReducer(queued, {
      type: "event",
      event: {
        schema_version: "v1",
        event_type: "session.hosted.message.accepted.v1",
        session_id: snapshot.session_id,
        session_sequence: "20",
        stream_cursor: "201",
        session_version: 8,
        occurred_at: "2026-09-03T00:00:20Z",
        payload: {
          summary: "Participant message accepted.",
          message_id: "msg.other.tab",
          turn_id: "turn.2",
        },
      },
    });
    expect(queued.snapshot?.activity?.work_state).toBe("queued");
    expect(accepted.snapshot?.activity?.work_state).toBe("queued");
    expect(accepted.snapshot?.transcript?.items[0]?.item_id).toBe("msg.other.tab");
    expect(accepted.snapshot?.last_confirmed_sequence).toBe("20");
    expect(accepted.lastStreamCursor).toBe("201");
  });

  it("lets a higher-version same-sequence snapshot replace working with failed", () => {
    const withSnapshot = sessionLiveReducer(emptySessionLiveView, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        session_version: 8,
        last_confirmed_sequence: "15",
        activity: { work_state: "working", turn_id: "turn.1" },
      },
    });
    const next = sessionLiveReducer(withSnapshot, {
      type: "snapshot",
      snapshot: {
        ...snapshot,
        session_version: 9,
        last_confirmed_sequence: "15",
        activity: { work_state: "failed", turn_id: "turn.1", resolution_category: "execution_failure" },
      },
    });
    expect(next.snapshot?.activity?.work_state).toBe("failed");
    expect(next.snapshot?.session_version).toBe(9);
    expect(sessionAgentTurnOpen(next.snapshot)).toBe(false);
  });

  it("detects post-send reconciliation from queued Agent work", () => {
    expect(sessionPostSendReconciled({
      ...snapshot,
      activity: { work_state: "queued" },
    }, "13")).toBe(true);
    expect(sessionPostSendReconciled({
      ...snapshot,
      activity: { work_state: "idle" },
      transcript: { items: [], older_available: false },
    }, "13")).toBe(false);
  });
});
