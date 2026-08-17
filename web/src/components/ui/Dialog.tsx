import { useEffect, useRef, type ReactNode } from "react";
import { Button } from "./Button";

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
}: DialogProps) {
  const cancelRef = useRef<HTMLButtonElement>(null);
  const titleRef = useRef<HTMLHeadingElement>(null);

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

    return () => {
      previousFocus?.focus();
    };
  }, [open, initialFocus]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onCancel();
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => { document.removeEventListener("keydown", handleKeyDown); };
  }, [open, onCancel]);

  if (!open) {
    return null;
  }

  return (
    <div className="dialog-backdrop" role="presentation" onClick={onCancel}>
      <div
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
          <Button
            variant={confirmVariant}
            onClick={onConfirm}
            disabled={isConfirming}
            aria-busy={isConfirming}
          >
            {isConfirming ? "Working…" : confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  );
}
