import type { SessionHostedEventEnvelopeV1, SessionSnapshotV1 } from "../../contracts/v1";

export type SessionConnectionState = "connecting" | "connected" | "reconnecting" | "offline";
export type SessionSendState = "idle" | "pending" | "checking" | "uncertain";

export interface SessionLiveView {
  snapshot: SessionSnapshotV1 | null;
  draft: string;
  sendState: SessionSendState;
  connection: SessionConnectionState;
  lastError: string | null;
}

export const emptySessionLiveView: SessionLiveView = {
  snapshot: null,
  draft: "",
  sendState: "idle",
  connection: "connecting",
  lastError: null,
};

export type SessionLiveAction =
  | { type: "snapshot"; snapshot: SessionSnapshotV1 }
  | { type: "event"; event: SessionHostedEventEnvelopeV1 }
  | { type: "draft"; draft: string }
  | { type: "send"; sendState: SessionSendState }
  | { type: "connection"; connection: SessionConnectionState }
  | { type: "error"; message: string | null };

export function sessionLiveReducer(state: SessionLiveView, action: SessionLiveAction): SessionLiveView {
  switch (action.type) {
    case "snapshot":
      return { ...state, snapshot: action.snapshot, lastError: null };
    case "event":
      return applyHostedEvent(state, action.event);
    case "draft":
      return { ...state, draft: action.draft };
    case "send":
      return { ...state, sendState: action.sendState };
    case "connection":
      return { ...state, connection: action.connection };
    case "error":
      return { ...state, lastError: action.message };
    default:
      return state;
  }
}

function applyHostedEvent(state: SessionLiveView, event: SessionHostedEventEnvelopeV1): SessionLiveView {
  const snapshot = state.snapshot;
  if (!snapshot || snapshot.session_id !== event.session_id) {
    return state;
  }

  const nextSequence = Number(event.session_sequence);
  const current = Number(snapshot.last_confirmed_sequence);
  if (!Number.isFinite(nextSequence) || nextSequence <= current) {
    return state;
  }

  const next: SessionSnapshotV1 = {
    ...snapshot,
    last_confirmed_sequence: event.session_sequence,
    session_version: event.session_version || snapshot.session_version,
    lifecycle_state: event.payload.lifecycle_state ?? snapshot.lifecycle_state,
    activity: event.payload.work_state
      ? {
          work_state: event.payload.work_state,
          turn_id: event.payload.turn_id,
          resolution_category: event.payload.resolution_category,
        }
      : snapshot.activity,
    recovery_category: event.payload.recovery_category ?? snapshot.recovery_category,
  };

  if (event.event_type === "session.hosted.lifecycle.changed.v1" && event.payload.lifecycle_state) {
    next.lifecycle_state = event.payload.lifecycle_state;
  }

  if (event.event_type === "session.hosted.agent.complete.v1") {
    const items = [...(next.transcript?.items ?? [])];
    const messageId = event.payload.agent_message_id;
    if (messageId) {
      const existing = items.findIndex((item) => item.item_id === messageId);
      if (existing >= 0) {
        items[existing] = { ...items[existing]!, status: "complete" };
        next.transcript = {
          items,
          older_available: next.transcript?.older_available ?? false,
          oldest_sequence: next.transcript?.oldest_sequence,
          newest_sequence: event.session_sequence,
        };
      }
    }
  }

  if (event.event_type === "session.hosted.agent.no_action.v1") {
    next.activity = {
      work_state: "no_action",
      turn_id: event.payload.turn_id,
      resolution_category: event.payload.resolution_category ?? "no_action",
    };
  }

  if (event.event_type === "session.hosted.agent.fragment.v1" && event.payload.text_delta) {
    const items = [...(next.transcript?.items ?? [])];
    const messageId = event.payload.agent_message_id ?? `amsg.${event.session_sequence}`;
    const existing = items.findIndex((item) => item.item_id === messageId);
    if (existing >= 0) {
      const currentItem = items[existing]!;
      items[existing] = {
        ...currentItem,
        status: "streaming",
        content: `${currentItem.content ?? ""}${event.payload.text_delta}`,
        sequence_end: event.session_sequence,
      };
    } else {
      items.push({
        item_id: messageId,
        author: "agent",
        status: "streaming",
        content: event.payload.text_delta,
        sequence_start: event.session_sequence,
        sequence_end: event.session_sequence,
        turn_id: event.payload.turn_id,
      });
    }
    next.transcript = {
      items,
      older_available: next.transcript?.older_available ?? false,
      oldest_sequence: next.transcript?.oldest_sequence,
      newest_sequence: event.session_sequence,
    };
  }

  if (event.event_type === "session.hosted.message.accepted.v1" && event.payload.message_id) {
    const items = [...(next.transcript?.items ?? [])];
    if (!items.some((item) => item.item_id === event.payload.message_id)) {
      items.push({
        item_id: event.payload.message_id,
        author: "participant",
        status: "accepted",
        content: null,
        sequence_start: event.session_sequence,
        sequence_end: event.session_sequence,
        occurred_at: event.occurred_at,
        turn_id: event.payload.turn_id,
      });
    }
    next.transcript = {
      items,
      older_available: next.transcript?.older_available ?? false,
      oldest_sequence: next.transcript?.oldest_sequence,
      newest_sequence: event.session_sequence,
    };
  }

  return { ...state, snapshot: next };
}
