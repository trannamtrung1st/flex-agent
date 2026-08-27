import { useEffect, type RefObject } from "react";

function isAssociatedLabelTarget(target: Node, options?: { labelId?: string; controlId?: string }) {
  if (options?.labelId) {
    const label = document.getElementById(options.labelId);
    if (label && (label === target || label.contains(target))) return true;
  }
  if (options?.controlId) {
    const label = document.querySelector(`label[for="${CSS.escape(options.controlId)}"]`);
    if (label && (label === target || label.contains(target))) return true;
  }
  return false;
}

export function useDismissOnOutsidePointer(
  open: boolean,
  rootRef: RefObject<HTMLElement | null>,
  onDismiss: (event: PointerEvent) => void,
  options?: { labelId?: string; controlId?: string },
) {
  useEffect(() => {
    if (!open) return;
    const onPointer = (event: PointerEvent) => {
      const target = event.target as Node;
      if (rootRef.current?.contains(target)) return;
      if (isAssociatedLabelTarget(target, options)) return;
      onDismiss(event);
    };
    document.addEventListener("pointerdown", onPointer);
    return () => document.removeEventListener("pointerdown", onPointer);
  }, [onDismiss, open, options?.controlId, options?.labelId, rootRef]);
}
