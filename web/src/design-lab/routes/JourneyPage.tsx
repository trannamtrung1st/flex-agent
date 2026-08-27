import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  AcknowledgmentGate,
  Announcer,
  DemoPlate,
  Key,
  PARTICIPANT_HOME,
  PARTICIPANT_IDENTITY,
  ProfileMenu,
  ReadoutList,
  SignOutCeremony,
  StateIndicator,
  TransmitChevron,
  usePrototypeSignOut,
} from "../components";
import { GuidedTaskLayout, PlateStatusMark, Stack, WorkWell, WorkWellHead, WorkWellSection } from "../../design-system";
import { JOURNEY_DEMO_KEYS, JOURNEY_DEMOS, JOURNEY_PHASES, type JourneyDemo } from "../data/fixtures/journey";
import { useAnnouncer } from "../../lib/useAnnouncer";
import { useDemoParam } from "../lib/useDemoParam";
import { useSurface } from "../lib/useSurface";
import { isReachable, PhaseSpine } from "../features/journey/PhaseSpine";

const JOURNEY_DEMO_OPTIONS = [
  { value: "briefing", label: "First arrival — briefing" },
  { value: "submission", label: "Submission current" },
  { value: "examination-ready", label: "Examination ready" },
  { value: "examination-active", label: "Examination in progress" },
  { value: "result-pending", label: "Result not available" },
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
    if (viewPhase === "result") statusRecord = snap.result === "released" ? "Released" : "Result not available";
    return { statusPhase, statusRecord };
  }, [phase.statusPhase, snap, viewPhase]);

  const nodeMod = /live|in session/i.test(status.statusRecord)
    ? "live"
    : /released|ready/i.test(status.statusRecord)
      ? "sealed"
      : "";

  return (
    <GuidedTaskLayout
      railLabel="Assignment phases"
      brandSuffix="Assignment Station"
      brandExtras={
        <>
          <Link className="rail-home-link" to={PARTICIPANT_HOME}>
            <svg viewBox="0 0 10 10" aria-hidden="true" focusable="false">
              <path d="M6.5 1.5 L3 5 L6.5 8.5" fill="none" stroke="currentColor" strokeWidth="1.1" strokeLinecap="square" />
            </svg>
            Home
          </Link>
          <ProfileMenu identity={PARTICIPANT_IDENTITY} actions={actions} className="strip-profile--rail" />
        </>
      }
      instruments={
        <>
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
        </>
      }
      heading={
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
      }
      actions={
        <>
          <p className="action-note bar-note">Synthetic demonstration content — no real participant data.</p>
          <div className="action-keys">
            <Actions
              view={viewPhase}
              snap={snap}
              demo={demo}
              briefingAcked={briefingAcked}
              onAnnounce={announce}
              onReturn={() => {
                setView(null);
                announce(`Returned to ${JOURNEY_PHASES.find((p) => p.id === snap.current)?.label} phase.`);
              }}
            />
          </div>
        </>
      }
      overlays={
        <>
          <Announcer message={message} />
          <SignOutCeremony open={signOutOpen} onClose={() => setSignOutOpen(false)} />
        </>
      }
    >
      <Well
        view={viewPhase}
        snap={snap}
        briefingAcked={briefingAcked}
        onAck={setBriefingAcked}
      />
    </GuidedTaskLayout>
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
      <WorkWell
        revealing
        head={
          <WorkWellHead
            title="Assignment briefing"
            ident="Enrollment ENR-7C19-8842 · Participant CND-8842-19"
          />
        }
        foot={
          isCurrent ? (
            <AcknowledgmentGate id="ackBox" checked={briefingAcked} onChange={onAck}>
              I acknowledge the assignment requirements and consent to participate under these rules.
            </AcknowledgmentGate>
          ) : (
            <PlateStatusMark>Briefing acknowledged and recorded.</PlateStatusMark>
          )
        }
      >
        <WorkWellSection>
          <h3>What you are completing</h3>
          <p>
            A text examination on your case study, <em>Real-time Inventory &amp; Order Management at Scale</em>. Configuration for this cohort was frozen at activation — every participant receives the same tasks, timing rules, and examiner conduct.
          </p>
        </WorkWellSection>
        <WorkWellSection>
          <h3>Before you begin</h3>
          <ul>
            <li>Complete required submission work before starting the timed examination.</li>
            <li>
              The examination is a governed conversation with an <strong>AI Agent</strong>, not a person. Session protocol and rules are presented again when you enter.
            </li>
            <li>
              Your result becomes visible only after publication. Until then the Result stays unavailable.
            </li>
          </ul>
        </WorkWellSection>
      </WorkWell>
    );
  }
  if (view === "submission") {
    const isCurrent = snap.current === "submission";
    return (
      <WorkWell
        revealing
        head={<WorkWellHead title="Submission" ident="Task · Case study upload · Versioned preservation" />}
        foot={isCurrent ? undefined : <PlateStatusMark>Submission recorded for Attempt 1.</PlateStatusMark>}
      >
        <WorkWellSection>
          <h3>Required work</h3>
          <p>Upload your written case study and any permitted attachments. Later versions are preserved — nothing is silently replaced.</p>
        </WorkWellSection>
        <WorkWellSection>
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
        </WorkWellSection>
        <div className="instrument-plate instrument-plate--dim">
          <span className="instrument-label">Upload channel</span>
          <p className="instrument-value">Not implemented in this prototype. Production will accept permitted file types here.</p>
        </div>
        {isCurrent ? (
          <p className="work-well__hint">Submit a version. Attempt readiness is server-authoritative and is not granted from this control.</p>
        ) : null}
      </WorkWell>
    );
  }
  if (view === "examination") {
    if (snap.examination === "locked") {
      return (
        <WorkWell revealing head={<WorkWellHead title="Examination" />}>
          <div className="instrument-plate instrument-plate--dim">
            <span className="instrument-label">Access</span>
            <p className="instrument-value">Complete briefing and submission before the text session unlocks.</p>
          </div>
        </WorkWell>
      );
    }
    if (snap.examination === "ready") {
      return (
        <WorkWell
          revealing
          head={<WorkWellHead title="Examination ready" ident="Session 07 · Attempt 1 · 45 minutes allotted" />}
        >
          <WorkWellSection>
            <h3>Text session</h3>
            <p>Enter the examination console to review session protocol, acknowledge recording rules, and begin your timed conversation with the governed Examiner Agent.</p>
          </WorkWellSection>
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
        </WorkWell>
      );
    }
    if (snap.examination === "active") {
      return (
        <WorkWell revealing head={<WorkWellHead title="Examination in progress" ident="Session 07 · Record open" />}>
          <WorkWellSection>
            <h3>Return to session</h3>
            <p>Your timed text session remains active. Time remaining, stage, and transcript are maintained on the examination console.</p>
          </WorkWellSection>
          <div className="instrument-plate">
            <span className="instrument-label">Session state</span>
            <p className="instrument-value">Live · examination stage in progress</p>
          </div>
        </WorkWell>
      );
    }
    return (
      <WorkWell
        revealing
        head={<WorkWellHead title="Examination complete" ident="Session 07 · Submitted for evaluation" />}
        foot={<PlateStatusMark>Examination closed. Result not available until publication.</PlateStatusMark>}
      >
        <WorkWellSection>
          <p>Your examination record has been transmitted. Result not available until publication.</p>
        </WorkWellSection>
      </WorkWell>
    );
  }
  if (snap.result === "locked") {
    return (
      <WorkWell revealing head={<WorkWellHead title="Result" />}>
        <div className="instrument-plate instrument-plate--dim">
          <span className="instrument-label">Visibility</span>
          <p className="instrument-value">Results appear only after publication. Complete prior phases first.</p>
        </div>
      </WorkWell>
    );
  }
  if (snap.result === "pending") {
    return (
      <WorkWell revealing head={<WorkWellHead title="Result not available" ident="Enrollment ENR-7C19-8842" />}>
        <WorkWellSection>
          <p>Result not available. Return to your activity or use the provided support route if you need help.</p>
        </WorkWellSection>
      </WorkWell>
    );
  }
  return (
    <WorkWell
      revealing
      head={
        <WorkWellHead gap="none">
          <svg className="work-well__seal" viewBox="0 0 52 52" aria-hidden="true">
            <circle cx="26" cy="26" r="24" />
            <path d="M15 27l8 8 15-17" />
          </svg>
          <Stack gap="2">
            <h2 className="work-well__title">Result released</h2>
            <p className="work-well__ident">Synthetic published-result specimen</p>
          </Stack>
        </WorkWellHead>
      }
    >
      <WorkWellSection>
        <p>
          This design-lab plate shows publication chrome only. Participant-visible fields come from the
          frozen release policy on the server; this specimen does not invent scores, criteria, or reviewer notes.
        </p>
      </WorkWellSection>
    </WorkWell>
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

function Actions({
  view,
  snap,
  briefingAcked,
  onAnnounce,
  onReturn,
}: {
  view: string;
  snap: (typeof JOURNEY_DEMOS)[JourneyDemo];
  demo: JourneyDemo;
  briefingAcked: boolean;
  onAnnounce: (msg: string) => void;
  onReturn: () => void;
}) {
  if (view === "briefing" && snap.current === "briefing") {
    return (
      <div className="action-status-plate" role="status">
        <p className="action-status">
          {briefingAcked
            ? "Briefing acknowledged. The phase rail does not unlock Submission."
            : "Acknowledge the briefing in the assignment well. This control does not advance lifecycle."}
        </p>
      </div>
    );
  }
  if (view === "submission" && snap.current === "submission") {
    return (
      <Key variant="begin" onClick={() => onAnnounce("Version submitted in this design lab. Attempt readiness is server-authoritative and is not granted here.")}>
        <span>Submit version</span>
        <TransmitChevron />
      </Key>
    );
  }
  if (view === "examination" && snap.examination === "ready") {
    return (
      <Key
        variant="begin"
        onClick={() => onAnnounce("Start Attempt requires authoritative readiness. Opening the Session console here is a visual specimen only.")}
      >
        <span>Start Attempt</span>
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
      <Key variant="quiet" to="/participant-home">
        Return
      </Key>
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
