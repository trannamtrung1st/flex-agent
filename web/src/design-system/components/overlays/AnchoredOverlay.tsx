import { useLayoutEffect, useRef, useState, type CSSProperties, type ReactNode, type Ref, type RefObject } from "react";
import { createPortal } from "react-dom";
import { overlayPortalRoot } from "./overlayPortalRoot";
import { overlayBoxWidth, rewriteOverlayPercent } from "./overlayWidthTokens";
import { placeFloating, type OverlayAlign, type OverlaySide } from "./placeFloating";
import { OVERLAY_PLATE_OFFSET } from "./overlayPlate";

const OVERLAY_TOKEN_KEYS = [
  "--select-popover-width",
  "--select-popover-min-width",
  "--select-popover-max-width",
  "--select-popover-max-height",
] as const;

export function copyOverlayTokens(from: HTMLElement, to: HTMLElement, percentBasePx?: number) {
  const styles = getComputedStyle(from);
  const plateOwnsWidth = to.classList.contains("datetime-popover");
  for (const key of OVERLAY_TOKEN_KEYS) {
    if (plateOwnsWidth && key !== "--select-popover-max-height") continue;
    const value = styles.getPropertyValue(key).trim();
    if (!value) continue;
    if (value.includes("%")) {
      if (percentBasePx == null) continue;
      to.style.setProperty(key, rewriteOverlayPercent(value, percentBasePx));
      continue;
    }
    to.style.setProperty(key, value);
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
  lockMinWidthToTrigger = true,
}: {
  open: boolean;
  triggerRef: RefObject<HTMLElement | null>;
  floatingRef: RefObject<HTMLElement | null>;
  tokenSourceRef?: RefObject<HTMLElement | null>;
  preferredSide: OverlaySide;
  align: OverlayAlign;
  offset?: number;
  size?: boolean;
  lockMinWidthToTrigger?: boolean;
}) {
  const [placed, setPlaced] = useState<{
    top: number;
    left: number;
    side: OverlaySide;
    width?: number;
    minWidth?: number;
    maxWidth?: number;
    maxHeight?: number;
    connector: number;
  } | null>(null);

  useLayoutEffect(() => {
    if (!open) return;
    const locate = () => {
      const trigger = triggerRef.current;
      const floating = floatingRef.current;
      if (!trigger || !floating) return;
      const tokenSource = tokenSourceRef?.current;
      const triggerBox = trigger.getBoundingClientRect();
      const visualViewport = window.visualViewport;
      const viewportWidth = visualViewport?.width ?? window.innerWidth;
      if (tokenSource) copyOverlayTokens(tokenSource, floating, triggerBox.width);
      const tokenStyles = getComputedStyle(tokenSource ?? floating);
      const rootFontPx = Number.parseFloat(getComputedStyle(document.documentElement).fontSize) || 16;
      const boxWidth = overlayBoxWidth({
        triggerWidth: triggerBox.width,
        viewportWidth,
        minWidthToken: tokenStyles.getPropertyValue("--select-popover-min-width"),
        maxWidthToken: tokenStyles.getPropertyValue("--select-popover-max-width"),
        stretch: align === "stretch",
        lockMinWidthToTrigger,
        rootFontPx,
      });
      if (boxWidth.width != null) floating.style.width = `${boxWidth.width}px`;
      else floating.style.removeProperty("width");
      if (boxWidth.minWidth != null) floating.style.minWidth = `${boxWidth.minWidth}px`;
      else floating.style.removeProperty("min-width");
      floating.style.maxWidth = `${boxWidth.maxWidth}px`;
      const box = floating.getBoundingClientRect();
      const next = placeFloating({
        trigger: {
          top: triggerBox.top,
          left: triggerBox.left,
          width: triggerBox.width,
          height: triggerBox.height,
        },
        floating: { width: boxWidth.width ?? box.width, height: box.height },
        viewport: {
          width: viewportWidth,
          height: visualViewport?.height ?? window.innerHeight,
        },
        offset,
        preferredSide,
        align,
        size,
      });
      if (tokenSource) {
        // Side marker only. CSS must not punch trigger or overlay bezels from this class.
        tokenSource.classList.toggle("is-overlay-above", next.side === "top");
      }
      setPlaced({ ...next, minWidth: boxWidth.minWidth, maxWidth: boxWidth.maxWidth, width: boxWidth.width ?? next.width });
    };
    locate();
    window.addEventListener("resize", locate);
    window.visualViewport?.addEventListener("resize", locate);
    return () => {
      window.removeEventListener("resize", locate);
      window.visualViewport?.removeEventListener("resize", locate);
      tokenSourceRef?.current?.classList.remove("is-overlay-above");
    };
  }, [align, lockMinWidthToTrigger, offset, open, preferredSide, size, tokenSourceRef, triggerRef, floatingRef]);

  const style: CSSProperties = open && placed
    ? {
        top: placed.top,
        left: placed.left,
        width: placed.width,
        minWidth: placed.minWidth,
        maxWidth: placed.maxWidth,
        maxHeight: placed.maxHeight,
        ["--tip-connector" as string]: `${placed.connector}px`,
      }
    : { visibility: "hidden", top: 0, left: 0 };

  return { style, side: placed?.side ?? preferredSide, connector: placed?.connector ?? 0 };
}

export function AnchoredOverlay({
  open,
  triggerRef,
  tokenSourceRef,
  floatingRef,
  preferredSide = "bottom",
  align = "start",
  offset = OVERLAY_PLATE_OFFSET,
  size = true,
  lockMinWidthToTrigger = true,
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
  lockMinWidthToTrigger?: boolean;
  children: (bind: {
    ref: (node: HTMLElement | null) => void;
    style: CSSProperties;
    overlayClassName: string;
  }) => ReactNode;
}) {
  const localRef = useRef<HTMLElement>(null);
  const [portalRoot, setPortalRoot] = useState(() => overlayPortalRoot(null));

  useLayoutEffect(() => {
    if (!open) return;
    const next = overlayPortalRoot(triggerRef.current ?? tokenSourceRef.current);
    setPortalRoot((current) => (current === next ? current : next));
  }, [open, tokenSourceRef, triggerRef]);

  const { style, side } = useFloatingPlacement({
    open,
    triggerRef,
    floatingRef: floatingRef ?? localRef,
    tokenSourceRef,
    preferredSide,
    align,
    offset,
    size,
    lockMinWidthToTrigger,
  });

  if (!open) return null;

  /* eslint-disable react-hooks/refs -- render-prop bind hands refs to caller-owned overlay nodes */
  return createPortal(
    children({
      ref: mergeOverlayRefs(floatingRef, localRef),
      style,
      overlayClassName: `floating-overlay${side === "top" ? " floating-overlay--above" : ""}`,
    }),
    portalRoot,
  );
  /* eslint-enable react-hooks/refs */
}
