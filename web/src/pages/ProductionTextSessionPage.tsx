import { useCallback, useEffect, useMemo, useReducer, useRef, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useProductionApi } from "../api/production-api";
import {
  createProductionSessionClient,
  createSessionCommandId,
  createSessionIdempotencyKey,
} from "../api/production-session";
import { SessionTranscriptLedger } from "../components/work/SessionTranscriptLedger";
import { SessionChrono } from "../components/work/SessionChrono";
import { AgentStatusLine, ProtocolPlate } from "../components/work/SessionMarks";
import { TextSessionStation } from "../components/work/TextSessionStation";
import { sessionKeys } from "../features/session/queryKeys";
import { sessionAtTimeBoundary } from "../features/session/session-stage";
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
  if (sessionAtTimeBoundary(snapshot)) {
    return "Checking Session end. Sending is closed.";
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
  const expiryReconcileOnce = useRef<string | null>(null);
  const [expiredByTime, setExpiredByTime] = useState(false);

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
    const sendOnce = async (envelope: SessionCommandEnvelopeV1) => {
      dispatch({ type: "send", sendState: "pending" });
      const outcome = await client.submitCommand(sessionId, envelope);
      if (outcome.outcome_category === "uncertain") {
        dispatch({ type: "send", sendState: "uncertain" });
        dispatch({ type: "error", message: "The command was not confirmed. Reconcile before sending again." });
        return "stop";
      }
      if (outcome.succeeded || outcome.outcome_category === "duplicate") {
        dispatch({
          type: "accepted",
          session_version: outcome.session_version,
          session_sequence: outcome.session_sequence,
        });
        if (envelope.command_type === "session.message.send.v1") {
          dispatch({ type: "draft", draft: "" });
          setPendingIdempotency(null);
        }
        await snapshotQuery.refetch();
        dispatch({ type: "send", sendState: "idle" });
        return "stop";
      }
      return outcome;
    };

    try {
      const first = await sendOnce(command);
      if (first === "stop") {
        return;
      }
      if (
        first.outcome_category === "conflict"
        && command.command_type === "session.message.send.v1"
      ) {
        const refreshed = await snapshotQuery.refetch();
        const nextVersion = refreshed.data?.session_version;
        if (
          refreshed.data
          && nextVersion != null
          && nextVersion !== command.expected_session_version
        ) {
          dispatch({ type: "snapshot", snapshot: refreshed.data });
          const retryKey = createSessionIdempotencyKey();
          setPendingIdempotency(retryKey);
          const retry = await sendOnce({
            ...command,
            command_id: createSessionCommandId(),
            idempotency_key: retryKey,
            expected_session_version: nextVersion,
          });
          if (retry === "stop") {
            return;
          }
        }
      }
      dispatch({ type: "send", sendState: "idle" });
      dispatch({
        type: "error",
        message: first.outcome_category === "conflict"
          ? "This conversation is still the same Session. Send again."
          : "The Session could not accept that command.",
      });
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
  const sendBusy = view.sendState !== "idle";
  const sendHeld = sendBusy || working;
  const timeEnded = sessionAtTimeBoundary(snapshot);
  const composerClosed = terminal || completing || paused || timeEnded || !can(snapshot, "send_message");
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

  /* Completing is a server lifecycle; the follow-up complete command runs
     after the authoritative snapshot arrives. */
  useEffect(() => {
    if (
      !snapshot
      || snapshot.projection_kind !== "participant"
      || snapshot.lifecycle_state !== "completing"
      || sessionAtTimeBoundary(snapshot)
    ) {
      return;
    }
    if (working || !can(snapshot, "complete_session") || sealedOnce.current === snapshot.session_id) {
      return;
    }

    sealedOnce.current = snapshot.session_id;
    queueMicrotask(() => {
      void submit({
        schema_version: "v1",
        command_type: "session.complete.v1",
        command_id: createSessionCommandId(),
        idempotency_key: createSessionIdempotencyKey(),
        session_locator: { session_id: sessionId },
        expected_session_version: snapshot.session_version,
        payload: {},
      });
    });
  }, [sessionId, snapshot, submit, working]);

  useEffect(() => {
    if (!snapshot || snapshot.projection_kind !== "participant" || !sessionAtTimeBoundary(snapshot)) {
      return;
    }
    if (!can(snapshot, "reconcile") || expiryReconcileOnce.current === snapshot.session_id) {
      return;
    }

    expiryReconcileOnce.current = snapshot.session_id;
    queueMicrotask(() => {
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
    });
  }, [sessionId, snapshot, submit]);

  if (timeEnded && !expiredByTime) {
    setExpiredByTime(true);
  }

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
      complete={terminal || timeEnded}
      mainRef={ledgerRef}
      instruments={(
        <>
          <ReadoutList
            rows={[
              { term: "Session ID", value: snapshot?.session_id ? <CompactId tabbable value={snapshot.session_id} /> : "—" },
              { term: "Participant", value: shell?.display_name?.trim() || "Participant" },
              { term: "Session", value: snapshot?.lifecycle_state ?? "loading" },
              { term: "Record", value: terminal ? "Preserved" : timeEnded ? "Closing" : "Open" },
              { term: "Link", value: view.connection === "connected" ? "Nominal" : "Recovering" },
            ]}
          />
          <ProtocolPlate label="Protocol" value="V1" />
        </>
      )}
      composer={composerClosed ? undefined : (
        <>
          <form
            className="composer"
            onSubmit={(event) => {
              event.preventDefault();
              if (!snapshot || sendHeld || view.draft.trim().length === 0) {
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
              disabled={sendBusy}
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
              waiting={view.sendState === "pending" || view.sendState === "checking"}
              disabled={sendHeld || view.draft.trim().length === 0}
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
            canSubmit={can(snapshot, "complete_session") && !terminal && !timeEnded}
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
      <SessionTranscriptLedger
        items={items}
        label="Session turns"
        copyFor={(item) => revealed[item.item_id] ?? transcriptItemCopy(item)}
        turnState={(item, index) => {
          const copy = revealed[item.item_id] ?? transcriptItemCopy(item);
          const target = transcriptItemCopy(item);
          return {
            arriving: item.author === "agent" && copy.length > 0 && copy.length < target.length,
            active: !terminal && !completing && !timeEnded && index === items.length - 1,
          };
        }}
      >
        {timeEnded ? (
          <li className="ledger-complete">
            <div className="complete-plate pane pane--notched">
              <h2 className="complete-title">Checking Session end</h2>
              <p className="complete-copy">Sending is closed. The server is reconciling the time cutoff. This is not a score or Result.</p>
            </div>
          </li>
        ) : null}
        {completing && !timeEnded ? (
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
              <h2 className="complete-title">{expiredByTime ? "Time ended. Session completed" : "Session Complete"}</h2>
              <p className="complete-copy">
                {expiredByTime
                  ? "Only content accepted before the Session cutoff is included."
                  : "Your examination record has been preserved. Nothing further is required of you."}
              </p>
              <p className="complete-copy">This confirmation is not a score or Result.</p>
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
      </SessionTranscriptLedger>
    </TextSessionStation>
  );
}
