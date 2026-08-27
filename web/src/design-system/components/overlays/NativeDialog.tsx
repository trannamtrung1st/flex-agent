import { useEffect, useRef, type ReactNode } from "react";

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

  return (
    <dialog
      ref={ref}
      id={id}
      className={className}
      aria-labelledby={labelledBy}
      onClose={onClose}
      onCancel={() => {
        onClose();
      }}
    >
      {children}
    </dialog>
  );
}
