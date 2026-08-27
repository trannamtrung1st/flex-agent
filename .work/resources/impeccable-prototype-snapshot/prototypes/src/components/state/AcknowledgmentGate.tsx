import type { ReactNode } from "react";
import { ControlLine } from "../fields/ControlLine";

export function AcknowledgmentGate({
  id,
  checked,
  onChange,
  children,
  className = "briefing-ack",
}: {
  id: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  children: ReactNode;
  className?: string;
}) {
  return (
    <ControlLine id={id} className={className} markClassName="ack-mark" checked={checked} onChange={onChange}>
      {children}
    </ControlLine>
  );
}
