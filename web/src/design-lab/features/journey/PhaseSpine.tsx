import { PhaseSpine as PhaseSpineNav, type PhaseSpineNode } from "../../../design-system";
import { JOURNEY_PHASES, type JourneyDemo, JOURNEY_DEMOS } from "../../data/fixtures/journey";

type Snap = (typeof JOURNEY_DEMOS)[JourneyDemo];

export function isReachable(phaseId: string, snap: Snap) {
  return phaseId === snap.current || (snap.completed as readonly string[]).includes(phaseId);
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
  const nodes: PhaseSpineNode[] = JOURNEY_PHASES.map((phase) => {
    const nodeState = phase.id === snap.current
      ? "current"
      : (snap.completed as readonly string[]).includes(phase.id)
        ? "complete"
        : "locked";
    const reachable = isReachable(phase.id, snap);
    const viewing = phase.id === viewPhase;

    return {
      id: phase.id,
      label: phase.label,
      short: nodeState === "locked" ? "Awaiting prior phase" : phase.short,
      state: nodeState,
      viewing,
      disabled: !reachable,
      ariaLabel: `${phase.label} — ${nodeState === "locked" ? "not yet available" : phase.short}`,
      onSelect: () => onSelect(phase.id),
    };
  });

  return <PhaseSpineNav nodes={nodes} aria-label="Phase sequence" />;
}
