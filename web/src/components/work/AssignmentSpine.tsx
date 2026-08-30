import { cx } from "../../lib/cx";

export type AssignmentStationView = "submission" | "attempt";

const NODES = [
  { id: "submission" as const, label: "Submission", short: "Prepare and accept a version" },
  { id: "attempt" as const, label: "Attempt", short: "Not available here" },
] as const;

function PhaseGlyph({ locked }: { locked: boolean }) {
  if (locked) {
    return (
      <svg className="phase-glyph phase-glyph--locked" viewBox="0 0 14 14" aria-hidden="true">
        <rect x="3.2" y="6.4" width="7.6" height="5.4" />
        <path d="M4.9 6.4V4.9a2.1 2.1 0 0 1 4.2 0v1.5" />
      </svg>
    );
  }
  return <span className="phase-node-dot" />;
}

export function AssignmentSpine({
  view,
  onSelect,
}: {
  view: AssignmentStationView;
  onSelect: (view: AssignmentStationView) => void;
}) {
  return (
    <nav className="phase-spine" aria-label="Assignment phases">
      <ol className="phase-list">
        {NODES.map((node, index) => {
          const viewing = view === node.id;
          const locked = node.id === "attempt";
          const nodeState = locked ? "locked" : "current";
          const name = locked
            ? `${node.label} — not available from this application`
            : `${node.label} — ${node.short}`;
          return (
            <li className="phase-item" key={node.id}>
              <button
                type="button"
                className={cx("phase-node", `phase-node--${nodeState}`, viewing && "is-viewing")}
                aria-current={viewing ? "step" : undefined}
                aria-label={name}
                onClick={() => onSelect(node.id)}
              >
                <span className="phase-marker" aria-hidden="true">
                  <PhaseGlyph locked={locked} />
                  {index < NODES.length - 1 ? <span className="phase-trace" /> : null}
                </span>
                <span className="phase-copy">
                  <span className="phase-label">{node.label}</span>
                  <span className="phase-short">{node.short}</span>
                </span>
              </button>
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
