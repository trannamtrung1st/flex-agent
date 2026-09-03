import { useCallback, useEffect, useMemo, useReducer, useRef, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useProductionApi } from "../api/production-api";
import {
  createProductionSessionClient,
  createSessionCommandId,
  createSessionIdempotencyKey,
} from "../api/production-session";
import { SessionChrono } from "../components/work/SessionChrono";
import { TextSessionStation } from "../components/work/TextSessionStation";
import { sessionKeys } from "../features/session/queryKeys";
import { emptySessionLiveView, sessionLiveReducer } from "../features/session/session-view";
import type { SessionCommandEnvelopeV1, SessionHostedEventEnvelopeV1, SessionSnapshotV1 } from "../contracts/v1";
import {
  Alert,
  BrandMark,
  CeremonyDialog,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  Key,
  KeyGroup,
  ReadoutList,
  TransmitChevron,
} from "../design-system";

function can(snapshot: SessionSnapshotV1 | null, action: SessionSnapshotV1["permitted_actions"][number]) {
  return snapshot?.permitted_actions.includes(action) ?? false;
}

function examinerLine(snapshot: SessionSnapshotV1 | null, working: boolean, terminal: boolean) {
  if (terminal) {
    return "Your record is stored. This confirmation is not a score or Result.";
  }
  if (snapshot?.lifecycle_state === "completing") {
    return "The Session is sealing. Sending is closed.";
  }
  if (snapshot?.lifecycle_state === "paused") {
    return "This Session is paused. Sending stays closed until an administrator resumes it.";
  }
  if (working) {
    return "The Examiner is considering your reply.";
  }
  if (snapshot?.activity?.work_state === "failed") {
    return "Agent work did not finish. You may continue when sending is permitted.";
  }
  if (snapshot?.activity?.work_state === "no_action") {
    return "The Agent recorded no further output for this turn.";
  }
  return "Take the time you need. Awaiting your text reply.";
}

export function ProductionTextSessionPage() {
  const { sessionId = "" } = useParams();
  const { apiState, fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionSessionClient(fetchJson), [fetchJson]);
  const [view, dispatch] = useReducer(sessionLiveReducer, emptySessionLiveView);
  const [confirmComplete, setConfirmComplete] = useState(false);
  const [pendingIdempotency, setPendingIdempotency] = useState<string | null>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const sealedOnce = useRef<string | null>(null);

  const snapshotQuery = useQuery({
    queryKey: sessionKeys.snapshot(sessionId),
    queryFn: () => client.getSnapshot(sessionId),
    enabled: apiState === "ready" && sessionId.length > 0,
  });

  useEffect(() => {
    if (snapshotQuery.data) {
      dispatch({ type: "snapshot", snapshot: snapshotQuery.data });
    }
  }, [snapshotQuery.data]);

  useEffect(() => {
    if (apiState !== "ready" || !sessionId) {
      return;
    }

    const source = new EventSource(`/v1/sessions/${sessionId}/events`);
    dispatch({ type: "connection", connection: "connecting" });
    source.onopen = () => dispatch({ type: "connection", connection: "connected" });
    source.onerror = () => dispatch({ type: "connection", connection: "reconnecting" });
    source.onmessage = (message: MessageEvent<string>) => {
      try {
        const event = JSON.parse(message.data) as SessionHostedEventEnvelopeV1;
        dispatch({ type: "event", event });
      } catch {
        dispatch({ type: "error", message: "A Session update could not be applied. Refresh from the server." });
      }
    };
    return () => source.close();
  }, [apiState, sessionId]);

  const submit = useCallback(async (command: SessionCommandEnvelopeV1) => {
    dispatch({ type: "send", sendState: "pending" });
    try {
      const outcome = await client.submitCommand(sessionId, command);
      if (outcome.outcome_category === "uncertain") {
        dispatch({ type: "send", sendState: "uncertain" });
        dispatch({ type: "error", message: "The command was not confirmed. Reconcile before sending again." });
        return;
      }
      if (!outcome.succeeded && outcome.outcome_category !== "duplicate") {
        dispatch({ type: "send", sendState: "idle" });
        dispatch({ type: "error", message: "The Session could not accept that command." });
        await snapshotQuery.refetch();
        return;
      }
      dispatch({ type: "send", sendState: "idle" });
      if (command.command_type === "session.message.send.v1") {
        dispatch({ type: "draft", draft: "" });
        setPendingIdempotency(null);
      }
      await snapshotQuery.refetch();
    } catch {
      dispatch({ type: "send", sendState: "uncertain" });
      dispatch({ type: "error", message: "The command outcome is uncertain. Reconcile before sending again." });
    }
  }, [client, sessionId, snapshotQuery]);

  const snapshot = view.snapshot;
  const terminal = snapshot?.lifecycle_state === "completed"
    || snapshot?.lifecycle_state === "terminated"
    || snapshot?.lifecycle_state === "aborted";
  const completing = snapshot?.lifecycle_state === "completing";
  const paused = snapshot?.lifecycle_state === "paused";
  const working = snapshot?.activity?.work_state === "working" || snapshot?.activity?.work_state === "queued";
  const composerClosed = terminal || completing || paused || !can(snapshot, "send_message");

  useEffect(() => {
    if (!snapshot || snapshot.projection_kind !== "participant" || snapshot.lifecycle_state !== "completing") {
      return;
    }
    if (working) {
      return;
    }
    if (sealedOnce.current === snapshot.session_id) {
      return;
    }
    if (!can(snapshot, "complete_session")) {
      return;
    }
    sealedOnce.current = snapshot.session_id;
    void submit({
      schema_version: "v1",
      command_type: "session.complete.v1",
      command_id: createSessionCommandId(),
      idempotency_key: createSessionIdempotencyKey(),
      session_locator: { session_id: sessionId },
      expected_session_version: snapshot.session_version,
      payload: {},
    });
  }, [sessionId, snapshot, submit, working]);

  if (snapshotQuery.isError && !snapshot) {
    return (
      <Alert variant="danger" title="Session unavailable">
        This Session cannot be opened with the current access.
      </Alert>
    );
  }

  if (snapshotQuery.isLoading && !snapshot) {
    return (
      <Alert variant="info" title="Loading Session">
        Restoring the authoritative Session snapshot.
      </Alert>
    );
  }

  if (snapshot?.projection_kind === "administrator") {
    return (
      <Alert variant="info" title="Administrator Session view">
        Live transcript is not loaded on this route. Use Session operations for pause, resume, or terminate.
        <KeyGroup>
          <Key to={`/sessions/${sessionId}/operations`}>Open Session operations</Key>
        </KeyGroup>
      </Alert>
    );
  }

  const items = snapshot?.transcript?.items ?? [];
  const linkLabel = view.connection === "connected"
    ? "Link Nominal"
    : view.connection === "reconnecting" || view.connection === "offline"
      ? "Link Recovering"
      : "Link Connecting";

  return (
    <TextSessionStation
      homeTo="/my-work"
      homeLabel="My work"
      railLabel="Session instruments"
      brandSuffix="Examination Console"
      brandExtras={(
        <div className="rail-nav">
          <Key className="rail-back" variant="quiet" to="/my-work" ariaLabel="Back to assignment">
            Assignment
          </Key>
        </div>
      )}
      warned={snapshot?.timing?.warning_code === "imminent"}
      complete={terminal}
      instruments={(
        <ReadoutList
          label="Session status"
          rows={[
            { term: "Status", value: snapshot?.lifecycle_state ?? "Loading", emphasis: "title" },
            { term: "Connection", value: view.connection === "reconnecting" ? "Reconnecting" : view.connection },
            { term: "Work", value: snapshot?.activity?.work_state ?? "idle" },
          ]}
        />
      )}
      composer={composerClosed ? (
        <p>{paused ? "This Session is paused. Sending is closed until an administrator resumes it." : "Composer closed."}</p>
      ) : (
        <>
          <form
            className="composer"
            onSubmit={(event) => {
              event.preventDefault();
              if (!snapshot || view.sendState !== "idle" || view.draft.trim().length === 0) {
                return;
              }
              const idempotency = pendingIdempotency ?? createSessionIdempotencyKey();
              setPendingIdempotency(idempotency);
              void submit({
                schema_version: "v1",
                command_type: "session.message.send.v1",
                command_id: createSessionCommandId(),
                idempotency_key: idempotency,
                session_locator: { session_id: sessionId },
                expected_session_version: snapshot.session_version,
                payload: { message_text: view.draft },
              });
            }}
          >
            <label className="visually-hidden" htmlFor="composerInput">Compose reply</label>
            <textarea
              id="composerInput"
              ref={inputRef}
              rows={1}
              placeholder="Write your next message"
              autoComplete="off"
              spellCheck
              disabled={view.sendState !== "idle"}
              value={view.draft}
              onChange={(event) => {
                dispatch({ type: "draft", draft: event.target.value });
                event.currentTarget.style.height = "auto";
                event.currentTarget.style.height = `${Math.min(event.currentTarget.scrollHeight, 200)}px`;
              }}
              onKeyDown={(event) => {
                if (event.key !== "Enter" || event.nativeEvent.isComposing) {
                  return;
                }
                if (event.ctrlKey || event.metaKey) {
                  event.preventDefault();
                  event.currentTarget.form?.requestSubmit();
                }
              }}
            />
            <Key
              variant="transmit"
              type="submit"
              waiting={view.sendState === "pending"}
              disabled={view.sendState !== "idle" || view.draft.trim().length === 0}
            >
              <span>{view.sendState === "pending" ? "Sending" : "Transmit"}</span>
              {view.sendState === "pending" ? null : <TransmitChevron />}
            </Key>
          </form>
          <p className="composer-hint">Enter starts a new line. Control or Command plus Enter sends.</p>
          {view.sendState === "uncertain" ? (
            <Key
              variant="quiet"
              type="button"
              onClick={() => {
                if (!snapshot) return;
                void submit({
                  schema_version: "v1",
                  command_type: "session.reconcile.v1",
                  command_id: createSessionCommandId(),
                  idempotency_key: createSessionIdempotencyKey(),
                  session_locator: { session_id: sessionId },
                  expected_session_version: snapshot.session_version,
                  client_last_seen_sequence: snapshot.last_confirmed_sequence === "0"
                    ? "1"
                    : snapshot.last_confirmed_sequence,
                  payload: {},
                });
              }}
            >
              Reconcile
            </Key>
          ) : null}
          <div className="link-plate" aria-label={`Connection status: ${linkLabel}`}>
            <span className="link-label">{linkLabel}</span>
          </div>
        </>
      )}
      examiner={(
        <>
          <section className="agent-post" aria-label="Examiner">
            <div
              className={`agent-core agent-core--live1${working ? " is-thinking" : ""}`}
              role="img"
              aria-label={terminal ? "Agent core — session complete" : working ? "Agent core — considering" : "Agent core — idle"}
            >
              <span className="live1-ring" aria-hidden="true" />
              <span className="live1-shell" aria-hidden="true" />
            </div>
            <h1 className="agent-name">
              <BrandMark />
              <span className="agent-name-role">{snapshot?.agent?.display_name ?? "Examiner"}</span>
            </h1>
            <p className="agent-line">{examinerLine(snapshot, working, terminal)}</p>
          </section>
          <SessionChrono
            snapshot={snapshot}
            canSubmit={can(snapshot, "complete_session") && !terminal}
            onSubmit={() => setConfirmComplete(true)}
          />
        </>
      )}
    >
      {view.connection === "reconnecting" || view.connection === "offline" ? (
        <Alert variant="info" title="Connection recovery">
          Reconnecting to the Session. Authoritative state is restored from the server snapshot, not from this browser clock.
        </Alert>
      ) : null}
      {snapshot?.activity?.work_state === "no_action" ? (
        <Alert variant="info" title="No further Agent output">
          The Agent recorded an explicit no-action for this turn. You may continue when sending is permitted.
        </Alert>
      ) : null}
      {snapshot?.activity?.work_state === "failed" && !terminal && !completing ? (
        <Alert variant="info" title="Agent work did not finish">
          Durable work failed closed. You may continue when sending is permitted. This is not a Session completion.
        </Alert>
      ) : null}
      {view.lastError ? <Alert variant="danger" title="Session recovery">{view.lastError}</Alert> : null}
      <ol className="ledger" aria-label="Examination transcript">
        {items.map((item, index) => (
          <li key={item.item_id} className={`turn turn--${item.author}`}>
            <div className="turn-body-wrap">
              <span className="turn-index turn-index--card-edge" aria-hidden="true">
                {String(index + 1).padStart(2, "0")}
              </span>
              <p className="turn-speaker">{item.author === "agent" ? "Agent" : "Participant"}</p>
              <p className="turn-text">{item.status === "unavailable" ? "Content unavailable." : item.content}</p>
              {item.occurred_at ? <p className="turn-time">{item.occurred_at}</p> : null}
            </div>
          </li>
        ))}
        {completing ? (
          <li>
            <div className="complete-plate pane pane--notched">
              <h2 className="complete-title">Session completing</h2>
              <p className="complete-copy">Sending is closed. The server is sealing the transcript cutoff. This is not a score or Result.</p>
            </div>
          </li>
        ) : null}
        {terminal ? (
          <li>
            <div className="complete-plate pane pane--notched">
              <svg className="complete-mark" viewBox="0 0 52 52" aria-hidden="true">
                <circle cx="26" cy="26" r="24" />
                <path d="M15 27l8 8 15-17" />
              </svg>
              <h2 className="complete-title">Session completed</h2>
              <p className="complete-copy">Your examination record has been preserved. Nothing further is required of you.</p>
              <p className="complete-copy">This confirmation does not include a score, Evaluation, Result, or release.</p>
              <div className="complete-keys">
                <Key variant="begin" to="/my-work">
                  <span>Return to assignment</span>
                  <TransmitChevron />
                </Key>
                {can(snapshot, "view_transcript") ? (
                  <Key variant="quiet" to={`/sessions/${sessionId}/transcript`}>View Session transcript</Key>
                ) : null}
              </div>
            </div>
          </li>
        ) : null}
      </ol>
      {confirmComplete ? (
        <CeremonyDialog open onClose={() => setConfirmComplete(false)} labelledBy="complete-session-title">
          <DialogPlate>
            <DialogPlateHead title="Complete this Session?" titleId="complete-session-title" />
            <DialogPlateBody>
              <p>Completion is final. You cannot send further messages after the Session completes.</p>
            </DialogPlateBody>
            <DialogPlateFooter>
              <Key variant="quiet" onClick={() => setConfirmComplete(false)}>Cancel</Key>
              <Key
                onClick={() => {
                  if (!snapshot) return;
                  setConfirmComplete(false);
                  void submit({
                    schema_version: "v1",
                    command_type: "session.complete.v1",
                    command_id: createSessionCommandId(),
                    idempotency_key: createSessionIdempotencyKey(),
                    session_locator: { session_id: sessionId },
                    expected_session_version: snapshot.session_version,
                    payload: {},
                  });
                }}
              >
                Complete Session
              </Key>
            </DialogPlateFooter>
          </DialogPlate>
        </CeremonyDialog>
      ) : null}
    </TextSessionStation>
  );
}
