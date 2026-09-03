import { createPortal } from "react-dom";
import { useCallback, useEffect, useRef, useState, type ReactNode, type RefObject } from "react";
import { cx } from "../../../lib/cx";
import { overlayPortalRoot } from "../overlays/overlayPortalRoot";
import { useOverlayDismiss } from "../overlays/useOverlayDismiss";
import { useFloatingPlacement } from "../overlays/AnchoredOverlay";
import { useTruncated, type TruncationAxis } from "./useTruncated";

export type TooltipTone = "label" | "value";

/** Linger before hide so the pointer can cross onto the plaque and select text. */
export const TOOLTIP_HIDE_DELAY_MS = 240;

let dismissOpenPlaque: (() => void) | null = null;
let openPlaqueOwner: object | null = null;

export function TooltipHost({
  tip,
  tipOnlyWhenTruncated,
  truncationRef,
  placementRef,
  children,
  className,
  tone = "label",
  openOnPress = false,
  truncationAxis = "inline",
  wrap = false,
}: {
  tip?: string;
  tipOnlyWhenTruncated?: boolean;
  truncationRef?: RefObject<HTMLElement | null>;
  /** Optical trigger for `placeFloating`. Hover, press, and dismiss still use the host. */
  placementRef?: RefObject<HTMLElement | null>;
  children: ReactNode;
  className?: string;
  /** `value` preserves case and tracking so identifiers stay copy-readable. */
  tone?: TooltipTone;
  /** Opens on primary press as well as hover (table CompactId; no extra tab stop). */
  openOnPress?: boolean;
  /** `block` also treats line-clamp overflow as truncated. */
  truncationAxis?: TruncationAxis;
  /** Allow the plaque to wrap long sentences instead of a single identifier line. */
  wrap?: boolean;
}) {
  const hostRef = useRef<HTMLSpanElement>(null);
  const plaqueRef = useRef<HTMLSpanElement>(null);
  const hideTimerRef = useRef<number | null>(null);
  const selectingRef = useRef(false);
  const pointerInsideRef = useRef(false);
  const [open, setOpen] = useState(false);
  const [portalRoot, setPortalRoot] = useState<HTMLElement | null>(null);
  const truncated = useTruncated(
    truncationRef ?? { current: null },
    Boolean(tipOnlyWhenTruncated && truncationRef),
    truncationAxis,
  );
  const effectiveTip = tipOnlyWhenTruncated ? (truncated ? tip : undefined) : tip;
  const watchTip = Boolean(tip);
  const owner = useRef({});
  const { style: plaqueStyle, side } = useFloatingPlacement({
    open,
    triggerRef: placementRef ?? hostRef,
    floatingRef: plaqueRef,
    preferredSide: "top",
    align: "center",
    offset: 10,
    size: false,
    lockMinWidthToTrigger: false,
  });

  const clearHideTimer = useCallback(() => {
    if (hideTimerRef.current != null) {
      window.clearTimeout(hideTimerRef.current);
      hideTimerRef.current = null;
    }
  }, []);

  const closePlaque = useCallback(() => {
    selectingRef.current = false;
    pointerInsideRef.current = false;
    clearHideTimer();
    if (openPlaqueOwner === owner.current) {
      openPlaqueOwner = null;
      dismissOpenPlaque = null;
    }
    setPortalRoot(null);
    setOpen(false);
  }, [clearHideTimer]);

  useOverlayDismiss(open, [hostRef, plaqueRef], closePlaque, { pointer: false, focus: false, scroll: true });

  const show = useCallback(() => {
    if (!effectiveTip) return;
    if (openPlaqueOwner !== owner.current && dismissOpenPlaque) {
      dismissOpenPlaque();
    }
    openPlaqueOwner = owner.current;
    dismissOpenPlaque = closePlaque;
    pointerInsideRef.current = true;
    clearHideTimer();
    setPortalRoot(overlayPortalRoot((placementRef ?? hostRef).current));
    setOpen(true);
  }, [clearHideTimer, closePlaque, effectiveTip, hostRef, placementRef]);

  const hideNow = useCallback((related?: EventTarget | null) => {
    if (selectingRef.current) return;
    if (related instanceof Node) {
      if (hostRef.current?.contains(related) || plaqueRef.current?.contains(related)) return;
    }
    closePlaque();
  }, [closePlaque]);

  const scheduleHide = useCallback(() => {
    pointerInsideRef.current = false;
    if (selectingRef.current) return;
    clearHideTimer();
    hideTimerRef.current = window.setTimeout(() => {
      hideTimerRef.current = null;
      if (selectingRef.current || pointerInsideRef.current) return;
      closePlaque();
    }, TOOLTIP_HIDE_DELAY_MS);
  }, [clearHideTimer, closePlaque]);

  useEffect(
    () => () => {
      if (openPlaqueOwner === owner.current) {
        openPlaqueOwner = null;
        dismissOpenPlaque = null;
      }
      clearHideTimer();
    },
    [clearHideTimer],
  );

  useEffect(() => {
    if (!open) return;
    const onPointerUp = () => {
      selectingRef.current = false;
      if (!pointerInsideRef.current) scheduleHide();
    };
    window.addEventListener("pointerup", onPointerUp);
    return () => window.removeEventListener("pointerup", onPointerUp);
  }, [open, scheduleHide]);

  useEffect(() => {
    if (effectiveTip) return;
    const frame = window.requestAnimationFrame(() => {
      closePlaque();
    });
    return () => window.cancelAnimationFrame(frame);
  }, [closePlaque, effectiveTip]);

  if (!watchTip) {
    return <span className={cx("tip-host", className)}>{children}</span>;
  }

  const showForFocusVisible = () => {
    requestAnimationFrame(() => {
      const host = hostRef.current;
      const active = document.activeElement;
      if (
        host
        && active instanceof HTMLElement
        && host.contains(active)
        && active.matches(":focus-visible")
      ) {
        show();
      }
    });
  };

  return (
    <>
      <span
        ref={hostRef}
        className={cx("tip-host", className)}
        onMouseEnter={show}
        onMouseLeave={scheduleHide}
        onPointerDown={(event) => {
          if (!openOnPress) return;
          if (event.pointerType === "mouse" && event.button !== 0) return;
          show();
        }}
        onFocusCapture={showForFocusVisible}
        onBlurCapture={(e) => hideNow(e.relatedTarget)}
      >
        {children}
      </span>
      {open && effectiveTip && portalRoot
        ? createPortal(
            <span
              ref={plaqueRef}
              className={cx(
                "tip-plaque",
                side === "top" ? "tip-plaque--above" : "tip-plaque--below",
                tone === "value" && "tip-plaque--value",
                wrap && "tip-plaque--wrap",
              )}
              style={plaqueStyle}
              role="tooltip"
              onMouseEnter={show}
              onMouseLeave={scheduleHide}
              onPointerDown={() => {
                selectingRef.current = true;
              }}
            >
              {effectiveTip}
            </span>,
            portalRoot,
          )
        : null}
    </>
  );
}
