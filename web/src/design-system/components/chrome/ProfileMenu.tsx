import { useState } from "react";
import { useId } from "react";
import { cx } from "../../../lib/cx";
import { ChevronGlyph } from "../glyphs/ChevronGlyph";
import { OperatorGlyph } from "../glyphs/OperatorGlyphs";
import { DropdownMenu, DropdownMenuItem, DropdownMenuSeparator } from "../menu";
import type { OperatorAction, OperatorIdentity } from "./operator";

export function ProfileMenu({
  identity,
  actions,
  ariaLabel,
  className,
}: {
  identity: OperatorIdentity;
  actions: OperatorAction[];
  ariaLabel?: string;
  className?: string;
}) {
  const [open, setOpen] = useState(false);
  const triggerId = useId();
  const named = identity.fullId.trim();
  const role = identity.role.trim();
  const label = ariaLabel ?? (
    named && named.toLowerCase() !== role.toLowerCase()
      ? `Operator menu, ${role.toLowerCase()} ${named}`
      : `Operator menu, ${role.toLowerCase() || named}`
  );
  const rail = Boolean(className?.includes("strip-profile--rail"));
  const standard = actions.filter((action) => action.intent !== "signout");
  const signOut = actions.filter((action) => action.intent === "signout");

  const activate = (action: OperatorAction) => {
    if (action.state === "disabled") return;
    setOpen(false);
    document.getElementById(triggerId)?.focus();
    action.onSelect?.();
  };

  return (
    <DropdownMenu
      open={open}
      onOpenChange={setOpen}
      align={rail ? "stretch" : "end"}
      focusOnOpen={false}
      labelledBy={triggerId}
      label="Operator menu"
      className={cx("strip-profile", className)}
      panelClassName="strip-profile-menu"
      trigger={(bind) => (
        <button
          ref={bind.ref}
          id={bind.id}
          type="button"
          className="strip-profile-key"
          aria-label={label}
          aria-haspopup={bind["aria-haspopup"]}
          aria-expanded={bind["aria-expanded"]}
          aria-controls={bind["aria-controls"]}
          onClick={bind.onClick}
          onKeyDown={bind.onKeyDown}
        >
          <OperatorGlyph />
          <span className="strip-profile-copy">
            <span className="strip-profile-id">{identity.shortId}</span>
          </span>
          <ChevronGlyph />
        </button>
      )}
    >
      <div className="strip-profile-head">
        <span className="strip-profile-role">{identity.role}</span>
        <span className="strip-profile-full">{identity.fullId}</span>
      </div>
      {standard.map((action) => {
        const disabled = action.state === "disabled";
        return (
          <DropdownMenuItem
            key={action.id}
            disabled={disabled}
            disabledNative={disabled}
            className="strip-profile-item"
            onSelect={() => activate(action)}
          >
            <span className="command-menu-item-label menu-row-label strip-profile-item-label">{action.label}</span>
            {disabled && action.disabledReason ? (
              <span className="command-menu-item-reason menu-row-reason strip-profile-item-note">{action.disabledReason}</span>
            ) : null}
          </DropdownMenuItem>
        );
      })}
      {standard.length > 0 && signOut.length > 0 ? <DropdownMenuSeparator /> : null}
      {signOut.map((action) => (
        <DropdownMenuItem
          key={action.id}
          className="strip-profile-item strip-profile-item--signout"
          onSelect={() => activate(action)}
        >
          <span className="command-menu-item-label menu-row-label strip-profile-item-label">{action.label}</span>
        </DropdownMenuItem>
      ))}
    </DropdownMenu>
  );
}
