import { useRef } from "react";
import { TooltipHost } from "../../design-system";

export function AgentStatusLine({ children }: { children: string }) {
  const lineRef = useRef<HTMLSpanElement>(null);
  return (
    <TooltipHost
      className="agent-line-host"
      tip={children}
      tone="value"
      wrap
      tipOnlyWhenTruncated
      truncationAxis="block"
      truncationRef={lineRef}
      placementRef={lineRef}
      openOnPress
    >
      <span ref={lineRef} className="agent-line">{children}</span>
    </TooltipHost>
  );
}

export function StageBars({ stage, total, complete }: { stage: number; total: number; complete?: boolean }) {
  return (
    <div className="stage-bars" aria-hidden="true">
      {Array.from({ length: total }, (_, i) => (
        <span
          key={i}
          className={complete || i < stage - 1 ? "is-done" : !complete && i === stage - 1 ? "is-now" : undefined}
        />
      ))}
    </div>
  );
}

export function ProtocolPlate({ label, value }: { label: string; value: string }) {
  return (
    <div className="protocol-plate pane pane--dim pane--br">
      <span className="protocol-label">{label}</span>
      <span className="protocol-value">{value}</span>
    </div>
  );
}
