import { JOURNEY_PHASES, type JourneyDemo, JOURNEY_DEMOS } from "../../data/fixtures/journey";

type Snap = (typeof JOURNEY_DEMOS)[JourneyDemo];

export function isReachable(phaseId: string, snap: Snap) {
  return phaseId === snap.current || (snap.completed as readonly string[]).includes(phaseId);
}

function PhaseGlyph({ state }: { state: string }) {
  if (state === "complete") {
    return (
      <svg className="phase-glyph phase-glyph--complete" viewBox="0 0 14 14">
        <circle cx="7" cy="7" r="6.1" />
        <path d="M4.1 7.3l2.1 2.2 3.9-4.6" />
      </svg>
    );
  }
  if (state === "locked") {
    return (
      <svg className="phase-glyph phase-glyph--locked" viewBox="0 0 14 14">
        <rect x="3.2" y="6.4" width="7.6" height="5.4" />
        <path d="M4.9 6.4V4.9a2.1 2.1 0 0 1 4.2 0v1.5" />
      </svg>
    );
  }
  return <span className="phase-node-dot" />;
}

export function PhaseSpine({
  snap,
  viewPhase,
  onSelect,
}: {
  snap: Snap;
  viewPhase: string;
  onSelect: (phaseId: string) => void;
}) {
  return (
    <nav className="phase-spine" aria-label="Phase sequence">
      <ol className="phase-list">
        {JOURNEY_PHASES.map((p, index) => {
          const nodeState = p.id === snap.current ? "current" : (snap.completed as readonly string[]).includes(p.id) ? "complete" : "locked";
          const reachable = isReachable(p.id, snap);
          return (
            <li className="phase-item" key={p.id}>
              <button
                type="button"
                className={`phase-node phase-node--${nodeState}${p.id === viewPhase ? " is-viewing" : ""}`}
                disabled={!reachable}
                aria-current={p.id === viewPhase ? "step" : undefined}
                aria-label={`${p.label} — ${nodeState === "locked" ? "not yet available" : p.short}`}
                onClick={() => onSelect(p.id)}
              >
                <span className="phase-marker" aria-hidden="true">
                  <PhaseGlyph state={nodeState} />
                  {index < JOURNEY_PHASES.length - 1 ? <span className="phase-trace" /> : null}
                </span>
                <span className="phase-copy">
                  <span className="phase-label">{p.label}</span>
                  <span className="phase-short">{nodeState === "locked" ? "Awaiting prior phase" : p.short}</span>
                </span>
              </button>
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
