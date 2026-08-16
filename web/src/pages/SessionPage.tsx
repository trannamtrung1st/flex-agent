import { useCallback, useEffect, useMemo, useRef, useState, type KeyboardEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { PermittedActionV1, SessionProjectionV1, SessionTranscriptItemV1 } from "../api/browser-contracts";
import { AgentPresence } from "../components/session/AgentPresence";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { ErrorSummary } from "../components/ui/ErrorSummary";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { SafeContent } from "../components/ui/SafeContent";
import type { SseSessionEventV1 } from "../contracts/v1";
import {
  AGENT_PREPARING_COPY,
  AGENT_RESPONDING_COPY,
  CHECKING_MESSAGE_STATUS_COPY,
  MESSAGE_NOT_ACCEPTED_COPY,
  OFFLINE_COPY,
  PROJECTION_RETRY_COPY,
  RECONNECTING_COPY,
  RECONCILING_COPY,
  applySseEvent,
  canMarkConnectedAfterReconcile,
  classifyCommandAdmission,
  commandsEnabled,
  createSessionRuntimeView,
  effectForCommandIdentityOutcome,
  evaluateProjectionCommit,
  isCurrentSessionAction,
  isSessionAccessLoss,
  markConnected,
  markOffline,
  markReconciling,
  markReconnecting,
  presenceForLifecycle,
  requiresSessionProjectionReconcile,
  shouldReconcileProjectionOnOpen,
  shouldRetainCommandIdentity,
  transcriptStatusLabel,
  type AgentPresenceState,
  type SessionRuntimeView,
} from "../session/sessionRuntimeView";
import { createIdempotencyKey, mapActionToCommand } from "../utils/commands";

function isAbortError(error: unknown): boolean {
  return typeof error === "object" && error !== null && "name" in error && (error as { name: string }).name === "AbortError";
}

function isTerminalLifecycle(lifecycleState: string): boolean {
  return lifecycleState === "completed" || lifecycleState === "terminated" || lifecycleState === "aborted";
}

const SESSION_UNAVAILABLE_COPY =
  "This Session is not available. Return to My work or use the provided support route.";

function authorLabel(role: string): string {
  if (role === "participant") {
    return "You";
  }
  if (role === "agent") {
    return "Agent";
  }
  return role;
}

function lifecycleLabel(state: string): string {
  switch (state) {
    case "active":
      return "Active";
    case "paused":
      return "Session paused";
    case "completed":
      return "Session completed";
    case "terminated":
      return "Session terminated";
    case "aborted":
      return "Session aborted";
    default:
      return state;
  }
}

function composerDisabledReason(lifecycleState: string): string {
  if (lifecycleState === "paused") {
    return "Session active time is paused. Sending is unavailable until the Session resumes.";
  }
  if (lifecycleState === "completed") {
    return "Session completed. No more messages are accepted.";
  }
  if (lifecycleState === "terminated" || lifecycleState === "aborted") {
    return "This Session is no longer accepting messages.";
  }
  return "Sending is unavailable in the current Session state.";
}

function visiblePresence(lifecycleState: string, view: SessionRuntimeView): AgentPresenceState {
  if (
    view.agentPresence === "dormant" ||
    lifecycleState === "completed" ||
    lifecycleState === "terminated" ||
    lifecycleState === "aborted"
  ) {
    return "dormant";
  }

  return presenceForLifecycle(lifecycleState, view.turnPhase);
}

function activityLabelFor(view: SessionRuntimeView): string | null {
  if (view.agentPresence === "dormant" || view.turnPhase === "cancelled") {
    return null;
  }
  if (view.turnPhase === "queued" || view.turnPhase === "working") {
    return AGENT_PREPARING_COPY;
  }
  if (view.turnPhase === "streaming") {
    return AGENT_RESPONDING_COPY;
  }
  return null;
}

function mergeTranscript(
  projectionItems: SessionTranscriptItemV1[],
  view: SessionRuntimeView,
): Array<{ id: string; role: string; content: string; status: string; streaming: boolean }> {
  const projectedIds = new Set(projectionItems.map((item) => item.item_id));
  const projected = projectionItems.map((item) => ({
    id: item.item_id,
    role: item.role,
    content: item.content,
    status: item.status,
    streaming: false,
  }));
  const streamed = view.streamedMessages
    .filter((item) => !projectedIds.has(item.id) && item.content.length > 0)
    .map((item) => ({
      id: item.id,
      role: "agent",
      content: item.content,
      status: item.status,
      streaming: item.status === "streaming",
    }));
  return [...projected, ...streamed];
}

export function SessionPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const { fetchJson, executeCommand, reconcileCommand } = useBrowserApi();
  const [session, setSession] = useState<SessionProjectionV1 | null>(null);
  const [messageText, setMessageText] = useState("");
  const [runtime, setRuntime] = useState<SessionRuntimeView>(() => createSessionRuntimeView("active"));
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);
  const [projectionError, setProjectionError] = useState<string | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);
  const eventSourceRef = useRef<EventSource | null>(null);
  const eventSourceEpochRef = useRef(0);
  const projectionRequestIdRef = useRef(0);
  const committedProjectionRef = useRef<{
    session_id: string;
    session_version: number;
    last_sequence?: string | null;
  } | null>(null);
  const hasLoadedSessionRef = useRef(false);
  const hasSeenConnectivityFailureRef = useRef(false);
  const actionGenerationRef = useRef(0);
  const pendingCommandRef = useRef<{
    actionId: string;
    key: string;
    draft: string;
    retainIdentity: boolean;
  } | null>(null);
  const sessionIdRef = useRef(sessionId);
  sessionIdRef.current = sessionId;
  const [streamRetryKey, setStreamRetryKey] = useState(0);
  const [checkingMessage, setCheckingMessage] = useState(false);
  const runtimeRef = useRef(runtime);
  runtimeRef.current = runtime;

  const loadSession = useCallback(async (): Promise<SessionProjectionV1 | null> => {
    if (!sessionId) {
      return null;
    }

    const requestedSessionId = sessionId;
    const requestId = ++projectionRequestIdRef.current;
    if (!hasLoadedSessionRef.current) {
      setLoading(true);
    }

    try {
      const projection = await fetchJson<SessionProjectionV1>(`/browser/sessions/${requestedSessionId}`, {
        signal: abortControllerRef.current?.signal,
      });
      const decision = evaluateProjectionCommit({
        requestId,
        latestRequestId: projectionRequestIdRef.current,
        requestedSessionId: sessionIdRef.current ?? requestedSessionId,
        incoming: projection,
        committed: committedProjectionRef.current,
      });
      if (decision === "superseded") {
        return null;
      }
      if (decision === "stale") {
        if (hasLoadedSessionRef.current) {
          setProjectionError(PROJECTION_RETRY_COPY);
        }
        return null;
      }

      committedProjectionRef.current = {
        session_id: projection.session_id,
        session_version: projection.session_version,
        last_sequence: projection.last_sequence,
      };
      hasLoadedSessionRef.current = true;
      setSession(projection);
      setError(null);
      setProjectionError(null);
      return projection;
    } catch (err: unknown) {
      if (isAbortError(err) || requestId !== projectionRequestIdRef.current) {
        return null;
      }

      const message = err instanceof Error ? err.message : "Failed to load session";
      if (isSessionAccessLoss(message)) {
        setError(SESSION_UNAVAILABLE_COPY);
        setSession(null);
        committedProjectionRef.current = null;
        return null;
      }

      if (hasLoadedSessionRef.current) {
        setProjectionError(PROJECTION_RETRY_COPY);
        return null;
      }

      setError(message);
      setSession(null);
      return null;
    } finally {
      if (requestId === projectionRequestIdRef.current) {
        setLoading(false);
      }
    }
  }, [sessionId, fetchJson]);

  const applyPendingCommandIdentity = useCallback(async () => {
    const pendingSend = pendingCommandRef.current;
    const liveSessionId = sessionIdRef.current;
    if (!pendingSend?.retainIdentity || pendingSend.actionId !== "send_message" || !liveSessionId) {
      return;
    }

    try {
      const result = await reconcileCommand({
        command_id: pendingSend.actionId,
        idempotency_key: pendingSend.key,
        command_type: "session.send_message",
        resource_id: liveSessionId,
      });
      const effect = effectForCommandIdentityOutcome(result.outcome);
      if (effect === "clear_accepted") {
        setMessageText("");
        pendingCommandRef.current = null;
        setCheckingMessage(false);
        return;
      }
      if (effect === "keep_checking") {
        setCheckingMessage(true);
        return;
      }
      if (effect === "retain_uncommitted") {
        setCheckingMessage(false);
        return;
      }
      if (effect === "retire_conflict") {
        pendingCommandRef.current = null;
        setCheckingMessage(false);
        setActionError(result.safe_message ?? "Action could not be completed.");
        return;
      }

      pendingCommandRef.current = null;
      setCheckingMessage(false);
      setError(SESSION_UNAVAILABLE_COPY);
      setSession(null);
      committedProjectionRef.current = null;
    } catch {
      setCheckingMessage(true);
    }
  }, [reconcileCommand]);

  const reconcileSession = useCallback(async () => {
    const epoch = eventSourceEpochRef.current;
    const source = eventSourceRef.current;
    const projection = await loadSession();
    if (!projection) {
      return null;
    }

    if (
      eventSourceRef.current !== source ||
      !canMarkConnectedAfterReconcile({
        reconcileEpoch: epoch,
        currentEpoch: eventSourceEpochRef.current,
        readyState: source?.readyState ?? -1,
        openReadyState: EventSource.OPEN,
      })
    ) {
      await applyPendingCommandIdentity();
      return projection;
    }

    setRuntime((current) => markConnected(current));
    await applyPendingCommandIdentity();
    return projection;
  }, [loadSession, applyPendingCommandIdentity]);

  useEffect(() => {
    const controller = new AbortController();
    abortControllerRef.current = controller;
    hasLoadedSessionRef.current = false;
    hasSeenConnectivityFailureRef.current = false;
    actionGenerationRef.current += 1;
    pendingCommandRef.current = null;
    committedProjectionRef.current = null;
    setSession(null);
    setMessageText("");
    setActionError(null);
    setCheckingMessage(false);
    setError(null);
    setProjectionError(null);
    setPending(false);
    setLoading(true);
    setRuntime(createSessionRuntimeView("active"));

    return () => {
      controller.abort();
    };
  }, [sessionId]);

  useEffect(() => {
    void loadSession();
  }, [loadSession]);

  const streamIsTerminal = Boolean(
    session && session.session_id === sessionId && isTerminalLifecycle(session.lifecycle_state),
  );

  useEffect(() => {
    if (!sessionId || !session || session.session_id !== sessionId || streamIsTerminal) {
      return;
    }

    const epoch = ++eventSourceEpochRef.current;
    const source = new EventSource(`/browser/sessions/${sessionId}/events`);
    eventSourceRef.current = source;

    source.onopen = () => {
      if (eventSourceEpochRef.current !== epoch) {
        return;
      }

      if (shouldReconcileProjectionOnOpen(hasSeenConnectivityFailureRef.current)) {
        setRuntime((current) => markReconciling(current));
        void reconcileSession();
        return;
      }

      if (source.readyState === EventSource.OPEN) {
        setRuntime((current) => markConnected(current));
      }
    };

    source.onmessage = (event: MessageEvent<string>) => {
      if (eventSourceEpochRef.current !== epoch) {
        return;
      }

      try {
        const payload = JSON.parse(event.data) as SseSessionEventV1;
        setRuntime((current) => {
          const next = applySseEvent(current, payload);
          return requiresSessionProjectionReconcile(payload.event_type) ? markReconciling(next) : next;
        });
        if (requiresSessionProjectionReconcile(payload.event_type)) {
          void reconcileSession();
        }
      } catch {
        // Ignore malformed SSE payloads in synthetic adapter.
      }
    };

    source.onerror = () => {
      if (eventSourceEpochRef.current !== epoch) {
        return;
      }
      if (source.readyState === EventSource.CONNECTING) {
        hasSeenConnectivityFailureRef.current = true;
        setRuntime((current) => markReconnecting(current));
        return;
      }
      if (source.readyState === EventSource.CLOSED) {
        hasSeenConnectivityFailureRef.current = true;
        setRuntime((current) => markOffline(current));
      }
    };

    return () => {
      source.close();
      if (eventSourceRef.current === source) {
        eventSourceRef.current = null;
      }
    };
    // Keep a healthy EventSource across projection version updates; recreate only for Session identity or explicit retry.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- session object identity is not the subscription key
  }, [sessionId, session?.session_id, streamIsTerminal, streamRetryKey, reconcileSession]);

  const sendAction = session?.permitted_actions.find((action) => action.action_id === "send_message");

  const runAction = async (action: PermittedActionV1) => {
    if (
      !session ||
      !sessionId ||
      session.session_id !== sessionId ||
      checkingMessage ||
      !commandsEnabled(runtimeRef.current.connectionState)
    ) {
      return;
    }

    const commandType = mapActionToCommand(action.action_id);
    if (!commandType) {
      return;
    }

    const startedSessionId = sessionId;
    const startedGeneration = actionGenerationRef.current;
    const actionStillCurrent = () =>
      isCurrentSessionAction({
        startedSessionId,
        liveSessionId: sessionIdRef.current,
        startedGeneration,
        liveGeneration: actionGenerationRef.current,
      });

    const draft = action.action_id === "send_message" ? messageText : "";
    const retainedCommand = pendingCommandRef.current;
    const idempotencyKey =
      retainedCommand?.retainIdentity &&
      retainedCommand.actionId === action.action_id &&
      retainedCommand.draft === draft
        ? retainedCommand.key
        : createIdempotencyKey();
    pendingCommandRef.current = { actionId: action.action_id, key: idempotencyKey, draft, retainIdentity: false };

    setPending(true);
    setActionError(null);

    const settleDispatchedCommand = async (
      kind: ReturnType<typeof classifyCommandAdmission>,
      safeMessage: string | null,
    ) => {
      if (kind === "pre_commit_rejection" || kind === "conflict" || kind === "access_loss") {
        pendingCommandRef.current = null;
        setCheckingMessage(false);
        setActionError(
          action.action_id === "send_message" && kind === "pre_commit_rejection"
            ? (safeMessage ?? MESSAGE_NOT_ACCEPTED_COPY)
            : (safeMessage ?? "Action could not be completed."),
        );
      } else if (kind === "succeeded") {
        pendingCommandRef.current = null;
        setCheckingMessage(false);
        if (action.action_id === "send_message") {
          setMessageText("");
        }
      } else if (kind === "uncertain" && action.action_id === "send_message") {
        pendingCommandRef.current = {
          actionId: action.action_id,
          key: idempotencyKey,
          draft,
          retainIdentity: shouldRetainCommandIdentity(kind),
        };
        setCheckingMessage(true);
        setActionError(null);
      } else {
        pendingCommandRef.current = null;
        setCheckingMessage(false);
        setActionError(safeMessage ?? "Action could not be completed.");
      }

      setRuntime((current) => markReconciling(current));
      if (!actionStillCurrent()) {
        return;
      }

      await reconcileSession();
    };

    try {
      const payload = action.action_id === "send_message" ? { message_text: draft } : undefined;

      const result = await executeCommand({
        command_id: action.action_id,
        idempotency_key: idempotencyKey,
        command_type: commandType,
        resource_id: startedSessionId,
        expected_version: session.session_version,
        payload,
      });

      if (!actionStillCurrent()) {
        return;
      }

      await settleDispatchedCommand(
        classifyCommandAdmission({
          threw: false,
          outcome: result.outcome,
          permittedRecoveryAction: result.permitted_recovery_action,
        }),
        result.safe_message ?? null,
      );
    } catch (err: unknown) {
      if (!actionStillCurrent()) {
        return;
      }
      await settleDispatchedCommand(
        classifyCommandAdmission({ threw: true }),
        err instanceof Error ? err.message : "Action failed",
      );
    } finally {
      if (actionStillCurrent()) {
        setPending(false);
      }
    }
  };

  const onComposerKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key !== "Enter" || !(event.ctrlKey || event.metaKey) || !sendAction) {
      return;
    }

    event.preventDefault();
    if (!pending && !checkingMessage && messageText.trim() && commandsEnabled(runtime.connectionState)) {
      void runAction(sendAction);
    }
  };

  const transcriptItems = useMemo(
    () => (session ? mergeTranscript(session.transcript, runtime) : []),
    [session, runtime],
  );

  if (loading) {
    return <ProtectedLoading label="Loading session…" />;
  }

  if (error || !session) {
    return (
      <Alert variant="danger" title="Session unavailable">
        <p>{error ?? SESSION_UNAVAILABLE_COPY}</p>
        <p>
          <Link to="/my-work">Return to My work</Link>
        </p>
      </Alert>
    );
  }

  const canSend = Boolean(sendAction);
  const mutationsEnabled = commandsEnabled(runtime.connectionState) && !checkingMessage;
  const presence = visiblePresence(session.lifecycle_state, runtime);
  const activityLabel = activityLabelFor(runtime);

  return (
    <div className="session-page">
      <header className="page-header">
        <h1>Session</h1>
        <p>
          <Badge variant="brand">{lifecycleLabel(session.lifecycle_state)}</Badge>
          {session.remaining_time ? (
            <span>
              {" "}
              · <span className="session-time">Time remaining {session.remaining_time}</span>
            </span>
          ) : null}
        </p>
        {session.bound_submission_summary ? <p>{session.bound_submission_summary}</p> : null}
      </header>

      <AgentPresence state={presence} activityLabel={activityLabel} />

      <div className="session-live-regions">
        <p
          className="sr-only"
          role="status"
          aria-live="polite"
          aria-atomic="true"
          aria-label="Session updates"
        >
          {runtime.politeAnnouncement ?? ""}
        </p>
        <p className="sr-only" role="alert" aria-live="assertive" aria-atomic="true">
          {runtime.assertiveAnnouncement ?? ""}
        </p>
      </div>

      {runtime.connectionState === "reconnecting" ? (
        <Alert variant="warning" title="Reconnecting">
          {RECONNECTING_COPY}
        </Alert>
      ) : null}

      {checkingMessage ? (
        <Alert variant="info" title="Checking message status">
          {CHECKING_MESSAGE_STATUS_COPY}
        </Alert>
      ) : runtime.connectionState === "reconciling" ? (
        <Alert variant="info" title="Updating Session">
          {RECONCILING_COPY}
        </Alert>
      ) : null}

      {runtime.connectionState === "offline" ? (
        <Alert variant="warning" title="Disconnected">
          <p>{OFFLINE_COPY}</p>
          <Button
            variant="secondary"
            onClick={() => {
              hasSeenConnectivityFailureRef.current = true;
              setRuntime((current) => markReconnecting(current));
              setStreamRetryKey((current) => current + 1);
            }}
          >
            Try reconnecting
          </Button>
        </Alert>
      ) : null}

      {projectionError ? (
        <Alert variant="warning" title="Could not update Session">
          <p>{projectionError}</p>
          <Button
            variant="secondary"
            onClick={() => {
              const source = eventSourceRef.current;
              if (!source || source.readyState !== EventSource.OPEN) {
                hasSeenConnectivityFailureRef.current = true;
                setRuntime((current) => markReconnecting(current));
                setStreamRetryKey((current) => current + 1);
                return;
              }

              setRuntime((current) => markReconciling(current));
              void reconcileSession();
            }}
          >
            Try again
          </Button>
        </Alert>
      ) : null}

      {actionError ? <ErrorSummary errors={[actionError]} /> : null}

      <div className="session-layout">
        <section aria-labelledby="transcript-heading">
          <h2 id="transcript-heading">Transcript</h2>
          <div className="transcript-panel" role="log" aria-live="off" aria-relevant="additions">
            {transcriptItems.length === 0 && !runtime.persistentTurnStatus ? (
              <p className="empty-state">No messages yet. Send a message when you are ready.</p>
            ) : (
              <>
                {transcriptItems.map((item) => {
                  const statusLabel = transcriptStatusLabel(item);
                  return (
                  <article
                    key={item.id}
                    className={`transcript-item transcript-item-${item.role}${item.streaming ? " transcript-item-streaming" : ""}`}
                  >
                    <p className="transcript-role">{authorLabel(item.role)}</p>
                    <SafeContent>
                      <p className="transcript-content">{item.content}</p>
                    </SafeContent>
                    {statusLabel ? (
                      <Badge variant={item.streaming ? "info" : "default"}>{statusLabel}</Badge>
                    ) : null}
                  </article>
                  );
                })}
                {runtime.persistentTurnStatus ? (
                  <p className="turn-status" role="status">
                    {runtime.persistentTurnStatus}
                  </p>
                ) : null}
              </>
            )}
          </div>
        </section>

        <aside aria-labelledby="session-controls-heading">
          <h2 id="session-controls-heading">Controls</h2>
          {canSend ? (
            <div className="composer-row field">
              <label htmlFor="session-message">Your message</label>
              <textarea
                id="session-message"
                className="textarea"
                value={messageText}
                readOnly={checkingMessage}
                onChange={(event) => {
                  setMessageText(event.target.value);
                }}
                onKeyDown={onComposerKeyDown}
                placeholder="Not sent. The Agent cannot see this draft."
                rows={3}
              />
              <p className="field-hint">
                {mutationsEnabled
                  ? "Enter adds a new line. Ctrl+Enter or Command+Enter sends."
                  : "Draft is kept locally. Sending is unavailable until the Session connection is restored."}
              </p>
            </div>
          ) : (
            <p className="composer-disabled-reason">{composerDisabledReason(session.lifecycle_state)}</p>
          )}

          <div className="action-row" role="group" aria-label="Session actions">
            {session.permitted_actions.map((action) => (
              <Button
                key={action.action_id}
                variant={action.is_destructive ? "danger" : "primary"}
                onClick={() => void runAction(action)}
                disabled={
                  pending ||
                  !mutationsEnabled ||
                  (action.action_id === "send_message" && !messageText.trim())
                }
              >
                {action.label}
              </Button>
            ))}
          </div>
        </aside>
      </div>

      <p className="page-section">
        <Link to="/my-work">Back to my work</Link>
      </p>
    </div>
  );
}
