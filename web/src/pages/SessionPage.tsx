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
  OFFLINE_COPY,
  RECONNECTING_COPY,
  RECONCILING_COPY,
  applySseEvent,
  commandsEnabled,
  createSessionRuntimeView,
  markConnected,
  markOffline,
  markReconciling,
  markReconnecting,
  presenceForLifecycle,
  requiresSessionProjectionReconcile,
  type AgentPresenceState,
  type SessionRuntimeView,
} from "../session/sessionRuntimeView";
import { createIdempotencyKey, mapActionToCommand } from "../utils/commands";

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

function transcriptStatusLabel(item: { streaming: boolean; status: string }): string | null {
  if (item.streaming) {
    return AGENT_RESPONDING_COPY;
  }
  if (item.status === "accepted") {
    return "Message accepted";
  }
  return null;
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
  const { fetchJson, executeCommand } = useBrowserApi();
  const [session, setSession] = useState<SessionProjectionV1 | null>(null);
  const [messageText, setMessageText] = useState("");
  const [runtime, setRuntime] = useState<SessionRuntimeView>(() => createSessionRuntimeView("active"));
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);
  const eventSourceRef = useRef<EventSource | null>(null);
  const eventSourceEpochRef = useRef(0);
  const hasLoadedSessionRef = useRef(false);
  const hasConnectedOnceRef = useRef(false);
  const [streamRetryKey, setStreamRetryKey] = useState(0);
  const runtimeRef = useRef(runtime);
  runtimeRef.current = runtime;

  const loadSession = useCallback(async (): Promise<SessionProjectionV1 | null> => {
    if (!sessionId) {
      return null;
    }

    if (!hasLoadedSessionRef.current) {
      setLoading(true);
    }
    setError(null);

    try {
      const projection = await fetchJson<SessionProjectionV1>(`/browser/sessions/${sessionId}`);
      hasLoadedSessionRef.current = true;
      setSession(projection);
      return projection;
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Failed to load session";
      if (message === "protected" || message === "Access denied" || message.includes("Access")) {
        setError(SESSION_UNAVAILABLE_COPY);
      } else {
        setError(message);
      }
      setSession(null);
      return null;
    } finally {
      setLoading(false);
    }
  }, [sessionId, fetchJson]);

  const reconcileSession = useCallback(async () => {
    const projection = await loadSession();
    if (projection) {
      setRuntime((current) => markConnected(current));
    }
  }, [loadSession]);

  useEffect(() => {
    hasLoadedSessionRef.current = false;
    hasConnectedOnceRef.current = false;
    setRuntime(createSessionRuntimeView("active"));
  }, [sessionId]);

  useEffect(() => {
    void loadSession();
  }, [loadSession]);

  useEffect(() => {
    if (!sessionId || !session || session.lifecycle_state === "completed" || session.lifecycle_state === "terminated" || session.lifecycle_state === "aborted") {
      return;
    }

    const epoch = ++eventSourceEpochRef.current;
    const source = new EventSource(`/browser/sessions/${sessionId}/events`);
    eventSourceRef.current = source;

    source.onopen = () => {
      if (eventSourceEpochRef.current !== epoch) {
        return;
      }

      if (!hasConnectedOnceRef.current) {
        hasConnectedOnceRef.current = true;
        setRuntime((current) => markConnected(current));
        return;
      }

      setRuntime((current) => markReconciling(current));
      void reconcileSession();
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
        setRuntime((current) => markReconnecting(current));
        return;
      }
      if (source.readyState === EventSource.CLOSED) {
        setRuntime((current) => markOffline(current));
      }
    };

    return () => {
      source.close();
      if (eventSourceRef.current === source) {
        eventSourceRef.current = null;
      }
    };
    // Reconnect when identity, lifecycle, version, or an explicit retry changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- session object identity is not the subscription key
  }, [sessionId, session?.lifecycle_state, session?.session_version, streamRetryKey, reconcileSession]);

  const sendAction = session?.permitted_actions.find((action) => action.action_id === "send_message");

  const runAction = async (action: PermittedActionV1) => {
    if (!session || !sessionId || !commandsEnabled(runtimeRef.current.connectionState)) {
      return;
    }

    const commandType = mapActionToCommand(action.action_id);
    if (!commandType) {
      return;
    }

    setPending(true);
    setActionError(null);

    try {
      const payload =
        action.action_id === "send_message" ? { message_text: messageText } : undefined;

      const result = await executeCommand({
        command_id: action.action_id,
        idempotency_key: createIdempotencyKey(),
        command_type: commandType,
        resource_id: session.session_id,
        expected_version: session.session_version,
        payload,
      });

      if (result.outcome !== "succeeded") {
        setActionError(result.safe_message ?? "Action could not be completed.");
      } else {
        if (action.action_id === "send_message") {
          setMessageText("");
        }
        await loadSession();
      }
    } catch (err: unknown) {
      setActionError(err instanceof Error ? err.message : "Action failed");
    } finally {
      setPending(false);
    }
  };

  const onComposerKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key !== "Enter" || !(event.ctrlKey || event.metaKey) || !sendAction) {
      return;
    }

    event.preventDefault();
    if (!pending && messageText.trim() && commandsEnabled(runtime.connectionState)) {
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
  const mutationsEnabled = commandsEnabled(runtime.connectionState);
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

      {runtime.connectionState === "reconciling" ? (
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
              setRuntime((current) => markReconnecting(current));
              setStreamRetryKey((current) => current + 1);
            }}
          >
            Try reconnecting
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
