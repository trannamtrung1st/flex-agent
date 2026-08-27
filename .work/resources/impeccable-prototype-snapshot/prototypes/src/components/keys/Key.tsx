import { Link, type To } from "react-router";
import { forwardRef, useId, type KeyboardEvent, type ReactNode } from "react";
import { cx } from "../../lib/cx";
import { TooltipHost } from "./TooltipHost";

export type KeySize = "compact" | "standard" | "large";
export type KeyVariant = "quiet" | "transmit" | "open" | "begin" | "activate" | "inspect" | "release" | "back";

function keySizeClass(size: KeySize) {
  if (size === "compact") return "key--compact";
  if (size === "large") return "key--large";
  return undefined;
}

export const Key = forwardRef<HTMLButtonElement, {
  variant?: KeyVariant;
  size?: KeySize;
  waiting?: boolean;
  type?: "button" | "submit";
  to?: To;
  disabled?: boolean;
  onClick?: () => void;
  onKeyDown?: (event: KeyboardEvent<HTMLButtonElement>) => void;
  children: ReactNode;
  className?: string;
  pressed?: boolean;
  id?: string;
  ariaLabel?: string;
  ariaExpanded?: boolean;
  ariaControls?: string;
  ariaHasPopup?: "menu" | "listbox" | "dialog";
  ariaDescribedBy?: string;
  tooltip?: string;
  disabledReason?: string;
}>(function Key({
  variant = "quiet",
  size = "standard",
  waiting,
  type = "button",
  to,
  disabled,
  onClick,
  onKeyDown,
  children,
  className,
  pressed,
  id,
  ariaLabel,
  ariaExpanded,
  ariaControls,
  ariaHasPopup,
  ariaDescribedBy,
  tooltip,
  disabledReason,
}, ref) {
  const reasonId = useId();
  const describedBy = cx(ariaDescribedBy, disabled && disabledReason ? reasonId : undefined) || undefined;
  const label = disabled && disabledReason && ariaLabel
    ? ariaLabel
    : disabled && disabledReason
      ? undefined
      : ariaLabel;
  const effectiveAriaLabel =
    disabled && disabledReason && !ariaLabel
      ? `${typeof children === "string" ? children : "Action"}. ${disabledReason}`
      : label;

  const cls = cx("key", `key--${variant}`, keySizeClass(size), waiting && "is-waiting", className);
  const plaque = tooltip ?? (disabled && disabledReason ? disabledReason : undefined);

  const reasonNode = disabled && disabledReason ? (
    <span id={reasonId} className="visually-hidden">
      {disabledReason}
    </span>
  ) : null;

  if (to) {
    return (
      <TooltipHost tip={plaque}>
        <Link
          id={id}
          className={cls}
          to={to}
          aria-disabled={disabled || undefined}
          tabIndex={disabled ? -1 : undefined}
          onClick={(event) => {
            if (disabled) {
              event.preventDefault();
              return;
            }
            onClick?.();
          }}
          aria-label={effectiveAriaLabel}
          aria-describedby={describedBy}
        >
          {children}
        </Link>
        {reasonNode}
      </TooltipHost>
    );
  }

  return (
    <TooltipHost tip={plaque}>
      <button
        ref={ref}
        id={id}
        className={cls}
        type={type}
        disabled={disabled}
        onClick={onClick}
        onKeyDown={onKeyDown}
        aria-busy={waiting || undefined}
        aria-pressed={pressed}
        aria-expanded={ariaExpanded}
        aria-controls={ariaControls}
        aria-haspopup={ariaHasPopup}
        aria-label={effectiveAriaLabel}
        aria-describedby={describedBy}
      >
        {waiting ? <span className="wait-mark" aria-hidden="true" /> : null}
        {children}
      </button>
      {reasonNode}
    </TooltipHost>
  );
});
