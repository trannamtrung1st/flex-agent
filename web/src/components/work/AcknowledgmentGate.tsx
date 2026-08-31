import type { ReactNode } from "react";
import { cx } from "../../lib/cx";
import { ControlLine } from "../../design-system/components/fields/ControlLine";

export function AcknowledgmentGate({
  id,
  checked,
  onChange,
  children,
  presentation = "plate",
  className,
}: {
  id: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  children: ReactNode;
  /** `plate` seats the bordered briefing acknowledgement; `inline` is the compact dialog control line. */
  presentation?: "plate" | "inline";
  className?: string;
}) {
  return (
    <ControlLine
      id={id}
      className={cx(presentation === "plate" && "briefing-ack", className)}
      markClassName="ack-mark"
      checked={checked}
      onChange={(checked) => onChange(checked)}
    >
      {children}
    </ControlLine>
  );
}
