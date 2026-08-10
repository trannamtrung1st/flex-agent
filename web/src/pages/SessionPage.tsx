import { useCallback, useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { PermittedActionV1, SessionProjectionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { ErrorSummary } from "../components/ui/ErrorSummary";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { SafeContent } from "../components/ui/SafeContent";
import { createIdempotencyKey, mapActionToCommand } from "../utils/commands";

interface AgentStreamItem {
  id: string;
  content: string;
  status: string;
}

export function SessionPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const { fetchJson, executeCommand } = useBrowserApi();
  const [session, setSession] = useState<SessionProjectionV1 | null>(null);
  const [messageText, setMessageText] = useState("");
  const [agentItems, setAgentItems] = useState<AgentStreamItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);
  const eventSourceRef = useRef<EventSource | null>(null);
  const seenSequencesRef = useRef<Set<string>>(new Set());

  const loadSession = useCallback(async () => {
    if (!sessionId) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const projection = await fetchJson<SessionProjectionV1>(`/browser/sessions/${sessionId}`);
      setSession(projection);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load session");
    } finally {
      setLoading(false);
    }
  }, [sessionId, fetchJson]);

  useEffect(() => {
    void loadSession();
  }, [loadSession]);

  useEffect(() => {
    if (!sessionId || !session || session.lifecycle_state !== "active") {
      return;
    }

    const source = new EventSource(`/browser/sessions/${sessionId}/events`);
    eventSourceRef.current = source;

    source.onmessage = (event: MessageEvent<string>) => {
      try {
        const payload = JSON.parse(event.data) as {
          event_type?: string;
          session_sequence?: string;
          payload?: { text_delta?: string; agent_message_id?: string };
        };

        const sequence = payload.session_sequence ?? event.lastEventId;
        if (sequence && seenSequencesRef.current.has(sequence)) {
          return;
        }

        if (sequence) {
          seenSequencesRef.current.add(sequence);
        }

        const content = payload.payload?.text_delta ?? "";
        const messageId = payload.payload?.agent_message_id ?? crypto.randomUUID();
        const status =
          payload.event_type === "session.agent.complete.v1" ? "confirmed" : "streaming";

        setAgentItems((current) => {
          const existing = current.find((item) => item.id === messageId);
          if (existing) {
            return current.map((item) =>
              item.id === messageId ? { ...item, content: item.content + content, status } : item,
            );
          }

          return [...current, { id: messageId, content, status }];
        });
      } catch {
        // Ignore malformed SSE payloads in synthetic adapter.
      }
    };

    return () => {
      source.close();
      eventSourceRef.current = null;
    };
  }, [sessionId, session?.lifecycle_state]);

  const runAction = async (action: PermittedActionV1) => {
    if (!session || !sessionId) {
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

  if (loading) {
    return <ProtectedLoading label="Loading session…" />;
  }

  if (error || !session) {
    return <Alert variant="danger" title="Could not load session">{error ?? "Session not found"}</Alert>;
  }

  const canSend = session.permitted_actions.some((action) => action.action_id === "send_message");

  return (
    <div>
      <header className="page-header">
        <h1>Session</h1>
        <p>
          <Badge variant="brand">{session.lifecycle_state}</Badge>
          {session.remaining_time ? <span> · {session.remaining_time} remaining</span> : null}
        </p>
        {session.bound_submission_summary ? <p>{session.bound_submission_summary}</p> : null}
      </header>

      {actionError ? <ErrorSummary errors={[actionError]} /> : null}

      <div className="session-layout">
        <section aria-labelledby="transcript-heading">
          <h2 id="transcript-heading">Transcript</h2>
          <div className="transcript-panel" role="log" aria-live="polite" aria-relevant="additions">
            {session.transcript.length === 0 && agentItems.length === 0 ? (
              <p className="empty-state">No messages yet.</p>
            ) : (
              <>
                {session.transcript.map((item) => (
                  <article key={item.item_id} className="transcript-item">
                    <p className="transcript-role">{item.role}</p>
                    <SafeContent>
                      <p>{item.content}</p>
                    </SafeContent>
                    <Badge variant="default">{item.status}</Badge>
                  </article>
                ))}
                {agentItems.map((item) => (
                  <article key={item.id} className="transcript-item">
                    <p className="transcript-role">agent</p>
                    <SafeContent>
                      <p>{item.content}</p>
                    </SafeContent>
                    <Badge variant={item.status === "streaming" ? "info" : "success"}>{item.status}</Badge>
                  </article>
                ))}
              </>
            )}
          </div>
        </section>

        <aside aria-labelledby="session-controls-heading">
          <h2 id="session-controls-heading">Controls</h2>
          {canSend ? (
            <div className="composer-row field">
              <label className="sr-only" htmlFor="session-message">Message</label>
              <textarea
                id="session-message"
                className="textarea"
                value={messageText}
                onChange={(event) => { setMessageText(event.target.value); }}
                placeholder="Type your message"
                rows={3}
              />
            </div>
          ) : null}

          <div className="action-row" role="group" aria-label="Session actions">
            {session.permitted_actions.map((action) => (
              <Button
                key={action.action_id}
                variant={action.is_destructive ? "danger" : "primary"}
                onClick={() => void runAction(action)}
                disabled={
                  pending ||
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
