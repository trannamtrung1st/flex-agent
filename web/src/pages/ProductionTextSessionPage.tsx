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
import { AgentStatusLine, ProtocolPlate } from "../components/work/SessionMarks";
import { TextSessionStation } from "../components/work/TextSessionStation";
import { sessionKeys } from "../features/session/queryKeys";
import { emptySessionLiveView, sessionLiveReducer } from "../features/session/session-view";
import { transcriptItemCopy, useTranscriptReveal } from "../features/session/useTranscriptReveal";
import type { SessionCommandEnvelopeV1, SessionHostedEventEnvelopeV1, SessionSnapshotV1 } from "../contracts/v1";
import {
  Alert,
  BrandMark,
  CompactId,
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
    return "Considering your reply…";
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
  const { apiState, fetchJson, shell } = useProductionApi();
  const client = useMemo(() => createProductionSessionClient(fetchJson), [fetchJson]);
  const [view, dispatch] = useReducer(sessionLiveReducer, emptySessionLiveView);
  const [confirmComplete, setConfirmComplete] = useState(false);
  const [leaveOpen, setLeaveOpen] = useState(false);
  const [pendingIdempotency, setPendingIdempotency] = useState<string | null>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const ledgerRef = useRef<HTMLElement>(null);
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
        dispatch({
          type: "error",
          message: outcome.outcome_category === "conflict"
            ? "The Session changed. Reconcile before sending again."
            : "The Session could not accept that command.",
        });
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
  const items = snapshot?.transcript?.items ?? [];
  const revealed = useTranscriptReveal(items, snapshot != null);

  useEffect(() => {
    const el = ledgerRef.current;
    if (!el) {
      return;
    }
    const active = el.querySelector(".turn.is-active");
    if (active) {
      active.scrollIntoView({ block: "nearest" });
      return;
    }
    el.scrollTop = el.scrollHeight;
  }, [items, revealed, completing, terminal]);

  useEffect(() => {
    if (!terminal) {
      return;
    }
    const frame = window.requestAnimationFrame(() => {
      const exit = document.getElementById("completeToAssignment");
      exit?.scrollIntoView({ block: "nearest" });
      if (exit instanceof HTMLElement) {
        exit.focus();
      }
    });
    return () => window.cancelAnimationFrame(frame);
  }, [terminal]);

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
          <Key className="rail-leave" onClick={() => setLeaveOpen(true)}>
            Leave session
          </Key>
        </div>
      )}
      warned={snapshot?.timing?.warning_code === "imminent"}
      complete={terminal}
      mainRef={ledgerRef}
      instruments={(
        <>
          <ReadoutList
            rows={[
              { term: "Session ID", value: snapshot?.session_id ? <CompactId tabbable value={snapshot.session_id} /> : "—" },
              { term: "Participant", value: shell?.display_name?.trim() || "Participant" },
              { term: "Session", value: snapshot?.lifecycle_state ?? "loading" },
            ]}
          />
          <section className="feed" aria-label="Console feed">
            <h2 className="rail-h">Console Feed</h2>
            <p className="feed-sub">Live transcript</p>
            <ol className="feed-log" aria-live="off">
              {items.slice(-4).map((item) => (
                <li key={item.item_id}>
                  <time>{item.occurred_at ? item.occurred_at.slice(11, 19) : "—"}</time>
                  <span>{item.author === "agent" ? "Agent" : "Participant"} admitted.</span>
                </li>
              ))}
              {snapshot?.activity?.work_state === "no_action" ? (
                <li>
                  <time>—</time>
                  <span>Agent recorded no further output.</span>
                </li>
              ) : null}
              {snapshot?.activity?.work_state === "failed" ? (
                <li>
                  <time>—</time>
                  <span className="feed-mark-amber">Agent work failed closed.</span>
                </li>
              ) : null}
              {view.lastError ? (
                <li>
                  <time>—</time>
                  <span className="feed-mark-amber">{view.lastError}</span>
                </li>
              ) : null}
              {view.connection === "reconnecting" || view.connection === "offline" ? (
                <li>
                  <time>—</time>
                  <span>Link recovering.</span>
                </li>
              ) : null}
            </ol>
          </section>
          <ReadoutList
            className="readout-stack readout-stack--sys"
            rows={[
              { term: "Record", value: terminal ? "Preserved" : "Open" },
              { term: "Link", value: view.connection === "connected" ? "Nominal" : "Recovering" },
            ]}
          />
          <ProtocolPlate label="Protocol" value="V1" />
        </>
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
                if (event.key === "Enter" && !event.shiftKey && !event.nativeEvent.isComposing) {
                  event.preventDefault();
                  event.currentTarget.form?.requestSubmit();
                }
              }}
            />
            <Key
              id="transmitBtn"
              variant="transmit"
              type="submit"
              waiting={view.sendState === "pending"}
              disabled={view.sendState !== "idle" || view.draft.trim().length === 0}
            >
              <span>Transmit</span>
              {view.sendState === "pending" ? null : <TransmitChevron />}
            </Key>
          </form>
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
            <svg className="link-glyph link-glyph--bars" viewBox="0 0 22 10" aria-hidden="true">
              <g className="link-bars">
                <rect x="0" y="0" width="2" height="10" />
                <rect x="4" y="0" width="2" height="10" />
                <rect x="8" y="0" width="2" height="10" />
                <rect x="12" y="0" width="2" height="10" />
                <rect x="16" y="0" width="2" height="10" />
                <rect x="20" y="0" width="2" height="10" />
              </g>
            </svg>
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
              <span className="agent-name-role">Examiner</span>
            </h1>
            <AgentStatusLine>{examinerLine(snapshot, working, terminal)}</AgentStatusLine>
          </section>
          <SessionChrono
            snapshot={snapshot}
            canSubmit={can(snapshot, "complete_session") && !terminal}
            onSubmit={() => setConfirmComplete(true)}
          />
        </>
      )}
      overlays={(
        <>
          <CeremonyDialog
            open={confirmComplete}
            onClose={() => setConfirmComplete(false)}
            labelledBy="confirmTitle"
            id="confirmDialog"
          >
            <DialogPlate width="wide">
              <DialogPlateHead title="Confirm Submission" titleId="confirmTitle" />
              <DialogPlateBody>
                <p>
                  This ends the Session and transmits your examination record. You will not be able to add further replies.
                </p>
              </DialogPlateBody>
              <DialogPlateFooter
                arrangement="split"
                secondary={
                  <Key id="confirmCancel" onClick={() => setConfirmComplete(false)}>
                    Remain in Session
                  </Key>
                }
                primary={
                  <Key
                    id="confirmSubmit"
                    variant="transmit"
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
                    <span>Submit Session</span>
                    <TransmitChevron />
                  </Key>
                }
              />
            </DialogPlate>
          </CeremonyDialog>
          <CeremonyDialog open={leaveOpen} onClose={() => setLeaveOpen(false)} labelledBy="leaveTitle" id="leaveDialog">
            <DialogPlate width="wide">
              <DialogPlateHead title="Leave session" titleId="leaveTitle" />
              <DialogPlateBody>
                <p>
                  Current replies stay preserved. The Session timer continues while this plate is open.
                </p>
              </DialogPlateBody>
              <DialogPlateFooter
                arrangement="split"
                secondary={<Key onClick={() => setLeaveOpen(false)}>Remain in session</Key>}
                primary={
                  <Key variant="quiet" to="/my-work">
                    Leave to assignment
                  </Key>
                }
              />
            </DialogPlate>
          </CeremonyDialog>
        </>
      )}
    >
      <ol className="ledger">
        {items.map((item, index) => {
          const copy = revealed[item.item_id] ?? transcriptItemCopy(item);
          const target = transcriptItemCopy(item);
          const arriving = item.author === "agent" && copy.length > 0 && copy.length < target.length;
          const active = !terminal && !completing && index === items.length - 1;
          return (
            <li
              key={item.item_id}
              className={`turn turn--${item.author}${active ? " is-active" : ""}${arriving ? " is-arriving" : ""}`}
            >
              <div className="turn-body-wrap">
                <span className="turn-index turn-index--card-edge" aria-hidden="true">
                  {String(index + 1).padStart(2, "0")}
                </span>
                <p className="turn-speaker">{item.author === "agent" ? "Agent" : "Participant"}</p>
                <p className="turn-text">{copy}</p>
                {item.occurred_at ? <p className="turn-time">{item.occurred_at}</p> : null}
              </div>
            </li>
          );
        })}
        {completing ? (
          <li className="ledger-complete">
            <div className="complete-plate pane pane--notched">
              <h2 className="complete-title">Session completing</h2>
              <p className="complete-copy">Sending is closed. The server is sealing the transcript cutoff. This is not a score or Result.</p>
            </div>
          </li>
        ) : null}
        {terminal ? (
          <li className="ledger-complete">
            <div className="complete-plate pane pane--notched">
              <svg className="complete-mark" viewBox="0 0 52 52" aria-hidden="true">
                <circle cx="26" cy="26" r="24" />
                <path d="M15 27l8 8 15-17" />
              </svg>
              <h2 className="complete-title">Session Complete</h2>
              <p className="complete-copy">Your examination record has been preserved. Nothing further is required of you.</p>
              <p className="complete-copy">A human reviewer will inspect the evaluation before any result is released. This confirmation is not a score or Result.</p>
              {snapshot?.session_id ? (
                <p className="complete-sub">
                  Record {snapshot.session_id} · Sealed
                </p>
              ) : null}
              <div className="complete-keys">
                <Key id="completeToAssignment" variant="begin" to="/my-work">
                  <span>Return to Assignment</span>
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
    </TextSessionStation>
  );
}
