import { useEffect, useRef, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { Button } from "./Button";

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

interface DialogProps {
  open: boolean;
  title: string;
  children: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  confirmVariant?: "primary" | "danger";
  initialFocus?: "title" | "cancel";
  describedBy?: string;
  onConfirm: () => void;
  onCancel: () => void;
  isConfirming?: boolean;
  confirmDisabled?: boolean;
  hideConfirm?: boolean;
  tertiaryLabel?: string;
  onTertiary?: () => void;
  tertiaryDisabled?: boolean;
}

function focusableElements(container: HTMLElement): HTMLElement[] {
  return [...container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)].filter(
    (element) => !element.hasAttribute("disabled") && element.tabIndex !== -1,
  );
}

export function Dialog({
  open,
  title,
  children,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  confirmVariant = "primary",
  initialFocus = "cancel",
  describedBy,
  onConfirm,
  onCancel,
  isConfirming = false,
  confirmDisabled = false,
  hideConfirm = false,
  tertiaryLabel,
  onTertiary,
  tertiaryDisabled = false,
}: DialogProps) {
  const cancelRef = useRef<HTMLButtonElement>(null);
  const titleRef = useRef<HTMLHeadingElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const hostRef = useRef<HTMLDivElement>(null);
  const confirmIsDisabled = isConfirming || confirmDisabled;
  const onCancelRef = useRef(onCancel);

  useEffect(() => {
    onCancelRef.current = onCancel;
  }, [onCancel]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const previousFocus = document.activeElement as HTMLElement | null;
    if (initialFocus === "title") {
      titleRef.current?.focus();
    } else {
      cancelRef.current?.focus();
    }

    const host = hostRef.current;
    const inerted: HTMLElement[] = [];
    if (host) {
      for (const child of Array.from(document.body.children)) {
        if (child === host || !(child instanceof HTMLElement)) {
          continue;
        }
        if (child.hasAttribute("inert")) {
          continue;
        }
        child.setAttribute("inert", "");
        inerted.push(child);
      }
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onCancelRef.current();
        return;
      }

      if (event.key !== "Tab" || !panelRef.current) {
        return;
      }

      const focusable = focusableElements(panelRef.current);
      if (focusable.length === 0) {
        event.preventDefault();
        return;
      }

      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      const active = document.activeElement;
      const activeIsDisabledControl =
        active instanceof HTMLElement &&
        panelRef.current.contains(active) &&
        (active.hasAttribute("disabled") || active.getAttribute("aria-disabled") === "true");
      if (event.shiftKey) {
        if (active === first || !panelRef.current.contains(active) || activeIsDisabledControl) {
          event.preventDefault();
          last.focus();
        }
      } else if (active === last || !panelRef.current.contains(active) || activeIsDisabledControl) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener("keydown", handleKeyDown, true);

    return () => {
      document.removeEventListener("keydown", handleKeyDown, true);
      for (const element of inerted) {
        element.removeAttribute("inert");
      }
      previousFocus?.focus();
    };
  }, [open, initialFocus]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const active = document.activeElement;
    if (
      active instanceof HTMLElement &&
      panelRef.current?.contains(active) &&
      (active.hasAttribute("disabled") || active.getAttribute("aria-disabled") === "true")
    ) {
      cancelRef.current?.focus();
    }
  }, [open, confirmIsDisabled]);

  if (!open) {
    return null;
  }

  return createPortal(
    <div ref={hostRef} className="dialog-backdrop" role="presentation" onClick={onCancel}>
      <div
        ref={panelRef}
        className="dialog-panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="dialog-title"
        aria-describedby={describedBy}
        onClick={(event) => { event.stopPropagation(); }}
      >
        <div className="dialog-header">
          <h2 id="dialog-title" className="dialog-title" tabIndex={-1} ref={titleRef}>{title}</h2>
        </div>
        <div className="dialog-body">{children}</div>
        <div className="dialog-footer">
          <Button ref={cancelRef} variant="secondary" onClick={onCancel} disabled={isConfirming}>
            {cancelLabel}
          </Button>
          {tertiaryLabel && onTertiary ? (
            <Button type="button" onClick={onTertiary} disabled={isConfirming || tertiaryDisabled}>
              {tertiaryLabel}
            </Button>
          ) : null}
          {hideConfirm ? null : (
            <Button
              variant={confirmVariant}
              onClick={onConfirm}
              disabled={confirmIsDisabled}
              aria-busy={isConfirming}
            >
              {isConfirming ? "Working…" : confirmLabel}
            </Button>
          )}
        </div>
      </div>
    </div>,
    document.body,
  );
}
