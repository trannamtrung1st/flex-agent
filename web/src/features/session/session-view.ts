import type { SessionHostedEventEnvelopeV1, SessionSnapshotTranscriptItemV1, SessionSnapshotV1 } from "../../contracts/v1";

export type SessionConnectionState = "connecting" | "connected" | "reconnecting" | "offline";
export type SessionSendState = "idle" | "pending" | "checking" | "uncertain";

export interface SessionLiveView {
  snapshot: SessionSnapshotV1 | null;
  draft: string;
  sendState: SessionSendState;
  connection: SessionConnectionState;
  lastError: string | null;
  lastAcceptedSequence: string | null;
  lastStreamCursor: string | null;
}

export const emptySessionLiveView: SessionLiveView = {
  snapshot: null,
  draft: "",
  sendState: "idle",
  connection: "connecting",
  lastError: null,
  lastAcceptedSequence: null,
  lastStreamCursor: null,
};

/** Agent turn still open: queued/working activity or a streaming Agent transcript item. */
export function sessionAgentTurnOpen(snapshot: SessionSnapshotV1 | null): boolean {
  if (!snapshot) {
    return false;
  }
  const workState = snapshot.activity?.work_state;
  if (workState === "working" || workState === "queued") {
    return true;
  }
  return (snapshot.transcript?.items ?? []).some(
    (item) => item.author === "agent" && item.status === "streaming",
  );
}

/** Post-send reconciliation observed on snapshot or hosted SSE. */
export function sessionPostSendReconciled(
  snapshot: SessionSnapshotV1 | null,
  lastAcceptedSequence: string | null,
): boolean {
  if (!snapshot) {
    return false;
  }
  if (sessionAgentTurnOpen(snapshot)) {
    return true;
  }
  const workState = snapshot.activity?.work_state;
  if (
    workState === "queued"
    || workState === "working"
    || workState === "no_action"
    || workState === "failed"
  ) {
    return true;
  }
  if (!lastAcceptedSequence) {
    return false;
  }
  const acceptedSequence = Number(lastAcceptedSequence);
  return (snapshot.transcript?.items ?? []).some((item) => {
    if (item.author !== "participant") {
      return false;
    }
    const itemSequence = Number(item.sequence_end ?? item.sequence_start);
    return Number.isFinite(itemSequence)
      && Number.isFinite(acceptedSequence)
      && itemSequence >= acceptedSequence;
  });
}

/** Hold send and completion until admission checks finish and the Agent turn resolves. */
export function sessionCommandsBlocked(
  snapshot: SessionSnapshotV1 | null,
  sendState: SessionSendState,
): boolean {
  return sendState !== "idle" || sessionAgentTurnOpen(snapshot);
}

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
      return {
        ...state,
        snapshot: mergeAuthoritativeSnapshot(state.snapshot, action.snapshot),
        lastStreamCursor: laterOptionalCursor(
          state.lastStreamCursor,
          action.snapshot.last_confirmed_stream_cursor ?? null,
        ),
      };
    case "event":
      return applyHostedEvent(state, action.event);
    case "draft":
      return { ...state, draft: action.draft };
    case "send":
      return {
        ...state,
        sendState: action.sendState,
        lastError: action.sendState === "pending" ? null : state.lastError,
        lastAcceptedSequence: action.sendState === "idle" ? null : state.lastAcceptedSequence,
      };
    case "accepted":
      return {
        ...state,
        snapshot: applyObservedVersion(state.snapshot, action.session_version, action.session_sequence),
        sendState: "checking",
        lastError: null,
        lastAcceptedSequence: action.session_sequence ?? state.lastAcceptedSequence,
      };
    case "connection":
      return { ...state, connection: action.connection };
    case "error":
      return { ...state, lastError: action.message };
    default:
      return state;
  }
}

function snapshotIsStale(previous: SessionSnapshotV1, incoming: SessionSnapshotV1): boolean {
  if (incoming.session_version > previous.session_version) {
    return false;
  }
  if (previous.session_version > incoming.session_version) {
    return true;
  }
  const previousSequence = Number(previous.last_confirmed_sequence);
  const incomingSequence = Number(incoming.last_confirmed_sequence);
  return Number.isFinite(previousSequence)
    && Number.isFinite(incomingSequence)
    && previousSequence > incomingSequence;
}

function mergeActivity(
  previous: SessionSnapshotV1,
  incoming: SessionSnapshotV1,
  incomingStale: boolean,
): SessionSnapshotV1["activity"] {
  if (incomingStale) {
    return previous.activity ?? incoming.activity;
  }
  const prior = previous.activity;
  const next = incoming.activity;
  if (!prior) {
    return next;
  }
  if (!next) {
    return prior;
  }
  const priorResolved = prior.work_state === "idle"
    || prior.work_state === "no_action"
    || prior.work_state === "failed";
  const priorBusy = prior.work_state === "working" || prior.work_state === "queued";
  const nextBusy = next.work_state === "working" || next.work_state === "queued";
  const nextResolved = next.work_state === "idle"
    || next.work_state === "no_action"
    || next.work_state === "failed";
  if (incoming.session_version > previous.session_version) {
    return next;
  }
  if (incoming.session_version < previous.session_version) {
    return prior;
  }
  const priorSequence = Number(previous.last_confirmed_sequence);
  const nextSequence = Number(incoming.last_confirmed_sequence);
  const sequenceComparable = Number.isFinite(priorSequence) && Number.isFinite(nextSequence);
  if (
    sequenceComparable
    && priorSequence >= nextSequence
    && ((priorResolved && nextBusy) || (priorBusy && nextResolved))
  ) {
    return prior;
  }
  return next;
}

function transcriptStatusRank(status: SessionSnapshotTranscriptItemV1["status"]): number {
  switch (status) {
    case "unavailable":
      return 5;
    case "complete":
      return 4;
    case "incomplete":
    case "cancelled":
      return 3;
    case "streaming":
      return 2;
    case "accepted":
      return 1;
    default:
      return 0;
  }
}

function mergeTranscriptItem(
  item: SessionSnapshotTranscriptItemV1,
  prior: SessionSnapshotTranscriptItemV1 | undefined,
): SessionSnapshotTranscriptItemV1 {
  if (!prior) {
    return item;
  }
  let merged = item;
  if (item.status === "unavailable") {
    return item;
  }
  if (!item.content && prior.content) {
    merged = { ...merged, content: prior.content };
  } else if (
    item.content
    && prior.content
    && prior.content.startsWith(item.content)
    && prior.content.length > item.content.length
  ) {
    merged = { ...merged, content: prior.content };
  }
  if (!merged.occurred_at && prior.occurred_at) {
    merged = { ...merged, occurred_at: prior.occurred_at };
  }
  if (item.author === "agent" && (item.status === "incomplete" || item.status === "cancelled")) {
    merged = { ...merged, status: item.status };
  } else if (prior.author === "agent" && (prior.status === "incomplete" || prior.status === "cancelled")) {
    merged = { ...merged, status: prior.status };
  } else {
    const resolvedStatus = transcriptStatusRank(prior.status) > transcriptStatusRank(merged.status)
      ? prior.status
      : merged.status;
    if (resolvedStatus !== merged.status) {
      merged = { ...merged, status: resolvedStatus };
    }
  }
  return merged;
}

function laterOptionalCursor(left: string | null, right: string | null): string | null {
  if (!left) {
    return right;
  }
  if (!right) {
    return left;
  }
  return laterSequence(left, right);
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

  const incomingStale = snapshotIsStale(previous, incoming);
  const merged: SessionSnapshotV1 = {
    ...incoming,
    session_version: Math.max(previous.session_version, incoming.session_version),
    last_confirmed_sequence: laterSequence(previous.last_confirmed_sequence, incoming.last_confirmed_sequence),
    last_confirmed_stream_cursor: laterOptionalCursor(
      previous.last_confirmed_stream_cursor ?? null,
      incoming.last_confirmed_stream_cursor ?? null,
    ) ?? undefined,
    activity: mergeActivity(previous, incoming, incomingStale),
  };
  const priorItems = previous.transcript?.items ?? [];
  const nextItems = incoming.transcript?.items ?? [];
  if (!incoming.transcript) {
    return merged;
  }

  const incomingIds = new Set(nextItems.map((item) => item.item_id));
  const incomingSequence = Number(incoming.last_confirmed_sequence);
  const preservedAhead = priorItems.filter((item) => {
    if (incomingIds.has(item.item_id)) {
      return false;
    }
    const itemSequence = Number(item.sequence_end ?? item.sequence_start);
    return Number.isFinite(itemSequence)
      && Number.isFinite(incomingSequence)
      && itemSequence >= incomingSequence;
  });

  if (priorItems.length === 0 || nextItems.length === 0) {
    if (preservedAhead.length === 0) {
      return merged;
    }
    return {
      ...merged,
      transcript: {
        ...incoming.transcript,
        items: preservedAhead,
      },
    };
  }

  return {
    ...merged,
    transcript: {
      ...incoming.transcript,
      items: [...nextItems, ...preservedAhead].map((item) => {
        const prior = priorItems.find((candidate) => candidate.item_id === item.item_id);
        return mergeTranscriptItem(item, prior);
      }),
    },
  };
}

function applyHostedEvent(state: SessionLiveView, event: SessionHostedEventEnvelopeV1): SessionLiveView {
  const snapshot = state.snapshot;
  if (!snapshot || snapshot.session_id !== event.session_id) {
    return state;
  }

  const streamCursor = Number(event.stream_cursor);
  const hasStreamCursor = Number.isFinite(streamCursor) && Boolean(event.stream_cursor);
  if (hasStreamCursor) {
    const applied = Number(state.lastStreamCursor ?? snapshot.last_confirmed_stream_cursor ?? "0");
    if (Number.isFinite(applied) && streamCursor <= applied) {
      return state;
    }
  } else {
    const nextSequence = Number(event.session_sequence);
    const current = Number(snapshot.last_confirmed_sequence);
    if (!Number.isFinite(nextSequence) || nextSequence <= current) {
      return state;
    }
  }

  const next: SessionSnapshotV1 = {
    ...snapshot,
    last_confirmed_sequence: laterSequence(snapshot.last_confirmed_sequence, event.session_sequence),
    last_confirmed_stream_cursor: hasStreamCursor
      ? laterSequence(snapshot.last_confirmed_stream_cursor ?? "0", event.stream_cursor!)
      : snapshot.last_confirmed_stream_cursor,
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
        items[existing] = {
          ...items[existing]!,
          status: event.payload.item_status ?? "complete",
          occurred_at: items[existing]!.occurred_at ?? event.occurred_at,
        };
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

  if (
    (event.event_type === "session.hosted.agent.work.v1" || event.event_type === "session.hosted.agent.fragment.v1")
    && event.payload.work_state
  ) {
    next.activity = {
      work_state: event.payload.work_state,
      turn_id: event.payload.turn_id ?? snapshot.activity?.turn_id,
      resolution_category: event.payload.resolution_category,
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
        occurred_at: currentItem.occurred_at ?? event.occurred_at,
      };
    } else {
      items.push({
        item_id: messageId,
        author: "agent",
        status: "streaming",
        content: event.payload.text_delta,
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

  return {
    ...state,
    snapshot: next,
    lastStreamCursor: hasStreamCursor
      ? laterSequence(state.lastStreamCursor ?? "0", event.stream_cursor!)
      : laterSequence(state.lastStreamCursor ?? "0", event.session_sequence),
  };
}
