import { PhaseSpine, type PhaseSpineNode } from "../../design-system";

export type AssignmentStationView = "submission" | "attempt";

export function AssignmentSpine({
  view,
  onSelect,
  attemptShort = "Readiness and start",
}: {
  view: AssignmentStationView;
  onSelect: (view: AssignmentStationView) => void;
  attemptShort?: string;
}) {
  const nodes: PhaseSpineNode[] = [
    {
      id: "submission",
      label: "Submission",
      short: "Prepare and accept a version",
      state: view === "submission" ? "current" : "rest",
      viewing: view === "submission",
      ariaLabel: "Submission — Prepare and accept a version",
      onSelect: () => onSelect("submission"),
    },
    {
      id: "attempt",
      label: "Attempt",
      short: attemptShort,
      state: view === "attempt" ? "current" : "rest",
      viewing: view === "attempt",
      ariaLabel: `Attempt — ${attemptShort}`,
      onSelect: () => onSelect("attempt"),
    },
  ];

  return <PhaseSpine nodes={nodes} aria-label="Assignment phases" />;
}
