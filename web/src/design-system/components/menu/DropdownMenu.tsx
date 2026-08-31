import {
  useCallback,
  useEffect,
  useId,
  useRef,
  type KeyboardEvent as ReactKeyboardEvent,
  type ReactNode,
  type Ref,
} from "react";
import { AnchoredOverlay } from "../overlays/AnchoredOverlay";
import { overlayPlateClass } from "../overlays/overlayPlate";
import { useOverlayDismiss } from "../overlays/useOverlayDismiss";
import { enabledMenuItems, stepMenuIndex } from "./dropdownMenuLogic";

export type DropdownMenuAlign = "start" | "end" | "stretch";
/** Retained for callers. Both values portal through `placeFloating`. */
export type DropdownMenuPlacement = "connected" | "fixed";

export type DropdownMenuTriggerBind = {
  ref: Ref<HTMLButtonElement>;
  id?: string;
  "aria-haspopup": "menu";
  "aria-expanded": boolean;
  "aria-controls"?: string;
  onClick: () => void;
  onKeyDown: (event: ReactKeyboardEvent<HTMLButtonElement>) => void;
};

export function DropdownMenuItem({
  children,
  disabled,
  disabledNative,
  destructive,
  className,
  onSelect,
}: {
  children: ReactNode;
  disabled?: boolean;
  disabledNative?: boolean;
  destructive?: boolean;
  className?: string;
  onSelect?: () => void;
}) {
  return (
    <button
      type="button"
      role="menuitem"
      className={`menu-row command-menu-item${destructive ? " command-menu-item--destructive" : ""}${className ? ` ${className}` : ""}`}
      disabled={disabledNative || undefined}
      aria-disabled={disabled && !disabledNative ? true : undefined}
      tabIndex={disabled || disabledNative ? -1 : 0}
      onClick={() => {
        if (disabled || disabledNative) return;
        onSelect?.();
      }}
    >
      {children}
    </button>
  );
}

export function DropdownMenuSeparator() {
  return <div className="command-menu-sep menu-separator" role="separator" />;
}

export function DropdownMenu({
  open,
  onOpenChange,
  trigger,
  children,
  align = "end",
  placement: _placement = "connected",
  focusOnOpen = true,
  labelledBy,
  label,
  menuId,
  className,
  panelClassName,
  triggerDisabled,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  trigger: (bind: DropdownMenuTriggerBind) => ReactNode;
  children: ReactNode;
  align?: DropdownMenuAlign;
  placement?: DropdownMenuPlacement;
  focusOnOpen?: boolean;
  labelledBy?: string;
  label?: string;
  menuId?: string;
  className?: string;
  panelClassName?: string;
  triggerDisabled?: boolean;
}) {
  const triggerRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const shellRef = useRef<HTMLDivElement>(null);
  const focusFirstRef = useRef(false);
  const generatedId = useId();
  const panelId = menuId ?? generatedId;
  void _placement;

  const close = useCallback(() => {
    onOpenChange(false);
    triggerRef.current?.focus();
  }, [onOpenChange]);

  useOverlayDismiss(open, [triggerRef, menuRef], () => onOpenChange(false));

  useEffect(() => {
    if (!open) return;
    if (!focusOnOpen && !focusFirstRef.current) return;
    focusFirstRef.current = false;
    const first = enabledMenuItems(menuRef.current)[0] ?? menuRef.current?.querySelector<HTMLButtonElement>("[role='menuitem']");
    first?.focus();
  }, [focusOnOpen, open]);

  const onMenuKeyDown = (event: ReactKeyboardEvent<HTMLDivElement>) => {
    const list = enabledMenuItems(menuRef.current);
    const idx = list.indexOf(document.activeElement as HTMLButtonElement);
    if (event.key === "ArrowDown") {
      event.preventDefault();
      list[stepMenuIndex(list.length, idx, 1)]?.focus();
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      list[stepMenuIndex(list.length, idx, -1)]?.focus();
    } else if (event.key === "Home") {
      event.preventDefault();
      list[0]?.focus();
    } else if (event.key === "End") {
      event.preventDefault();
      list[list.length - 1]?.focus();
    } else if (event.key === "Escape") {
      event.preventDefault();
      close();
    } else if (event.key === "Tab") {
      close();
    }
  };

  const bind: DropdownMenuTriggerBind = {
    ref: triggerRef,
    id: labelledBy,
    "aria-haspopup": "menu",
    "aria-expanded": open,
    "aria-controls": open ? panelId : undefined,
    onClick: () => {
      if (triggerDisabled) return;
      onOpenChange(!open);
    },
    onKeyDown: (event) => {
      if (triggerDisabled) return;
      if (event.key === "ArrowDown") {
        event.preventDefault();
        if (open) {
          enabledMenuItems(menuRef.current)[0]?.focus();
        } else {
          focusFirstRef.current = true;
          onOpenChange(true);
        }
        return;
      }
      if (!open && (event.key === "Enter" || event.key === " ")) {
        event.preventDefault();
        focusFirstRef.current = true;
        onOpenChange(true);
      }
      if (event.key === "Escape" && open) {
        event.preventDefault();
        close();
      }
    },
  };

  return (
    <div ref={shellRef} className={`menu-shell menu-shell--${align}${className ? ` ${className}` : ""}`}>
      {trigger(bind)}
      <AnchoredOverlay
        open={open}
        triggerRef={triggerRef}
        tokenSourceRef={shellRef}
        floatingRef={menuRef}
        align={align}
      >
        {({ ref, style, overlayClassName }) => (
          <div
            ref={ref}
            id={panelId}
            className={overlayPlateClass("menu-popover", "command-menu", "menu-popover--fixed", overlayClassName, panelClassName)}
            role="menu"
            aria-labelledby={labelledBy}
            aria-label={label}
            style={style}
            onKeyDown={onMenuKeyDown}
          >
            {children}
          </div>
        )}
      </AnchoredOverlay>
    </div>
  );
}
