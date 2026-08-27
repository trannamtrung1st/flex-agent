import { forwardRef, useId, type KeyboardEvent, type ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { TooltipHost } from "./TooltipHost";

export const IconButton = forwardRef<HTMLButtonElement, {
  label: string;
  children: ReactNode;
  tooltip?: string;
  disabledReason?: string;
  className?: string;
  id?: string;
  disabled?: boolean;
  expanded?: boolean;
  controls?: string;
  hasPopup?: "menu" | "listbox" | "dialog";
  onClick?: () => void;
  onKeyDown?: (event: KeyboardEvent<HTMLButtonElement>) => void;
}>(function IconButton({
  label,
  children,
  tooltip,
  disabledReason,
  className,
  id,
  disabled,
  expanded,
  controls,
  hasPopup,
  onClick,
  onKeyDown,
}, ref) {
  const reasonId = useId();
  const describedBy = disabled && disabledReason ? reasonId : undefined;
  const plaque = disabled && disabledReason ? disabledReason : tooltip;

  return (
    <TooltipHost tip={plaque}>
      <button
        ref={ref}
        id={id}
        className={cx("icon-button", className)}
        type="button"
        disabled={disabled}
        aria-label={label}
        aria-describedby={describedBy}
        aria-expanded={expanded}
        aria-controls={controls}
        aria-haspopup={hasPopup}
        onClick={onClick}
        onKeyDown={onKeyDown}
      >
        {children}
      </button>
      {disabled && disabledReason ? (
        <span id={reasonId} className="visually-hidden">
          {disabledReason}
        </span>
      ) : null}
    </TooltipHost>
  );
});
