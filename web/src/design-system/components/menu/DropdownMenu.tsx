import {
  useCallback,
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type KeyboardEvent as ReactKeyboardEvent,
  type ReactNode,
  type Ref,
} from "react";
import { createPortal } from "react-dom";
import { enabledMenuItems, stepMenuIndex } from "./dropdownMenuLogic";

export type DropdownMenuAlign = "start" | "end" | "stretch";
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
  placement = "connected",
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
  const focusFirstRef = useRef(false);
  const generatedId = useId();
  const panelId = menuId ?? generatedId;
  const [pos, setPos] = useState<{ top: number; left: number; width?: number }>({ top: 0, left: 0 });

  const close = useCallback(() => {
    onOpenChange(false);
    triggerRef.current?.focus();
  }, [onOpenChange]);

  const place = useCallback(() => {
    const triggerEl = triggerRef.current;
    const menu = menuRef.current;
    if (!triggerEl || !menu) return;
    const rect = triggerEl.getBoundingClientRect();
    const box = menu.getBoundingClientRect();
    let top = rect.bottom;
    let left = align === "start" ? rect.left : rect.right - box.width;
    if (align === "stretch") left = rect.left;
    if (top + box.height > window.innerHeight - 8) top = Math.max(8, rect.top - box.height);
    if (left < 8) left = Math.min(rect.left, window.innerWidth - box.width - 8);
    if (align === "stretch") {
      setPos({ top, left, width: rect.width });
      return;
    }
    if (left + box.width > window.innerWidth - 8) left = Math.max(8, window.innerWidth - box.width - 8);
    setPos({ top, left, width: undefined });
  }, [align]);

  useLayoutEffect(() => {
    if (!open || placement !== "fixed") return;
    place();
  }, [open, place, placement, children]);

  useEffect(() => {
    if (!open) return;
    const onPointer = (event: PointerEvent) => {
      const target = event.target as Node;
      if (menuRef.current?.contains(target) || triggerRef.current?.contains(target)) return;
      onOpenChange(false);
      triggerRef.current?.focus();
    };
    document.addEventListener("pointerdown", onPointer);
    return () => document.removeEventListener("pointerdown", onPointer);
  }, [open, onOpenChange]);

  useEffect(() => {
    if (!open || placement !== "fixed") return;
    const onScroll = () => onOpenChange(false);
    window.addEventListener("scroll", onScroll, true);
    window.addEventListener("resize", place);
    return () => {
      window.removeEventListener("scroll", onScroll, true);
      window.removeEventListener("resize", place);
    };
  }, [open, onOpenChange, place, placement]);

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

  const panel = open ? (
    <div
      ref={menuRef}
      id={panelId}
      className={`menu-popover select-popover popover-surface menu-surface command-menu${placement === "fixed" ? " menu-popover--fixed" : ""}${panelClassName ? ` ${panelClassName}` : ""}`}
      role="menu"
      aria-labelledby={labelledBy}
      aria-label={label}
      style={placement === "fixed" ? { top: pos.top, left: pos.left, width: pos.width } : undefined}
      onKeyDown={onMenuKeyDown}
    >
      {children}
    </div>
  ) : null;

  return (
    <div className={`menu-shell menu-shell--${align}${className ? ` ${className}` : ""}`}>
      {trigger(bind)}
      {placement === "fixed" && panel ? createPortal(panel, document.body) : panel}
    </div>
  );
}
