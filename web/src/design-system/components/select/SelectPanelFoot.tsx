import type { ReactNode } from "react";
import { Key } from "../keys/Key";

export function SelectPanelFoot({
  leading,
  doneLabel = "Done",
  onDone,
  className,
}: {
  leading?: ReactNode;
  doneLabel?: string;
  onDone: () => void;
  className?: string;
}) {
  return (
    <div
      className={`multiselect-foot${leading ? "" : " multiselect-foot--trailing"}${className ? ` ${className}` : ""}`}
    >
      {leading}
      <Key variant="quiet" size="compact" onClick={onDone}>
        {doneLabel}
      </Key>
    </div>
  );
}
