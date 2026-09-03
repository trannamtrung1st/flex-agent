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
  | { type: "accepted"; session_version?: number | null; session_sequence?: string | null }
  | { type: "connection"; connection: SessionConnectionState }
  | { type: "error"; message: string | null };

export function sessionLiveReducer(state: SessionLiveView, action: SessionLiveAction): SessionLiveView {
  switch (action.type) {
    case "snapshot":
      return { ...state, snapshot: mergeAuthoritativeSnapshot(state.snapshot, action.snapshot) };
    case "event":
      return applyHostedEvent(state, action.event);
    case "draft":
      return { ...state, draft: action.draft };
    case "send":
      return {
        ...state,
        sendState: action.sendState,
        lastError: action.sendState === "pending" ? null : state.lastError,
      };
    case "accepted":
      return {
        ...state,
        snapshot: applyObservedVersion(state.snapshot, action.session_version, action.session_sequence),
        sendState: "idle",
        lastError: null,
      };
    case "connection":
      return { ...state, connection: action.connection };
    case "error":
      return { ...state, lastError: action.message };
    default:
      return state;
  }
}

function laterSequence(left: string, right: string): string {
  const leftNumber = Number(left);
  const rightNumber = Number(right);
  if (!Number.isFinite(leftNumber)) {
    return right;
  }
  if (!Number.isFinite(rightNumber) || leftNumber > rightNumber) {
    return left;
  }
  return right;
}

function applyObservedVersion(
  snapshot: SessionSnapshotV1 | null,
  sessionVersion?: number | null,
  sessionSequence?: string | null,
): SessionSnapshotV1 | null {
  if (!snapshot) {
    return snapshot;
  }

  return {
    ...snapshot,
    session_version: Math.max(sessionVersion ?? 0, snapshot.session_version),
    last_confirmed_sequence: sessionSequence
      ? laterSequence(snapshot.last_confirmed_sequence, sessionSequence)
      : snapshot.last_confirmed_sequence,
  };
}

function mergeAuthoritativeSnapshot(
  previous: SessionSnapshotV1 | null,
  incoming: SessionSnapshotV1,
): SessionSnapshotV1 {
  if (!previous || previous.session_id !== incoming.session_id) {
    return incoming;
  }

  const merged: SessionSnapshotV1 = {
    ...incoming,
    session_version: Math.max(previous.session_version, incoming.session_version),
    last_confirmed_sequence: laterSequence(previous.last_confirmed_sequence, incoming.last_confirmed_sequence),
  };
  const priorItems = previous.transcript?.items ?? [];
  const nextItems = incoming.transcript?.items ?? [];
  if (priorItems.length === 0 || nextItems.length === 0 || !incoming.transcript) {
    return merged;
  }

  return {
    ...merged,
    transcript: {
      ...incoming.transcript,
      items: nextItems.map((item) => {
        const prior = priorItems.find((candidate) => candidate.item_id === item.item_id);
        if (!prior?.content) {
          return item;
        }
        if (item.status === "unavailable") {
          return item;
        }
        if (!item.content) {
          return {
            ...item,
            content: prior.content,
          };
        }
        if (prior.content.startsWith(item.content) && prior.content.length > item.content.length) {
          return { ...item, content: prior.content };
        }
        return item;
      }),
    },
  };
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
    session_version: Math.max(event.session_version ?? 0, snapshot.session_version),
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
    next.activity = {
      work_state: event.payload.work_state ?? "idle",
      turn_id: event.payload.turn_id ?? snapshot.activity?.turn_id,
      resolution_category: event.payload.resolution_category,
    };
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
    next.activity = {
      work_state: event.payload.work_state ?? "working",
      turn_id: event.payload.turn_id ?? snapshot.activity?.turn_id,
      resolution_category: event.payload.resolution_category,
    };
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
