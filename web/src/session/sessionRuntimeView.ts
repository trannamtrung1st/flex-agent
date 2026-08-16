import type { SseSessionEventV1 } from "../contracts/v1";

export type AgentPresenceState = "dormant" | "ready" | "processing";

export type AgentTurnPhase =
  | "idle"
  | "queued"
  | "working"
  | "streaming"
  | "complete"
  | "intentional_no_reply"
  | "suppressed_failure"
  | "execution_failure"
  | "cancelled";

export type ConnectionState = "connecting" | "connected" | "reconnecting" | "reconciling" | "offline";

export interface StreamedAgentMessage {
  id: string;
  turnId?: string;
  content: string;
  status: "streaming" | "confirmed" | "incomplete";
}

export interface SessionRuntimeView {
  seenSequences: ReadonlySet<string>;
  announcedMilestones: ReadonlySet<string>;
  agentPresence: AgentPresenceState;
  turnPhase: AgentTurnPhase;
  turnId: string | null;
  persistentTurnStatus: string | null;
  politeAnnouncement: string | null;
  assertiveAnnouncement: string | null;
  streamedMessages: readonly StreamedAgentMessage[];
  connectionState: ConnectionState;
}

export const NO_AGENT_REPLY_STATUS = "No Agent reply for this turn";
export const AGENT_PREPARING_COPY = "The Agent is preparing a response.";
export const AGENT_RESPONDING_COPY = "Agent is responding";
export const AGENT_COMPLETE_COPY = "Agent response complete.";
export const NEW_AGENT_MESSAGE_COPY = "New Agent message";
export const SUPPRESSED_TURN_STATUS = "This turn could not be completed.";
export const EXECUTION_FAILURE_TURN_STATUS = "The Agent could not finish this response.";
export const RECONNECTING_COPY =
  "Reconnecting. Your Session and time have not been paused by this connection issue.";
export const RECONCILING_COPY = "Updating Session state.";
export const OFFLINE_COPY =
  "You cannot continue while disconnected. Session time may continue.";
export const AGENT_COMPLETE_STATUS = "Complete";
export const AGENT_INCOMPLETE_STATUS = "Incomplete";
export const AGENT_CANCELLED_STATUS = "Cancelled";
export const PROJECTION_RETRY_COPY = "Could not update Session. Your draft and transcript are still here.";

export function requiresSessionProjectionReconcile(eventType: string): boolean {
  return eventType === "session.state.changed.v1" || eventType === "session.terminal.v1";
}

export function commandsEnabled(connectionState: ConnectionState): boolean {
  return connectionState === "connected";
}

export function isSessionAccessLoss(message: string): boolean {
  return (
    message === "protected" ||
    message === "unauthenticated" ||
    message === "Access denied" ||
    message.includes("Access")
  );
}

export function compareSessionSequence(left?: string | null, right?: string | null): number {
  if (left == null || left === "") {
    return 0;
  }
  if (right == null || right === "") {
    return 1;
  }

  const leftNumber = Number(left);
  const rightNumber = Number(right);
  if (Number.isFinite(leftNumber) && Number.isFinite(rightNumber)) {
    return leftNumber === rightNumber ? 0 : leftNumber > rightNumber ? 1 : -1;
  }

  return left === right ? 0 : left > right ? 1 : -1;
}

export function isProjectionAtLeastAsNew(
  incoming: { session_version: number; last_sequence?: string | null },
  committed: { session_version: number; last_sequence?: string | null } | null,
): boolean {
  if (!committed) {
    return true;
  }
  if (incoming.session_version !== committed.session_version) {
    return incoming.session_version > committed.session_version;
  }
  return compareSessionSequence(incoming.last_sequence, committed.last_sequence) >= 0;
}

export function shouldCommitProjection(input: {
  requestId: number;
  latestRequestId: number;
  incoming: { session_version: number; last_sequence?: string | null };
  committed: { session_version: number; last_sequence?: string | null } | null;
}): boolean {
  return input.requestId === input.latestRequestId && isProjectionAtLeastAsNew(input.incoming, input.committed);
}

export function canMarkConnectedAfterReconcile(input: {
  reconcileEpoch: number;
  currentEpoch: number;
  readyState: number;
  openReadyState: number;
}): boolean {
  return input.reconcileEpoch === input.currentEpoch && input.readyState === input.openReadyState;
}

export function transcriptStatusLabel(item: { streaming: boolean; status: string }): string | null {
  if (item.streaming) {
    return AGENT_RESPONDING_COPY;
  }
  if (item.status === "accepted") {
    return "Message accepted";
  }
  if (item.status === "confirmed") {
    return AGENT_COMPLETE_STATUS;
  }
  if (item.status === "incomplete") {
    return AGENT_INCOMPLETE_STATUS;
  }
  if (item.status === "cancelled") {
    return AGENT_CANCELLED_STATUS;
  }
  return null;
}

const FORBIDDEN_CONTROL_PATTERNS = [
  /\bno_action\b/i,
  /\bwork_state\b/i,
  /\bresolution_category\b/i,
  /\brequested_action\b/i,
  /\boutput_id\b/i,
  /\baudience\b/i,
  /\brevision_id\b/i,
  /\bfire_at\b/i,
  /\blane_state\b/i,
  /\bschedule revision\b/i,
];

export function containsForbiddenControlCopy(value: string): boolean {
  return FORBIDDEN_CONTROL_PATTERNS.some((pattern) => pattern.test(value));
}

export function createSessionRuntimeView(
  lifecycleState: string,
  connectionState: ConnectionState = "connecting",
): SessionRuntimeView {
  return {
    seenSequences: new Set(),
    announcedMilestones: new Set(),
    agentPresence: presenceForLifecycle(lifecycleState, "idle"),
    turnPhase: "idle",
    turnId: null,
    persistentTurnStatus: null,
    politeAnnouncement: null,
    assertiveAnnouncement: null,
    streamedMessages: [],
    connectionState,
  };
}

export function applySseEvent(view: SessionRuntimeView, event: SseSessionEventV1): SessionRuntimeView {
  const sequence = event.session_sequence;
  if (sequence && view.seenSequences.has(sequence)) {
    return view;
  }

  const seenSequences = new Set(view.seenSequences);
  if (sequence) {
    seenSequences.add(sequence);
  }

  const next: SessionRuntimeView = {
    ...view,
    seenSequences,
    politeAnnouncement: null,
    connectionState: view.connectionState === "connecting" ? "connected" : view.connectionState,
  };

  switch (event.event_type) {
    case "session.agent.work.v1":
      return applyWorkEvent(next, event);
    case "session.agent.fragment.v1":
      return applyFragmentEvent(next, event);
    case "session.agent.complete.v1":
      return applyCompleteEvent(next, event);
    case "session.state.changed.v1":
      return announceAssertive(next, event.payload.summary);
    case "session.terminal.v1":
      return applyTerminalEvent(next, event.payload.summary);
    default:
      return next;
  }
}

export function markReconnecting(view: SessionRuntimeView): SessionRuntimeView {
  if (view.connectionState === "reconnecting") {
    return view;
  }

  return {
    ...view,
    connectionState: "reconnecting",
    assertiveAnnouncement: RECONNECTING_COPY,
  };
}

export function markReconciling(view: SessionRuntimeView): SessionRuntimeView {
  return {
    ...view,
    connectionState: "reconciling",
    politeAnnouncement: RECONCILING_COPY,
  };
}

export function markOffline(view: SessionRuntimeView): SessionRuntimeView {
  if (view.connectionState === "offline") {
    return view;
  }

  return {
    ...view,
    connectionState: "offline",
    assertiveAnnouncement: OFFLINE_COPY,
  };
}

export function markConnected(view: SessionRuntimeView): SessionRuntimeView {
  const connectivityAnnouncement =
    view.assertiveAnnouncement === RECONNECTING_COPY || view.assertiveAnnouncement === OFFLINE_COPY;

  return {
    ...view,
    connectionState: "connected",
    assertiveAnnouncement: connectivityAnnouncement ? null : view.assertiveAnnouncement,
  };
}

export function presenceForLifecycle(lifecycleState: string, turnPhase: AgentTurnPhase): AgentPresenceState {
  if (lifecycleState === "completed" || lifecycleState === "terminated" || lifecycleState === "aborted") {
    return "dormant";
  }

  if (turnPhase === "queued" || turnPhase === "working" || turnPhase === "streaming") {
    return "processing";
  }

  return "ready";
}

function applyWorkEvent(view: SessionRuntimeView, event: SseSessionEventV1): SessionRuntimeView {
  const turnId = event.payload.turn_id ?? view.turnId;
  const workState = event.payload.work_state;

  if (workState === "queued" || workState === "working") {
    const milestoneKey = `${turnId ?? "turn"}:preparing`;
    const alreadyAnnounced = view.announcedMilestones.has(milestoneKey);
    const announcedMilestones = new Set(view.announcedMilestones);
    announcedMilestones.add(milestoneKey);

    return {
      ...view,
      turnId,
      turnPhase: workState === "queued" ? "queued" : "working",
      agentPresence: "processing",
      persistentTurnStatus: null,
      announcedMilestones,
      politeAnnouncement: alreadyAnnounced ? null : AGENT_PREPARING_COPY,
    };
  }

  if (workState === "resolved") {
    return applyResolvedWork(view, event, turnId);
  }

  return view;
}

function applyResolvedWork(
  view: SessionRuntimeView,
  event: SseSessionEventV1,
  turnId: string | null | undefined,
): SessionRuntimeView {
  const category = event.payload.resolution_category;
  const milestoneKey = `${turnId ?? "turn"}:resolved:${category ?? "unknown"}`;
  const alreadyAnnounced = view.announcedMilestones.has(milestoneKey);
  const announcedMilestones = new Set(view.announcedMilestones);
  announcedMilestones.add(milestoneKey);

  if (category === "no_action") {
    const persistent =
      event.payload.show_persistent_turn_status === true ? NO_AGENT_REPLY_STATUS : null;
    return {
      ...view,
      turnId: turnId ?? view.turnId,
      turnPhase: "intentional_no_reply",
      agentPresence: "ready",
      persistentTurnStatus: persistent,
      announcedMilestones,
      politeAnnouncement: alreadyAnnounced ? null : "Turn resolved without Agent reply.",
    };
  }

  if (category === "suppressed_failure") {
    return {
      ...view,
      turnId: turnId ?? view.turnId,
      turnPhase: "suppressed_failure",
      agentPresence: "ready",
      persistentTurnStatus: SUPPRESSED_TURN_STATUS,
      announcedMilestones,
      politeAnnouncement: alreadyAnnounced ? null : SUPPRESSED_TURN_STATUS,
    };
  }

  if (category === "execution_failure") {
    return {
      ...view,
      turnId: turnId ?? view.turnId,
      turnPhase: "execution_failure",
      agentPresence: "ready",
      persistentTurnStatus: EXECUTION_FAILURE_TURN_STATUS,
      announcedMilestones,
      politeAnnouncement: alreadyAnnounced ? null : EXECUTION_FAILURE_TURN_STATUS,
    };
  }

  return {
    ...view,
    turnId: turnId ?? view.turnId,
    turnPhase: "complete",
    agentPresence: "ready",
    announcedMilestones,
  };
}

function applyFragmentEvent(view: SessionRuntimeView, event: SseSessionEventV1): SessionRuntimeView {
  const messageId = event.payload.agent_message_id;
  if (!messageId) {
    return view;
  }

  const delta = event.payload.text_delta ?? "";
  const turnId = event.payload.turn_id ?? view.turnId ?? undefined;
  const existing = view.streamedMessages.find((item) => item.id === messageId);
  const streamedMessages = existing
    ? view.streamedMessages.map((item) =>
        item.id === messageId
          ? { ...item, content: item.content + delta, status: "streaming" as const, turnId }
          : item,
      )
    : [...view.streamedMessages, { id: messageId, turnId, content: delta, status: "streaming" as const }];

  const milestoneKey = `${messageId}:available`;
  const alreadyAnnounced = view.announcedMilestones.has(milestoneKey);
  const announcedMilestones = new Set(view.announcedMilestones);
  announcedMilestones.add(milestoneKey);

  return {
    ...view,
    turnId: turnId ?? view.turnId,
    turnPhase: "streaming",
    agentPresence: "processing",
    persistentTurnStatus: null,
    streamedMessages,
    announcedMilestones,
    politeAnnouncement: alreadyAnnounced ? null : NEW_AGENT_MESSAGE_COPY,
  };
}

function applyCompleteEvent(view: SessionRuntimeView, event: SseSessionEventV1): SessionRuntimeView {
  const messageId = event.payload.agent_message_id;
  const streamedMessages = messageId
    ? view.streamedMessages.map((item) =>
        item.id === messageId ? { ...item, status: "confirmed" as const } : item,
      )
    : view.streamedMessages;

  const milestoneKey = `${messageId ?? view.turnId ?? "turn"}:complete`;
  const alreadyAnnounced = view.announcedMilestones.has(milestoneKey);
  const announcedMilestones = new Set(view.announcedMilestones);
  announcedMilestones.add(milestoneKey);

  return {
    ...view,
    turnPhase: "complete",
    agentPresence: "ready",
    streamedMessages,
    announcedMilestones,
    politeAnnouncement: alreadyAnnounced ? null : AGENT_COMPLETE_COPY,
  };
}

function applyTerminalEvent(view: SessionRuntimeView, summary: string): SessionRuntimeView {
  const inFlight =
    view.turnPhase === "queued" || view.turnPhase === "working" || view.turnPhase === "streaming";

  return {
    ...announceAssertive(view, summary),
    agentPresence: "dormant",
    turnPhase: inFlight ? "cancelled" : view.turnPhase,
    streamedMessages: view.streamedMessages.map((item) =>
      item.status === "streaming" ? { ...item, status: "incomplete" } : item,
    ),
  };
}

function announceAssertive(view: SessionRuntimeView, summary: string): SessionRuntimeView {
  return {
    ...view,
    assertiveAnnouncement: summary,
  };
}
