import { emptySessionLiveView, sessionLiveReducer } from "./session-view";
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

  it("keeps richer local Agent text when a later snapshot is unavailable", () => {
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

    expect(next.snapshot?.transcript?.items[0]?.content).toBe("Hello examiner");
    expect(next.snapshot?.transcript?.items[0]?.status).toBe("streaming");
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
});
