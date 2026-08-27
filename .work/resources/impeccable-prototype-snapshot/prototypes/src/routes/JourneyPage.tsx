import { useMemo, useState } from "react";
import { Link } from "react-router";
import {
  AcknowledgmentGate,
  Announcer,
  DemoPlate,
  Key,
  PARTICIPANT_HOME,
  PARTICIPANT_IDENTITY,
  ProfileMenu,
  RailBrand,
  ReadoutList,
  SignOutCeremony,
  StateIndicator,
  TransmitChevron,
  usePrototypeSignOut,
} from "../components";
import { JOURNEY_DEMO_KEYS, JOURNEY_DEMOS, JOURNEY_PHASES, type JourneyDemo } from "../data/fixtures/journey";
import { useAnnouncer } from "../lib/useAnnouncer";
import { useDemoParam } from "../lib/useDemoParam";
import { useSurface } from "../lib/useSurface";
import { isReachable, PhaseSpine } from "../features/journey/PhaseSpine";

const JOURNEY_DEMO_OPTIONS = [
  { value: "briefing", label: "First arrival — briefing" },
  { value: "submission", label: "Submission current" },
  { value: "examination-ready", label: "Examination ready" },
  { value: "examination-active", label: "Examination in progress" },
  { value: "result-pending", label: "Result pending release" },
  { value: "result-released", label: "Result released" },
] as const;

export function JourneyPage() {
  useSurface("participant-journey");
  const [demo, setDemo] = useDemoParam(JOURNEY_DEMO_KEYS, "briefing");
  const [view, setView] = useState<string | null>(null);
  const [briefingAcked, setBriefingAcked] = useState(demo !== "briefing");
  const { message, announce } = useAnnouncer();
  const { actions, signOutOpen, setSignOutOpen } = usePrototypeSignOut();
  const snap = JOURNEY_DEMOS[demo];
  const viewPhase = view && isReachable(view, snap) ? view : snap.current;
  const phase = JOURNEY_PHASES.find((p) => p.id === viewPhase)!;

  const status = useMemo(() => {
    let statusPhase: string = phase.statusPhase;
    let statusRecord: string = snap.record;
    if (viewPhase === "examination") {
      if (snap.examination === "ready") statusPhase = "Examination — Ready";
      if (snap.examination === "active") statusPhase = "Examination — Live";
      if (snap.examination === "complete") statusPhase = "Examination — Complete";
    }
    if (viewPhase === "result") statusRecord = snap.result === "released" ? "Released" : "Pending release";
    return { statusPhase, statusRecord };
  }, [phase.statusPhase, snap, viewPhase]);

  const nodeMod = /live|in session/i.test(status.statusRecord)
    ? "live"
    : /released|ready/i.test(status.statusRecord)
      ? "sealed"
      : "";

  return (
    <>
      <div className="station">
        <div className="frame-traces" aria-hidden="true">
          <span className="trace trace-top" />
          <span className="trace trace-rail" />
        </div>
        <aside className="phase-rail" aria-label="Assignment phases">
          <RailBrand suffix="Assignment Station">
            <Link className="rail-home-link" to={PARTICIPANT_HOME}>
              <svg viewBox="0 0 10 10" aria-hidden="true" focusable="false">
                <path d="M6.5 1.5 L3 5 L6.5 8.5" fill="none" stroke="currentColor" strokeWidth="1.1" strokeLinecap="square" />
              </svg>
              Home
            </Link>
            <ProfileMenu identity={PARTICIPANT_IDENTITY} actions={actions} className="strip-profile--rail" />
          </RailBrand>
          <div className="phase-rail-scroll">
            <ReadoutList
              rows={[
                { term: "Enrollment", value: "ENR-7C19-8842" },
                { term: "Campaign", value: "Systems Design Q3" },
                { term: "Attempt", value: "1 of 1" },
              ]}
            />
            <PhaseSpine
              snap={snap}
              viewPhase={viewPhase}
              onSelect={(phaseId) => setView(phaseId === snap.current ? null : phaseId)}
            />
            <DemoPlate
              id="demoState"
              value={demo}
              describedBy="demoNote"
              plateLabel="Prototype demonstration controls"
              onChange={(next) => {
                const demoValue = next as JourneyDemo;
                setDemo(demoValue);
                setView(null);
                setBriefingAcked(demoValue !== "briefing");
                const label = JOURNEY_DEMO_OPTIONS.find((opt) => opt.value === demoValue)?.label ?? next;
                announce(`Demo state set to ${label}.`);
              }}
              options={[...JOURNEY_DEMO_OPTIONS]}
              note={
                <p className="demo-note" id="demoNote">
                  Cycles assignment beats for later implementation reference.
                </p>
              }
            />
            <div className="protocol-plate pane pane--dim pane--br">
              <span className="protocol-label">Protocol</span>
              <span className="protocol-value">V7.3.1</span>
            </div>
          </div>
        </aside>
        <div className="station-main">
          <header className="assignment-head">
            <div className="assignment-ident">
              <h1 className="assignment-title">Real-time Inventory &amp; Order Management at Scale</h1>
              <p className="assignment-meta">Activity · Text examination · Session 07 · FXA-7C19-2A07</p>
            </div>
            <dl className="status-readout" aria-label="Assignment status">
              <div className="status-item">
                <dt>Phase</dt>
                <dd>{status.statusPhase}</dd>
              </div>
              <div className="status-item">
                <dt>Record</dt>
                <dd>
                  <StateIndicator
                    variant={nodeMod === "live" ? "live" : nodeMod === "sealed" ? "sealed" : "rest"}
                  />
                  {status.statusRecord}
                </dd>
              </div>
            </dl>
          </header>
          <main className="well-frame pane">
            <article className="well is-revealing" aria-live="polite" aria-atomic="true">
              <Well
                view={viewPhase}
                snap={snap}
                briefingAcked={briefingAcked}
                onAck={setBriefingAcked}
              />
            </article>
          </main>
          <footer className="action-row">
            <p className="action-note bar-note">Synthetic demonstration content — no real participant data.</p>
            <div className="action-keys">
              <Actions
                view={viewPhase}
                snap={snap}
                demo={demo}
                briefingAcked={briefingAcked}
                onCommit={(next, msg) => {
                  setDemo(next);
                  setView(null);
                  announce(msg);
                }}
                onReturn={() => {
                  setView(null);
                  announce(`Returned to ${JOURNEY_PHASES.find((p) => p.id === snap.current)?.label} phase.`);
                }}
              />
            </div>
          </footer>
        </div>
      </div>
      <Announcer message={message} />
      <SignOutCeremony open={signOutOpen} onClose={() => setSignOutOpen(false)} />
    </>
  );
}

function DocGlyph({ variant }: { variant?: string }) {
  return (
    <svg className={`doc-glyph${variant ? ` ${variant}` : ""}`} viewBox="0 0 12 14">
      <path d="M1 .5h6.5L11 4v9.5H1z" />
      <path d="M7.5 .5V4H11" />
    </svg>
  );
}

function Well({
  view,
  snap,
  briefingAcked,
  onAck,
}: {
  view: string;
  snap: (typeof JOURNEY_DEMOS)[JourneyDemo];
  briefingAcked: boolean;
  onAck: (v: boolean) => void;
}) {
  if (view === "briefing") {
    const isCurrent = snap.current === "briefing";
    return (
      <>
        <header className="well-head">
          <h2 className="well-title">Assignment briefing</h2>
          <p className="well-ident">Enrollment ENR-7C19-8842 · Participant CND-8842-19</p>
        </header>
        <section className="well-sec">
          <h3>What you are completing</h3>
          <p>
            A text examination on your case study, <em>Real-time Inventory &amp; Order Management at Scale</em>. Configuration for this cohort was frozen at activation — every participant receives the same tasks, timing rules, and examiner conduct.
          </p>
        </section>
        <section className="well-sec">
          <h3>Before you begin</h3>
          <ul>
            <li>Complete required submission work before starting the timed examination.</li>
            <li>
              The examination is a governed conversation with an <strong>AI Agent</strong>, not a person. Session protocol and rules are presented again when you enter.
            </li>
            <li>
              Your result becomes visible only after human review and audited <strong>Release</strong>.
            </li>
          </ul>
        </section>
        {isCurrent ? (
          <AcknowledgmentGate id="ackBox" className="well-ack" checked={briefingAcked} onChange={onAck}>
            I acknowledge the assignment requirements and consent to participate under these rules.
          </AcknowledgmentGate>
        ) : (
          <p className="well-complete-mark">Briefing acknowledged and recorded.</p>
        )}
      </>
    );
  }
  if (view === "submission") {
    const isCurrent = snap.current === "submission";
    return (
      <>
        <header className="well-head">
          <h2 className="well-title">Submission</h2>
          <p className="well-ident">Task · Case study upload · Versioned preservation</p>
        </header>
        <section className="well-sec">
          <h3>Required work</h3>
          <p>Upload your written case study and any permitted attachments. Later versions are preserved — nothing is silently replaced.</p>
        </section>
        <section className="well-sec">
          <h3>Preserved versions</h3>
          <ol className="version-list">
            <li className="version-row version-row--current">
              <span className="version-doc" aria-hidden="true">
                <DocGlyph variant="doc-glyph--current" />
              </span>
              <span className="version-tag">v2</span>
              <span className="version-name">inventory-order-mgmt-case-study.pdf</span>
              <span className="version-meta">Synthetic · 248 KB · preserved</span>
            </li>
            <li className="version-row">
              <span className="version-doc" aria-hidden="true">
                <DocGlyph />
              </span>
              <span className="version-tag">v1</span>
              <span className="version-name">inventory-order-mgmt-draft.pdf</span>
              <span className="version-meta">Synthetic · 231 KB · superseded</span>
            </li>
          </ol>
        </section>
        <div className="instrument-plate instrument-plate--dim">
          <span className="instrument-label">Upload channel</span>
          <p className="instrument-value">Not implemented in this prototype. Production will accept permitted file types here.</p>
        </div>
        {isCurrent ? (
          <p className="well-hint">Mark submission complete to unlock the examination when your cohort permits.</p>
        ) : (
          <p className="well-complete-mark">Submission recorded for Attempt 1.</p>
        )}
      </>
    );
  }
  if (view === "examination") {
    if (snap.examination === "locked") {
      return (
        <>
          <header className="well-head">
            <h2 className="well-title">Examination</h2>
          </header>
          <div className="instrument-plate instrument-plate--dim">
            <span className="instrument-label">Access</span>
            <p className="instrument-value">Complete briefing and submission before the text session unlocks.</p>
          </div>
        </>
      );
    }
    if (snap.examination === "ready") {
      return (
        <>
          <header className="well-head">
            <h2 className="well-title">Examination ready</h2>
            <p className="well-ident">Session 07 · Attempt 1 · 45 minutes allotted</p>
          </header>
          <section className="well-sec">
            <h3>Text session</h3>
            <p>Enter the examination console to review session protocol, acknowledge recording rules, and begin your timed conversation with the governed Examiner Agent.</p>
          </section>
          <dl className="session-readout">
            <div>
              <dt>Session ID</dt>
              <dd>FXA-7C19-2A07</dd>
            </div>
            <div>
              <dt>Stages</dt>
              <dd>5 examination exchanges</dd>
            </div>
            <div>
              <dt>Isolation</dt>
              <dd>One participant · one session</dd>
            </div>
          </dl>
        </>
      );
    }
    if (snap.examination === "active") {
      return (
        <>
          <header className="well-head">
            <h2 className="well-title">Examination in progress</h2>
            <p className="well-ident">Session 07 · Record open</p>
          </header>
          <section className="well-sec">
            <h3>Return to session</h3>
            <p>Your timed text session remains active. Time remaining, stage, and transcript are maintained on the examination console.</p>
          </section>
          <div className="instrument-plate">
            <span className="instrument-label">Session state</span>
            <p className="instrument-value">Live · examination stage in progress</p>
          </div>
        </>
      );
    }
    return (
      <>
        <header className="well-head">
          <h2 className="well-title">Examination complete</h2>
          <p className="well-ident">Session 07 · Submitted for evaluation</p>
        </header>
        <section className="well-sec">
          <p>Your examination record has been transmitted. Human reviewers inspect evidence and evaluation before any result is released.</p>
        </section>
        <p className="well-complete-mark">Examination closed · awaiting release</p>
      </>
    );
  }
  if (snap.result === "locked") {
    return (
      <>
        <header className="well-head">
          <h2 className="well-title">Result</h2>
        </header>
        <div className="instrument-plate instrument-plate--dim">
          <span className="instrument-label">Visibility</span>
          <p className="instrument-value">Results appear only after audited Release. Complete prior phases first.</p>
        </div>
      </>
    );
  }
  if (snap.result === "pending") {
    return (
      <>
        <header className="well-head">
          <h2 className="well-title">Awaiting release</h2>
          <p className="well-ident">Evaluation under human review</p>
        </header>
        <section className="well-sec">
          <p>Your submission, examination transcript, and evidence-backed evaluation are with reviewers. No participant-facing outcome is available until Release is recorded.</p>
        </section>
        <dl className="session-readout">
          <div>
            <dt>Session</dt>
            <dd>07 · FXA-7C19-2A07</dd>
          </div>
          <div>
            <dt>Enrollment</dt>
            <dd>ENR-7C19-8842</dd>
          </div>
          <div>
            <dt>Release</dt>
            <dd>Not yet issued</dd>
          </div>
        </dl>
      </>
    );
  }
  return (
    <>
      <header className="well-head">
        <svg className="well-seal" viewBox="0 0 52 52" aria-hidden="true">
          <circle cx="26" cy="26" r="24" />
          <path d="M15 27l8 8 15-17" />
        </svg>
        <h2 className="well-title">Result released</h2>
        <p className="well-ident">Release RLS-2026-0842 · Synthetic record</p>
      </header>
      <section className="well-sec">
        <p>Your audited result has been released to this enrollment. The participant result surface — criterion feedback, scores, and evidence references — is not yet built in this workspace.</p>
      </section>
    </>
  );
}

function Actions({
  view,
  snap,
  briefingAcked,
  onCommit,
  onReturn,
}: {
  view: string;
  snap: (typeof JOURNEY_DEMOS)[JourneyDemo];
  demo: JourneyDemo;
  briefingAcked: boolean;
  onCommit: (next: JourneyDemo, msg: string) => void;
  onReturn: () => void;
}) {
  if (view === "briefing" && snap.current === "briefing") {
    return (
      <Key variant="begin" disabled={!briefingAcked} onClick={() => onCommit("submission", "Briefing acknowledged. Submission phase is now current.")}>
        <span>Acknowledge &amp; Continue</span>
        <TransmitChevron />
      </Key>
    );
  }
  if (view === "submission" && snap.current === "submission") {
    return (
      <Key variant="begin" onClick={() => onCommit("examination-ready", "Submission complete. Examination is ready to enter.")}>
        <span>Mark Submission Complete</span>
        <TransmitChevron />
      </Key>
    );
  }
  if (view === "examination" && snap.examination === "ready") {
    return (
      <Key variant="begin" to="/participant-session">
        <span>Enter Session</span>
        <TransmitChevron />
      </Key>
    );
  }
  if (view === "examination" && snap.examination === "active") {
    return (
      <Key variant="begin" to="/participant-session">
        <span>Return to Session</span>
        <TransmitChevron />
      </Key>
    );
  }
  if (view === "result" && snap.result === "pending" && view === snap.current) {
    return (
      <div className="action-status-plate" role="status">
        <span className="instrument-label">Release</span>
        <p className="action-status">Awaiting audited Release — no action required</p>
      </div>
    );
  }
  if (view === "result" && snap.result === "released") {
    return (
      <Key disabled>View Result — not built</Key>
    );
  }
  if (view !== snap.current) {
    return (
      <Key onClick={onReturn}>Return to current phase</Key>
    );
  }
  return null;
}
