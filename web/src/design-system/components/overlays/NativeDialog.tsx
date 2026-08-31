import { useEffect, useRef, type KeyboardEvent, type ReactNode, type SyntheticEvent } from "react";

const NESTED_OVERLAY = [
  '[aria-expanded="true"][aria-haspopup="dialog"]',
  '[aria-expanded="true"][aria-haspopup="listbox"]',
  '[aria-expanded="true"][aria-haspopup="menu"]',
].join(", ");

function nestedOverlayExpanded(dialog: HTMLElement | null) {
  return Boolean(dialog?.querySelector(NESTED_OVERLAY));
}

export function NativeDialog({
  open,
  onClose,
  className,
  labelledBy,
  id,
  children,
}: {
  open: boolean;
  onClose: () => void;
  className: string;
  labelledBy: string;
  id?: string;
  children: ReactNode;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const triggerRef = useRef<HTMLElement | null>(null);
  const suppressEscapeClose = useRef(false);

  useEffect(() => {
    const node = ref.current;
    if (!node) return;
    if (open && !node.open) {
      triggerRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
      node.showModal();
    }
    if (!open && node.open) {
      const restore = triggerRef.current;
      node.close();
      requestAnimationFrame(() => {
        if (!restore?.isConnected) return;
        if (restore instanceof HTMLButtonElement && restore.disabled) return;
        if (restore.getAttribute("aria-disabled") === "true") return;
        restore.focus();
      });
    }
  }, [open]);

  function onKeyDownCapture(event: KeyboardEvent<HTMLDialogElement>) {
    if (event.key !== "Escape") return;
    if (nestedOverlayExpanded(ref.current)) {
      suppressEscapeClose.current = true;
    }
  }

  function onKeyUpCapture(event: KeyboardEvent<HTMLDialogElement>) {
    if (event.key !== "Escape") return;
    if (!nestedOverlayExpanded(ref.current)) {
      suppressEscapeClose.current = false;
    }
  }

  function onCancel(event: SyntheticEvent<HTMLDialogElement>) {
    event.preventDefault();
    if (suppressEscapeClose.current) {
      suppressEscapeClose.current = false;
      return;
    }
    onClose();
  }

  return (
    <dialog
      ref={ref}
      id={id}
      className={className}
      aria-labelledby={labelledBy}
      onClose={onClose}
      onCancel={onCancel}
      onKeyDownCapture={onKeyDownCapture}
      onKeyUpCapture={onKeyUpCapture}
    >
      <div className="dialog-stage">{children}</div>
    </dialog>
  );
}
