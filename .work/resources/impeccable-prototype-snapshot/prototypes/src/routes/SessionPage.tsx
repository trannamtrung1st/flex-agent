import { useEffect, useMemo, useReducer, useRef, useState } from "react";
import { AcknowledgmentGate, Announcer, BrandMark, CeremonyDialog, Key, RailBrand, ReadoutList, StageBars, TransmitChevron } from "../components";
import { arcPath, formatClock, polar } from "../lib/format";
import { useAnnouncer } from "../lib/useAnnouncer";
import { useStateParam } from "../lib/useDemoParam";
import { useSurface } from "../lib/useSurface";
import { initialSessionModel, sessionReducer } from "../features/session/sessionReducer";

const GAUGE_MINUTES = 60;
const SWEEP = 300;

export function SessionPage() {
  useSurface("participant-session");
  const stateParam = useStateParam(["live", "warned", "complete", "briefing"] as const, null);
  const [state, dispatch] = useReducer(sessionReducer, stateParam, initialSessionModel);
  const { message, announce } = useAnnouncer();
  const agentTimer = useRef<number | null>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const ledgerRef = useRef<HTMLElement>(null);
  const [acked, setAcked] = useState(false);
  const [leaveOpen, setLeaveOpen] = useState(false);

  useEffect(() => {
    if (state.briefing || state.complete) return;
    const id = window.setInterval(() => dispatch({ type: "tick" }), 1000);
    return () => window.clearInterval(id);
  }, [state.briefing, state.complete]);

  useEffect(() => {
    if (state.warned && !state.complete && !state.briefing) {
      announce("Time warning: 40 minutes remaining.");
      const t = window.setTimeout(() => dispatch({ type: "clear-warn" }), 5200);
      return () => window.clearTimeout(t);
    }
  }, [announce, state.briefing, state.complete, state.warned]);

  useEffect(() => {
    if (stateParam === "complete" && !state.complete) dispatch({ type: "complete" });
  }, [state.complete, stateParam]);

  useEffect(() => {
    const el = ledgerRef.current;
    if (!el) return;
    const active = el.querySelector(".turn.is-active");
    if (active) active.scrollIntoView({ block: "nearest" });
    else el.scrollTop = el.scrollHeight;
  }, [state.turns]);

  const hadComplete = useRef(state.complete);
  useEffect(() => {
    if (!state.complete) {
      hadComplete.current = false;
      return;
    }
    if (hadComplete.current) return;
    hadComplete.current = true;
    const frame = window.requestAnimationFrame(() => {
      const exit = document.getElementById("completeToAssignment");
      exit?.scrollIntoView({ block: "nearest" });
      exit?.focus();
    });
    return () => window.cancelAnimationFrame(frame);
  }, [state.complete]);

  useEffect(() => () => {
    if (agentTimer.current !== null) window.clearTimeout(agentTimer.current);
  }, []);

  useEffect(() => {
    const el = inputRef.current;
    if (!el || state.composer) return;
    el.style.height = "auto";
  }, [state.composer]);

  const assignmentDemo = state.complete ? "result-pending" : state.briefing ? "examination-ready" : "examination-active";
  const deg = Math.max(0, Math.min(SWEEP, (state.remaining / (GAUGE_MINUTES * 60)) * SWEEP));
  const [nx, ny] = polar(48, 48, 40, deg);
  const ticks = useMemo(() => {
    const items = [];
    for (let m = 0; m <= GAUGE_MINUTES; m += 5) {
      const d = (m / GAUGE_MINUTES) * SWEEP;
      const [x1, y1] = polar(48, 48, 40, d);
      const [x2, y2] = polar(48, 48, m % 15 === 0 ? 32 : 35.5, d);
      items.push({ x1, y1, x2, y2, m });
    }
    return items;
  }, []);

  return (
    <>
      <div className={`console${state.warned && !state.complete ? " is-warned" : ""}${state.complete ? " is-complete" : ""}`}>
        <div className="frame-traces" aria-hidden="true">
          <span className="trace trace-top" />
          <span className="trace trace-chrono" />
          <span className="trace trace-foot" />
        </div>
        <aside className="rail" aria-label="Session instruments">
          <RailBrand suffix="Examination Console" />
          <ReadoutList
            rows={[
              { term: "Session ID", value: "FXA-7C19-2A07" },
              { term: "Participant ID", value: "CND-8842-19" },
              { term: "Session", value: "07" },
            ]}
          />
          <div className="rail-nav">
            <Key
              className="rail-back"
              variant="quiet"
              to={`/participant-journey?demo=${assignmentDemo}`}
              ariaLabel="Back to assignment"
            >
              Assignment
            </Key>
            <Key className="rail-leave" onClick={() => setLeaveOpen(true)}>
              Leave session
            </Key>
          </div>
          <section className="feed" aria-label="Console feed">
            <h2 className="rail-h">Console Feed</h2>
            <p className="feed-sub">Live transcript</p>
            <ol className="feed-log" aria-live="off">
              {state.feed.map((item, i) => (
                <li key={`${item.text}-${i}`}>
                  <time>{item.t}</time>
                  <span className={item.amber ? "feed-mark-amber" : undefined}>{item.text}</span>
                </li>
              ))}
            </ol>
          </section>
          <ReadoutList
            className="readout-stack readout-stack--sys"
            rows={[
              { term: "Record", value: "Preserved" },
              { term: "Link", value: "Nominal" },
            ]}
          />
          <div className="protocol-plate pane pane--dim pane--br">
            <span className="protocol-label">Protocol</span>
            <span className="protocol-value">V7.3.1</span>
          </div>
        </aside>

        <div className="session-main">
          <main className="ledger-frame pane pane--tl" aria-label="Examination transcript" ref={ledgerRef}>
            <ol className="ledger">
              {state.turns.map((turn, i) => {
                const idx = String(state.turns.slice(0, i + 1).filter((t) => !t.thinking).length).padStart(2, "0");
                return (
                  <li
                    key={i}
                    className={`turn turn--${turn.speaker}${turn.active ? " is-active" : ""}${turn.thinking ? " is-thinking-row" : ""}${turn.arriving ? " is-arriving" : ""}`}
                  >
                    <div className="turn-body-wrap">
                      <span className="turn-index turn-index--card-edge" aria-hidden="true">
                        {turn.thinking ? "" : idx}
                      </span>
                      <p className="turn-speaker">{turn.speaker === "agent" ? "Agent" : "Participant"}</p>
                      <p className={`turn-text${turn.thinking ? " wait-copy" : ""}`}>{turn.text}</p>
                      {turn.time ? <p className="turn-time">{turn.time}</p> : null}
                    </div>
                  </li>
                );
              })}
              {state.complete ? (
                <li>
                  <div className="complete-plate pane pane--notched">
                    <svg className="complete-mark" viewBox="0 0 52 52" aria-hidden="true">
                      <circle cx="26" cy="26" r="24" />
                      <path d="M15 27l8 8 15-17" />
                    </svg>
                    <h2 className="complete-title">Session Complete</h2>
                    <p className="complete-copy">Your examination record for Session 07 has been transmitted and preserved. Nothing further is required of you.</p>
                    <p className="complete-copy">A human reviewer will inspect the evaluation before any result is released. You will be notified when your result is available.</p>
                    <p className="complete-sub">Record FXA-7C19-2A07 · Attempt 1 · Sealed</p>
                    <div className="complete-keys">
                      <Key id="completeToAssignment" variant="begin" to={`/participant-journey?demo=${assignmentDemo}`}>
                        <span>Return to Assignment</span>
                        <TransmitChevron />
                      </Key>
                    </div>
                  </div>
                </li>
              ) : null}
            </ol>
          </main>
          <footer className="composer-row">
            <form
              className="composer"
              onSubmit={(e) => {
                e.preventDefault();
                if (state.busy || state.complete) return;
                if (!state.composer.trim()) {
                  inputRef.current?.focus();
                  return;
                }
                dispatch({ type: "transmit" });
                dispatch({ type: "agent-start" });
                if (agentTimer.current !== null) window.clearTimeout(agentTimer.current);
                agentTimer.current = window.setTimeout(() => {
                  dispatch({ type: "agent-done" });
                  announce("The Examiner asked a follow-up.");
                  agentTimer.current = null;
                }, 1900 + Math.random() * 900);
              }}
            >
              <label className="visually-hidden" htmlFor="composerInput">
                Compose reply
              </label>
              <textarea
                id="composerInput"
                ref={inputRef}
                rows={1}
                placeholder="Compose reply — Attempt 1, Session 07"
                autoComplete="off"
                spellCheck
                disabled={state.complete}
                value={state.composer}
                onChange={(e) => {
                  dispatch({ type: "compose", value: e.target.value });
                  e.target.style.height = "auto";
                  e.target.style.height = `${Math.min(e.target.scrollHeight, 200)}px`;
                }}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && !e.shiftKey && !e.nativeEvent.isComposing) {
                    e.preventDefault();
                    (e.currentTarget.form as HTMLFormElement | null)?.requestSubmit();
                  }
                }}
              />
              <Key
                id="transmitBtn"
                variant="transmit"
                type="submit"
                waiting={state.busy}
                disabled={state.busy || state.complete}
              >
                <span>Transmit</span>
                {state.busy ? null : <TransmitChevron />}
              </Key>
            </form>
            <div className="link-plate" aria-label="Connection status: link nominal">
              <span className="link-label">Link Nominal</span>
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
          </footer>
        </div>

        <aside className="agent-panel pane" aria-label="Examiner station">
          <section className="agent-post" aria-label="Examiner">
            <div
              className={`agent-core agent-core--live1${state.thinking ? " is-thinking" : ""}`}
              role="img"
              aria-label={state.complete ? "Agent core — session complete" : state.thinking ? "Agent core — considering" : "Agent core — idle"}
            >
              <span className="live1-ring" aria-hidden="true" />
              <span className="live1-shell" aria-hidden="true" />
            </div>
            <h1 className="agent-name">
              <BrandMark />
              <span className="agent-name-role">Examiner</span>
            </h1>
            <p className="agent-line">
              {state.complete ? "Well done. Your record is safely stored." : state.thinking ? "Considering your reply…" : "Take the time you need. I’m listening."}
            </p>
          </section>
          <section className="chrono" aria-label="Session timing">
            <div className="chrono-main">
              <div className="chrono-digits-block">
                <h2 className="chrono-label">Time Remaining</h2>
                <p className="chrono-digits" role="timer" aria-live="off">
                  {formatClock(state.remaining)}
                </p>
              </div>
              <svg className="chrono-gauge" viewBox="0 0 96 96" aria-hidden="true">
                <path className="gauge-track" d={arcPath(48, 48, 40, 0, SWEEP)} />
                <path className="gauge-fill" d={deg > 0.5 ? arcPath(48, 48, 40, 0, deg) : ""} />
                <g className="gauge-ticks">
                  {ticks.map((t) => (
                    <line key={t.m} x1={t.x1} y1={t.y1} x2={t.x2} y2={t.y2} />
                  ))}
                </g>
                <g className="gauge-nums">
                  <text x={polar(48, 48, 25, 0)[0]} y={polar(48, 48, 25, 0)[1]}>
                    60
                  </text>
                  <text x={polar(48, 48, 25, 150)[0]} y={polar(48, 48, 25, 150)[1]}>
                    30
                  </text>
                </g>
                <circle className="gauge-needle" cx={nx} cy={ny} r="3.4" />
              </svg>
            </div>
            <div className="chrono-stage">
              <p className="stage-line">
                Stage — <span>{state.complete ? "Complete" : "Examination"}</span>{" "}
                <span className="stage-count">
                  <span>{state.stage}</span> of 5
                </span>
              </p>
              <StageBars stage={state.stage} total={5} complete={state.complete} />
              <Key id="submitOpen" disabled={state.complete} onClick={() => dispatch({ type: "open-confirm", open: true })}>
                Submit Session
              </Key>
            </div>
          </section>
        </aside>
      </div>

      {!state.dismissed ? (
        <div className={`briefing${state.briefing ? "" : " is-dismissed"}`} role="dialog" aria-modal="true" aria-labelledby="briefTitle">
          <div className="briefing-plate pane pane--notched">
            <header className="briefing-head">
              <span className="briefing-kicker-mark" aria-hidden="true" />
              <h1 className="briefing-title" id="briefTitle">
                Examination Briefing
              </h1>
              <p className="briefing-ident">Session 07 · FXA-7C19-2A07 · Participant CND-8842-19</p>
            </header>
            <div className="briefing-body">
              <section className="briefing-sec">
                <h2>Assignment</h2>
                <p>
                  A text examination on your submitted case study, <em>“Real-time Inventory &amp; Order Management at Scale”</em> (Submission v2, preserved). The Examiner — an AI agent operating under a frozen configuration — will ask follow-up questions about your work. There are 5 stages; your session is timed.
                </p>
              </section>
              <section className="briefing-sec">
                <h2>Rules of the session</h2>
                <ul>
                  <li>You are conversing with an AI agent, not a person. Its conduct is governed and recorded.</li>
                  <li>
                    Time allotted: <strong>45 minutes</strong>. You will receive a warning when 40 minutes remain in this demonstration.
                  </li>
                  <li>Answer in your own words. You may take as long as you need within the time limit.</li>
                  <li>Every exchange is numbered and preserved for human review before any result is released.</li>
                </ul>
              </section>
              <AcknowledgmentGate id="ackBox" checked={acked} onChange={setAcked}>
                I acknowledge the rules and consent to the recording of this session.
              </AcknowledgmentGate>
            </div>
            <footer className="briefing-foot">
              <p className="briefing-note">Synthetic demonstration content — no real participant data.</p>
              <Key
                variant="begin"
                disabled={!acked}
                onClick={() => {
                  dispatch({ type: "begin" });
                  announce("Examination started. The Examiner's question is on screen.");
                  window.setTimeout(() => inputRef.current?.focus(), 460);
                }}
              >
                <span>Resume Examination</span>
                <TransmitChevron />
              </Key>
            </footer>
          </div>
        </div>
      ) : null}

      <CeremonyDialog
        open={state.confirm}
        onClose={() => dispatch({ type: "open-confirm", open: false })}
        labelledBy="confirmTitle"
        id="confirmDialog"
        className="confirm"
      >
        <div className="confirm-plate pane pane--notched">
          <h2 className="confirm-title" id="confirmTitle">
            Confirm Submission
          </h2>
          <p className="confirm-copy">This ends Session 07 and transmits your examination record for evaluation and human review. You will not be able to add further replies.</p>
          <div className="confirm-keys">
            <Key id="confirmCancel" onClick={() => dispatch({ type: "open-confirm", open: false })}>Remain in Session</Key>
            <Key
              id="confirmSubmit"
              variant="transmit"
              onClick={() => {
                dispatch({ type: "complete" });
                announce("Session submitted and sealed. A human reviewer will release your result.");
              }}
            >
              <span>Submit Session</span>
              <TransmitChevron />
            </Key>
          </div>
        </div>
      </CeremonyDialog>
      <CeremonyDialog
        open={leaveOpen}
        onClose={() => setLeaveOpen(false)}
        labelledBy="leaveTitle"
        id="leaveDialog"
        className="confirm"
      >
        <div className="confirm-plate pane pane--notched">
          <h2 className="confirm-title" id="leaveTitle">
            Leave session
          </h2>
          <p className="confirm-copy">
            Current replies stay preserved in this prototype. The demonstration timer continues while this plate is
            open. Production pause and resume are owned by the examination runtime and are not simulated here.
          </p>
          <div className="confirm-keys">
            <Key onClick={() => setLeaveOpen(false)}>Remain in session</Key>
            <Key variant="quiet" to={`/participant-journey?demo=${assignmentDemo}`}>
              Leave to assignment
            </Key>
          </div>
        </div>
      </CeremonyDialog>
      <Announcer message={message} />
    </>
  );
}
