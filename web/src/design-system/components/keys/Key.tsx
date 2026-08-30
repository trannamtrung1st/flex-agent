import { Link, type To } from "react-router-dom";
import { forwardRef, useId, useRef, type KeyboardEvent, type ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { TooltipHost } from "./TooltipHost";

export type KeySize = "compact" | "standard" | "large";
export type KeyVariant = "quiet" | "transmit" | "open" | "begin" | "activate" | "inspect" | "release" | "back";

function keySizeClass(size: KeySize) {
  if (size === "compact") return "key--compact";
  if (size === "large") return "key--large";
  return undefined;
}

function isTextLabel(children: ReactNode) {
  return typeof children === "string" || typeof children === "number";
}

export const Key = forwardRef<HTMLButtonElement, {
  variant?: KeyVariant;
  size?: KeySize;
  waiting?: boolean;
  /** Horizontal ellipsis when the caption can clip. Do not use this to stretch keys in a group. */
  truncate?: boolean;
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
  truncate,
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
  const labelRef = useRef<HTMLSpanElement>(null);
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

  const cls = cx("key", `key--${variant}`, keySizeClass(size), waiting && "is-waiting", truncate && "key--truncate", className);
  const caption = isTextLabel(children) ? String(children) : undefined;
  const distinctTip = tooltip && caption && tooltip === caption ? undefined : tooltip;
  const truncateTip = truncate ? distinctTip ?? caption : undefined;
  const reasonPlaque = Boolean(disabled && disabledReason);
  const plaque = reasonPlaque ? disabledReason : (truncateTip ?? distinctTip);

  const reasonNode = disabled && disabledReason ? (
    <span id={reasonId} className="visually-hidden">
      {disabledReason}
    </span>
  ) : null;

  const labelNode =
    truncate || isTextLabel(children)
      ? <span ref={truncate ? labelRef : undefined} className="key-label">{children}</span>
      : children;

  if (to) {
    return (
      <TooltipHost
        tip={plaque}
        tipOnlyWhenTruncated={truncate && !reasonPlaque}
        truncationRef={truncate && !reasonPlaque ? labelRef : undefined}
      >
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
          {waiting ? <span className="wait-mark" aria-hidden="true" /> : null}
          {labelNode}
        </Link>
        {reasonNode}
      </TooltipHost>
    );
  }

  return (
    <TooltipHost
      tip={plaque}
      tipOnlyWhenTruncated={truncate && !reasonPlaque}
      truncationRef={truncate && !reasonPlaque ? labelRef : undefined}
    >
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
        {labelNode}
      </button>
      {reasonNode}
    </TooltipHost>
  );
});
