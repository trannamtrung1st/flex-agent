import {
  applySseEvent,
  canMarkConnectedAfterReconcile,
  commandsEnabled,
  containsForbiddenControlCopy,
  createSessionRuntimeView,
  evaluateProjectionCommit,
  isCurrentCommandIdentityReconciliation,
  isCurrentSessionAction,
  markConnected,
  markOffline,
  markReconciling,
  markReconnecting,
  shouldReconcileProjectionOnOpen,
  classifyCommandAdmission,
  effectForCommandIdentityOutcome,
  shouldRetainCommandIdentity,
  transcriptStatusLabel,
} from "./sessionRuntimeView";
import type { SseSessionEventV1 } from "../contracts/v1";

function workEvent(
  sequence: string,
  workState: "queued" | "working" | "resolved",
  extras: Partial<SseSessionEventV1["payload"]> = {},
): SseSessionEventV1 {
  return {
    schema_version: "v1",
    event_type: "session.agent.work.v1",
    session_id: "sess.synthetic.0001",
    session_sequence: sequence,
    occurred_at: "2026-08-16T00:00:00Z",
    payload: {
      summary: extras.summary ?? "The Agent is preparing a response.",
      turn_id: extras.turn_id ?? "turn.synthetic.0001",
      work_state: workState,
      ...extras,
    },
  };
}

describe("sessionRuntimeView", () => {
  it("does not treat SSE payloads as a live connection while the stream is still connecting", () => {
    let view = createSessionRuntimeView("active");
    view = applySseEvent(view, workEvent("1", "queued"));

    expect(view.connectionState).toBe("connecting");
    expect(commandsEnabled(view.connectionState)).toBe(false);
    expect(view.turnPhase).toBe("queued");
  });

  it("treats queued and working as Agent presence processing without a transcript message", () => {
    let view = createSessionRuntimeView("active");
    view = applySseEvent(view, workEvent("1", "queued"));
    view = applySseEvent(view, workEvent("2", "working"));

    expect(view.agentPresence).toBe("processing");
    expect(view.turnPhase).toBe("working");
    expect(view.streamedMessages).toEqual([]);
    expect(view.politeAnnouncement).toBeNull();
    expect(view.persistentTurnStatus).toBeNull();
  });

  it("announces preparing once for queued then working", () => {
    let view = createSessionRuntimeView("active");
    view = applySseEvent(view, workEvent("1", "queued"));
    expect(view.politeAnnouncement).toBe("The Agent is preparing a response.");
    view = applySseEvent(view, workEvent("2", "working"));
    expect(view.politeAnnouncement).toBeNull();
  });

  it("resolves no-action without an Agent message, error, or internal label", () => {
    let view = createSessionRuntimeView("active");
    view = applySseEvent(view, workEvent("1", "queued"));
    view = applySseEvent(view, workEvent("2", "working"));
    view = applySseEvent(
      view,
      workEvent("3", "resolved", {
        resolution_category: "no_action",
        show_persistent_turn_status: true,
        summary: "Turn resolved without Agent reply.",
      }),
    );

    expect(view.agentPresence).toBe("ready");
    expect(view.turnPhase).toBe("intentional_no_reply");
    expect(view.streamedMessages).toEqual([]);
    expect(view.persistentTurnStatus).toBe("No Agent reply for this turn");
    expect(view.politeAnnouncement).toBe("Turn resolved without Agent reply.");
    expect(containsForbiddenControlCopy(view.persistentTurnStatus ?? "")).toBe(false);
    expect(containsForbiddenControlCopy(view.politeAnnouncement ?? "")).toBe(false);
  });

  it("omits persistent no-action status when the workflow does not require one", () => {
    let view = createSessionRuntimeView("active");
    view = applySseEvent(
      view,
      workEvent("1", "resolved", {
        resolution_category: "no_action",
        show_persistent_turn_status: false,
        summary: "Turn resolved without Agent reply.",
      }),
    );

    expect(view.turnPhase).toBe("intentional_no_reply");
    expect(view.persistentTurnStatus).toBeNull();
    expect(view.streamedMessages).toEqual([]);
  });

  it("does not re-announce a replayed resolved sequence", () => {
    const resolved = workEvent("3", "resolved", {
      resolution_category: "no_action",
      show_persistent_turn_status: true,
      summary: "Turn resolved without Agent reply.",
    });
    let view = createSessionRuntimeView("active");
    view = applySseEvent(view, resolved);
    view = applySseEvent(view, resolved);

    expect(view.politeAnnouncement).toBe("Turn resolved without Agent reply.");
    const replayed = applySseEvent(
      { ...view, politeAnnouncement: null },
      resolved,
    );
    expect(replayed.politeAnnouncement).toBeNull();
    expect(replayed.seenSequences.size).toBe(1);
  });

  it("presents suppressed failure as a bounded turn status, not no-action or a message", () => {
    let view = createSessionRuntimeView("active");
    view = applySseEvent(
      view,
      workEvent("1", "resolved", {
        resolution_category: "suppressed_failure",
        show_persistent_turn_status: false,
        summary: "This turn could not be completed.",
      }),
    );

    expect(view.turnPhase).toBe("suppressed_failure");
    expect(view.persistentTurnStatus).toBe("This turn could not be completed.");
    expect(view.streamedMessages).toEqual([]);
    expect(view.turnPhase).not.toBe("intentional_no_reply");
  });

  it("presents execution failure as Agent unfinished, not policy suppression", () => {
    let view = createSessionRuntimeView("active");
    view = applySseEvent(
      view,
      workEvent("1", "resolved", {
        resolution_category: "execution_failure",
        summary: "The Agent could not finish this turn.",
      }),
    );

    expect(view.turnPhase).toBe("execution_failure");
    expect(view.persistentTurnStatus).toBe("The Agent could not finish this response.");
  });

  it("appends durable fragments into one Agent message and confirms completion", () => {
    let view = createSessionRuntimeView("active");
    view = applySseEvent(view, {
      schema_version: "v1",
      event_type: "session.agent.fragment.v1",
      session_id: "sess.synthetic.0001",
      session_sequence: "4",
      occurred_at: "2026-08-16T00:00:00Z",
      payload: {
        summary: "Agent response fragment.",
        agent_message_id: "msg.synthetic.agent.1",
        text_delta: "Hello ",
        turn_id: "turn.synthetic.0001",
      },
    });
    view = applySseEvent(view, {
      schema_version: "v1",
      event_type: "session.agent.fragment.v1",
      session_id: "sess.synthetic.0001",
      session_sequence: "5",
      occurred_at: "2026-08-16T00:00:00Z",
      payload: {
        summary: "Agent response fragment.",
        agent_message_id: "msg.synthetic.agent.1",
        text_delta: "there.",
        turn_id: "turn.synthetic.0001",
      },
    });
    view = applySseEvent(view, {
      schema_version: "v1",
      event_type: "session.agent.complete.v1",
      session_id: "sess.synthetic.0001",
      session_sequence: "6",
      occurred_at: "2026-08-16T00:00:00Z",
      payload: {
        summary: "Agent response complete.",
        agent_message_id: "msg.synthetic.agent.1",
        turn_id: "turn.synthetic.0001",
        resolution_category: "message_stream",
      },
    });

    expect(view.streamedMessages).toEqual([
      {
        id: "msg.synthetic.agent.1",
        turnId: "turn.synthetic.0001",
        content: "Hello there.",
        status: "confirmed",
      },
    ]);
    expect(view.turnPhase).toBe("complete");
    expect(view.agentPresence).toBe("ready");
  });

  it("marks Agent presence dormant on a terminal event even if the turn was working", () => {
    let view = createSessionRuntimeView("active");
    view = applySseEvent(view, workEvent("1", "working"));
    view = applySseEvent(view, {
      schema_version: "v1",
      event_type: "session.terminal.v1",
      session_id: "sess.synthetic.0001",
      session_sequence: "2",
      occurred_at: "2026-08-16T00:00:00Z",
      payload: { summary: "Session completed." },
    });

    expect(view.agentPresence).toBe("dormant");
    expect(view.turnPhase).toBe("cancelled");
    expect(view.assertiveAnnouncement).toBe("Session completed.");

    const reconciled = markConnected(markReconciling(view));
    expect(reconciled.connectionState).toBe("connected");
    expect(reconciled.assertiveAnnouncement).toBe("Session completed.");
    expect(commandsEnabled(markReconciling(view).connectionState)).toBe(false);
  });

  it("clears only connectivity announcements when the stream is connected again", () => {
    const reconnecting = markReconnecting({
      ...createSessionRuntimeView("active"),
      connectionState: "connected",
    });
    const recovered = markConnected(markReconciling(reconnecting));
    expect(recovered.connectionState).toBe("connected");
    expect(recovered.assertiveAnnouncement).toBeNull();
    expect(recovered.politeAnnouncement).toBeNull();
  });

  it("enables mutating commands only while the stream is connected", () => {
    expect(commandsEnabled("connected")).toBe(true);
    expect(commandsEnabled("connecting")).toBe(false);
    expect(commandsEnabled("reconnecting")).toBe(false);
    expect(commandsEnabled("reconciling")).toBe(false);
    expect(commandsEnabled("offline")).toBe(false);
  });

  it("treats CONNECTING recovery as reconnecting and CLOSED as offline", () => {
    const connected = { ...createSessionRuntimeView("active"), connectionState: "connected" as const };
    expect(markReconnecting(connected).connectionState).toBe("reconnecting");
    expect(markOffline(connected).connectionState).toBe("offline");
    expect(commandsEnabled(markReconnecting(connected).connectionState)).toBe(false);
    expect(commandsEnabled(markOffline(connected).connectionState)).toBe(false);
  });

  it("records Incomplete when a streaming Agent message is terminalized", () => {
    let view = createSessionRuntimeView("active");
    view = applySseEvent(view, {
      schema_version: "v1",
      event_type: "session.agent.fragment.v1",
      session_id: "sess.synthetic.0001",
      session_sequence: "4",
      occurred_at: "2026-08-16T00:00:00Z",
      payload: {
        summary: "Agent response fragment.",
        agent_message_id: "msg.synthetic.agent.1",
        text_delta: "Visible prefix. ",
        turn_id: "turn.synthetic.0001",
      },
    });
    view = applySseEvent(view, {
      schema_version: "v1",
      event_type: "session.terminal.v1",
      session_id: "sess.synthetic.0001",
      session_sequence: "5",
      occurred_at: "2026-08-16T00:00:01Z",
      payload: { summary: "Session completed." },
    });

    expect(view.streamedMessages).toEqual([
      {
        id: "msg.synthetic.agent.1",
        turnId: "turn.synthetic.0001",
        content: "Visible prefix. ",
        status: "incomplete",
      },
    ]);
    expect(view.turnPhase).toBe("cancelled");
    expect(transcriptStatusLabel({ streaming: false, status: "incomplete" })).toBe("Incomplete");
    expect(transcriptStatusLabel({ streaming: false, status: "confirmed" })).toBe("Complete");
  });

  it("commits only the latest same-Session projection that is not older than the committed version", () => {
    const committed = { session_id: "sess.synthetic.0001", session_version: 5, last_sequence: "9" };
    const incomingNewer = {
      session_id: "sess.synthetic.0001",
      session_version: 5,
      last_sequence: "10",
    };
    expect(
      evaluateProjectionCommit({
        requestId: 2,
        latestRequestId: 2,
        requestedSessionId: "sess.synthetic.0001",
        incoming: incomingNewer,
        committed,
      }),
    ).toBe("commit");
    expect(
      evaluateProjectionCommit({
        requestId: 1,
        latestRequestId: 2,
        requestedSessionId: "sess.synthetic.0001",
        incoming: { session_id: "sess.synthetic.0001", session_version: 4, last_sequence: "8" },
        committed,
      }),
    ).toBe("superseded");
    expect(
      evaluateProjectionCommit({
        requestId: 3,
        latestRequestId: 3,
        requestedSessionId: "sess.synthetic.0001",
        incoming: { session_id: "sess.synthetic.0001", session_version: 4, last_sequence: "20" },
        committed,
      }),
    ).toBe("stale");
    expect(
      evaluateProjectionCommit({
        requestId: 4,
        latestRequestId: 4,
        requestedSessionId: "sess.synthetic.0002",
        incoming: { session_id: "sess.synthetic.0001", session_version: 9, last_sequence: "30" },
        committed: null,
      }),
    ).toBe("superseded");
  });

  it("marks connected after reconcile only when the same EventSource epoch is still open", () => {
    expect(
      canMarkConnectedAfterReconcile({
        reconcileEpoch: 2,
        currentEpoch: 2,
        readyState: 1,
        openReadyState: 1,
      }),
    ).toBe(true);
    expect(
      canMarkConnectedAfterReconcile({
        reconcileEpoch: 2,
        currentEpoch: 2,
        readyState: 2,
        openReadyState: 1,
      }),
    ).toBe(false);
    expect(
      canMarkConnectedAfterReconcile({
        reconcileEpoch: 2,
        currentEpoch: 3,
        readyState: 1,
        openReadyState: 1,
      }),
    ).toBe(false);
  });

  it("requires projection reconcile on OPEN after any connectivity failure, including before the first OPEN", () => {
    expect(shouldReconcileProjectionOnOpen(false)).toBe(false);
    expect(shouldReconcileProjectionOnOpen(true)).toBe(true);
  });

  it("treats a Session action as current only for the same Session and generation", () => {
    expect(
      isCurrentSessionAction({
        startedSessionId: "sess.a",
        liveSessionId: "sess.a",
        startedGeneration: 1,
        liveGeneration: 1,
      }),
    ).toBe(true);
    expect(
      isCurrentSessionAction({
        startedSessionId: "sess.a",
        liveSessionId: "sess.b",
        startedGeneration: 1,
        liveGeneration: 2,
      }),
    ).toBe(false);
    expect(
      isCurrentSessionAction({
        startedSessionId: "sess.b",
        liveSessionId: "sess.b",
        startedGeneration: 1,
        liveGeneration: 2,
      }),
    ).toBe(false);
  });

  it("treats identity reconciliation as current only for the same Session, generation, and retained key", () => {
    const pending = { actionId: "send_message", key: "key-a", retainIdentity: true };
    expect(
      isCurrentCommandIdentityReconciliation({
        startedSessionId: "sess.a",
        liveSessionId: "sess.a",
        startedGeneration: 1,
        liveGeneration: 1,
        startedKey: "key-a",
        livePending: pending,
      }),
    ).toBe(true);
    expect(
      isCurrentCommandIdentityReconciliation({
        startedSessionId: "sess.a",
        liveSessionId: "sess.b",
        startedGeneration: 1,
        liveGeneration: 2,
        startedKey: "key-a",
        livePending: null,
      }),
    ).toBe(false);
    expect(
      isCurrentCommandIdentityReconciliation({
        startedSessionId: "sess.a",
        liveSessionId: "sess.a",
        startedGeneration: 1,
        liveGeneration: 1,
        startedKey: "key-a",
        livePending: { actionId: "send_message", key: "key-b", retainIdentity: true },
      }),
    ).toBe(false);
  });

  it("classifies dispatched command outcomes so only confirmed pre-commit failures stay freely actionable", () => {
    expect(classifyCommandAdmission({ threw: false, outcome: "succeeded" })).toBe("succeeded");
    expect(classifyCommandAdmission({ threw: true })).toBe("uncertain");
    expect(classifyCommandAdmission({ threw: false, outcome: "uncertain", permittedRecoveryAction: "reconcile" })).toBe(
      "uncertain",
    );
    expect(classifyCommandAdmission({ threw: false, outcome: "conflict", permittedRecoveryAction: "reconcile" })).toBe(
      "conflict",
    );
    expect(classifyCommandAdmission({ threw: false, outcome: "denied" })).toBe("access_loss");
    expect(classifyCommandAdmission({ threw: false, outcome: "validation_failed", permittedRecoveryAction: "retry" })).toBe(
      "pre_commit_rejection",
    );
    expect(shouldRetainCommandIdentity("uncertain")).toBe(true);
    expect(shouldRetainCommandIdentity("conflict")).toBe(false);
    expect(shouldRetainCommandIdentity("pre_commit_rejection")).toBe(false);
    expect(effectForCommandIdentityOutcome("accepted")).toBe("clear_accepted");
    expect(effectForCommandIdentityOutcome("still_pending")).toBe("keep_checking");
    expect(effectForCommandIdentityOutcome("confirmed_not_committed")).toBe("retire_uncommitted");
    expect(effectForCommandIdentityOutcome("conflict")).toBe("retire_conflict");
  });
});

describe("containsForbiddenControlCopy", () => {
  it("rejects internal Decision and timer vocabulary", () => {
    expect(containsForbiddenControlCopy("no_action")).toBe(true);
    expect(containsForbiddenControlCopy("requested_action delay")).toBe(true);
    expect(containsForbiddenControlCopy("revision_id 1")).toBe(true);
    expect(containsForbiddenControlCopy("No Agent reply for this turn")).toBe(false);
  });
});
