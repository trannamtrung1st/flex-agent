import { PhaseSpine, type PhaseSpineNode } from "../../design-system";

export type AssignmentStationView = "submission" | "attempt";

const NODES = [
  { id: "submission" as const, label: "Submission", short: "Prepare and accept a version" },
  { id: "attempt" as const, label: "Attempt", short: "Not available here" },
] as const;

export function AssignmentSpine({
  view,
  onSelect,
}: {
  view: AssignmentStationView;
  onSelect: (view: AssignmentStationView) => void;
}) {
  const nodes: PhaseSpineNode[] = NODES.map((node) => {
    const viewing = view === node.id;
    const locked = node.id === "attempt";
    const state = locked ? "locked" : (viewing ? "current" : "rest");
    const ariaLabel = locked
      ? `${node.label} — not available from this application`
      : `${node.label} — ${node.short}`;

    return {
      id: node.id,
      label: node.label,
      short: node.short,
      state,
      viewing,
      ariaLabel,
      onSelect: () => onSelect(node.id),
    };
  });

  return <PhaseSpine nodes={nodes} aria-label="Assignment phases" />;
}
