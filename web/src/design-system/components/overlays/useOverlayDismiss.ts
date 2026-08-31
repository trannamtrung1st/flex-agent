import { useEffect, useRef, type RefObject } from "react";

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

export type OverlayDismissOptions = {
  labelId?: string;
  controlId?: string;
  pointer?: boolean;
  focus?: boolean;
  scroll?: boolean;
};

export function useOverlayDismiss(
  open: boolean,
  rootRef: RefObject<HTMLElement | null> | Array<RefObject<HTMLElement | null>>,
  onDismiss: () => void,
  options?: OverlayDismissOptions,
) {
  const pointer = options?.pointer ?? true;
  const focus = options?.focus ?? true;
  const scroll = options?.scroll ?? true;
  const rootsRef = useRef(rootRef);
  const onDismissRef = useRef(onDismiss);

  useEffect(() => {
    rootsRef.current = rootRef;
    onDismissRef.current = onDismiss;
  });

  useEffect(() => {
    if (!open) return;

    const roots = () => {
      const current = rootsRef.current;
      return Array.isArray(current) ? current : [current];
    };

    const inside = (target: EventTarget | null) => {
      if (!(target instanceof Node)) return false;
      return roots().some((root) => root.current?.contains(target));
    };

    const associated = (target: EventTarget | null) =>
      target instanceof Node && isAssociatedLabelTarget(target, options);

    let dismissed = false;
    const dismissIfOutside = (target: EventTarget | null) => {
      if (dismissed) return;
      if (inside(target) || associated(target)) return;
      dismissed = true;
      onDismissRef.current();
    };

    const onPointer = (event: PointerEvent) => dismissIfOutside(event.target);
    const onFocusIn = (event: FocusEvent) => dismissIfOutside(event.target);
    const onScroll = (event: Event) => dismissIfOutside(event.target);

    if (pointer) document.addEventListener("pointerdown", onPointer);
    if (focus) document.addEventListener("focusin", onFocusIn);
    if (scroll) {
      window.addEventListener("scroll", onScroll, true);
      document.addEventListener("scroll", onScroll, true);
      window.visualViewport?.addEventListener("scroll", onScroll);
    }

    return () => {
      document.removeEventListener("pointerdown", onPointer);
      document.removeEventListener("focusin", onFocusIn);
      window.removeEventListener("scroll", onScroll, true);
      document.removeEventListener("scroll", onScroll, true);
      window.visualViewport?.removeEventListener("scroll", onScroll);
    };
  }, [focus, open, options?.controlId, options?.labelId, pointer, scroll]);
}
