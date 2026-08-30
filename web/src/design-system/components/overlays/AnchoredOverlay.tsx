import { useLayoutEffect, useRef, useState, type CSSProperties, type ReactNode, type Ref, type RefObject } from "react";
import { createPortal } from "react-dom";
import { overlayPortalRoot } from "./overlayPortalRoot";
import { placeFloating, type OverlayAlign, type OverlaySide } from "./placeFloating";

const OVERLAY_TOKEN_KEYS = [
  "--select-popover-max-height",
] as const;

export function copyOverlayTokens(from: HTMLElement, to: HTMLElement) {
  const styles = getComputedStyle(from);
  for (const key of OVERLAY_TOKEN_KEYS) {
    const value = styles.getPropertyValue(key).trim();
    if (value) to.style.setProperty(key, value);
  }
}

function assignRef<T>(ref: Ref<T> | undefined, value: T | null) {
  if (!ref) return;
  if (typeof ref === "function") ref(value);
  else ref.current = value;
}

export function mergeOverlayRefs<T>(...refs: Array<Ref<T> | undefined>) {
  return (value: T | null) => {
    for (const ref of refs) assignRef(ref, value);
  };
}

export function useFloatingPlacement({
  open,
  triggerRef,
  floatingRef,
  tokenSourceRef,
  preferredSide,
  align,
  offset = 0,
  size = false,
}: {
  open: boolean;
  triggerRef: RefObject<HTMLElement | null>;
  floatingRef: RefObject<HTMLElement | null>;
  tokenSourceRef?: RefObject<HTMLElement | null>;
  preferredSide: OverlaySide;
  align: OverlayAlign;
  offset?: number;
  size?: boolean;
}) {
  const [placed, setPlaced] = useState<{
    top: number;
    left: number;
    side: OverlaySide;
    width?: number;
    maxHeight?: number;
    connector: number;
  } | null>(null);

  useLayoutEffect(() => {
    if (!open) {
      setPlaced(null);
      return;
    }
    const measure = () => {
      const trigger = triggerRef.current;
      const floating = floatingRef.current;
      if (!trigger || !floating) return;
      const tokenSource = tokenSourceRef?.current;
      if (tokenSource) copyOverlayTokens(tokenSource, floating);
      const triggerBox = trigger.getBoundingClientRect();
      const visualViewport = window.visualViewport;
      const viewportWidth = visualViewport?.width ?? window.innerWidth;
      if (align === "stretch") {
        floating.style.width = `${Math.min(triggerBox.width, viewportWidth - 16)}px`;
        floating.style.minWidth = `${Math.min(triggerBox.width, viewportWidth - 16)}px`;
        floating.style.maxWidth = `${viewportWidth - 16}px`;
      } else {
        floating.style.minWidth = `${triggerBox.width}px`;
        floating.style.maxWidth = `${viewportWidth - 16}px`;
      }
      const box = floating.getBoundingClientRect();
      const next = placeFloating({
        trigger: { top: triggerBox.top, left: triggerBox.left, width: triggerBox.width, height: triggerBox.height },
        floating: { width: box.width, height: box.height },
        viewport: {
          width: viewportWidth,
          height: visualViewport?.height ?? window.innerHeight,
        },
        padding: 8,
        offset,
        preferredSide,
        align,
        size,
      });
      if (tokenSource) {
        tokenSource.classList.toggle("is-overlay-above", next.side === "top");
      }
      setPlaced(next);
    };
    measure();
    window.addEventListener("scroll", measure, true);
    window.addEventListener("resize", measure);
    window.visualViewport?.addEventListener("resize", measure);
    window.visualViewport?.addEventListener("scroll", measure);
    return () => {
      window.removeEventListener("scroll", measure, true);
      window.removeEventListener("resize", measure);
      window.visualViewport?.removeEventListener("resize", measure);
      window.visualViewport?.removeEventListener("scroll", measure);
      tokenSourceRef?.current?.classList.remove("is-overlay-above");
    };
  }, [align, offset, open, preferredSide, size, tokenSourceRef, triggerRef, floatingRef]);

  const style: CSSProperties = placed
    ? {
        top: placed.top,
        left: placed.left,
        width: placed.width,
        maxHeight: placed.maxHeight,
        ["--tip-connector" as string]: `${placed.connector}px`,
      }
    : { visibility: "hidden" };

  return { style, side: placed?.side ?? preferredSide, connector: placed?.connector ?? 0 };
}

export function AnchoredOverlay({
  open,
  triggerRef,
  tokenSourceRef,
  floatingRef,
  preferredSide = "bottom",
  align = "start",
  offset = 0,
  size = true,
  children,
}: {
  open: boolean;
  triggerRef: RefObject<HTMLElement | null>;
  tokenSourceRef: RefObject<HTMLElement | null>;
  floatingRef?: RefObject<HTMLElement | null>;
  preferredSide?: OverlaySide;
  align?: OverlayAlign;
  offset?: number;
  size?: boolean;
  children: (bind: {
    ref: (node: HTMLElement | null) => void;
    style: CSSProperties;
    overlayClassName: string;
  }) => ReactNode;
}) {
  const localRef = useRef<HTMLElement>(null);
  const resolvedRef = floatingRef ?? localRef;
  const { style, side } = useFloatingPlacement({
    open,
    triggerRef,
    floatingRef: resolvedRef,
    tokenSourceRef,
    preferredSide,
    align,
    offset,
    size,
  });

  if (!open) return null;

  return createPortal(
    children({
      ref: (node) => {
        resolvedRef.current = node;
      },
      style,
      overlayClassName: `floating-overlay${side === "top" ? " floating-overlay--above" : ""}`,
    }),
    overlayPortalRoot(triggerRef.current ?? tokenSourceRef.current),
  );
}
