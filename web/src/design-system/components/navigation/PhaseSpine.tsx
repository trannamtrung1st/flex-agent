import { cx } from "../../../lib/cx";

export type PhaseSpineNodeState = "current" | "complete" | "locked" | "rest";

export type PhaseSpineNode = {
  id: string;
  label: string;
  short: string;
  state: PhaseSpineNodeState;
  viewing?: boolean;
  disabled?: boolean;
  ariaLabel: string;
  onSelect: () => void;
};

function PhaseGlyph({ state }: { state: PhaseSpineNodeState }) {
  if (state === "complete") {
    return (
      <svg className="phase-glyph phase-glyph--complete" viewBox="0 0 14 14" aria-hidden="true">
        <circle cx="7" cy="7" r="6.1" />
        <path d="M4.1 7.3l2.1 2.2 3.9-4.6" />
      </svg>
    );
  }
  if (state === "locked") {
    return (
      <svg className="phase-glyph phase-glyph--locked" viewBox="0 0 14 14" aria-hidden="true">
        <rect x="3.2" y="6.4" width="7.6" height="5.4" />
        <path d="M4.9 6.4V4.9a2.1 2.1 0 0 1 4.2 0v1.5" />
      </svg>
    );
  }
  return <span className="phase-node-dot" />;
}

export function PhaseSpine({
  nodes,
  "aria-label": ariaLabel,
}: {
  nodes: readonly PhaseSpineNode[];
  "aria-label": string;
}) {
  return (
    <nav className="phase-spine" aria-label={ariaLabel}>
      <ol className="phase-list">
        {nodes.map((node, index) => (
          <li className="phase-item" key={node.id}>
            <button
              type="button"
              className={cx(
                "phase-node",
                `phase-node--${node.state}`,
                node.viewing && "is-viewing",
              )}
              disabled={node.disabled}
              aria-current={node.viewing ? "step" : undefined}
              aria-label={node.ariaLabel}
              onClick={node.onSelect}
            >
              <span className="phase-marker" aria-hidden="true">
                <PhaseGlyph state={node.state} />
                {index < nodes.length - 1 ? <span className="phase-trace" /> : null}
              </span>
              <span className="phase-copy">
                <span className="phase-label">{node.label}</span>
                <span className="phase-short">{node.short}</span>
              </span>
            </button>
          </li>
        ))}
      </ol>
    </nav>
  );
}
