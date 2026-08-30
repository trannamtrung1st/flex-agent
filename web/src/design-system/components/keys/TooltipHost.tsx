import { createPortal } from "react-dom";
import { useCallback, useEffect, useRef, useState, type ReactNode, type RefObject } from "react";
import { cx } from "../../../lib/cx";
import { overlayPortalRoot } from "../overlays/overlayPortalRoot";
import { useFloatingPlacement } from "../overlays/AnchoredOverlay";
import { useTruncated } from "./useTruncated";

export type TooltipTone = "label" | "value";

/** Linger before hide so the pointer can cross onto the plaque and select text. */
export const TOOLTIP_HIDE_DELAY_MS = 240;

let dismissOpenPlaque: (() => void) | null = null;
let openPlaqueOwner: object | null = null;

export function TooltipHost({
  tip,
  tipOnlyWhenTruncated,
  truncationRef,
  children,
  className,
  tone = "label",
}: {
  tip?: string;
  tipOnlyWhenTruncated?: boolean;
  truncationRef?: RefObject<HTMLElement | null>;
  children: ReactNode;
  className?: string;
  /** `value` preserves case and tracking so identifiers stay copy-readable. */
  tone?: TooltipTone;
}) {
  const hostRef = useRef<HTMLSpanElement>(null);
  const plaqueRef = useRef<HTMLSpanElement>(null);
  const hideTimerRef = useRef<number | null>(null);
  const selectingRef = useRef(false);
  const pointerInsideRef = useRef(false);
  const [open, setOpen] = useState(false);
  const truncated = useTruncated(truncationRef ?? { current: null }, Boolean(tipOnlyWhenTruncated && truncationRef));
  const effectiveTip = tipOnlyWhenTruncated ? (truncated ? tip : undefined) : tip;
  const watchTip = Boolean(tip);
  const owner = useRef({});
  const { style: plaqueStyle, side } = useFloatingPlacement({
    open,
    triggerRef: hostRef,
    floatingRef: plaqueRef,
    preferredSide: "top",
    align: "center",
    offset: 10,
    size: false,
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
    setOpen(false);
  }, [clearHideTimer]);

  const show = useCallback(() => {
    if (!effectiveTip) return;
    if (openPlaqueOwner !== owner.current && dismissOpenPlaque) {
      dismissOpenPlaque();
    }
    openPlaqueOwner = owner.current;
    dismissOpenPlaque = closePlaque;
    pointerInsideRef.current = true;
    clearHideTimer();
    setOpen(true);
  }, [clearHideTimer, closePlaque, effectiveTip]);

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
        onFocusCapture={showForFocusVisible}
        onBlurCapture={(e) => hideNow(e.relatedTarget)}
      >
        {children}
      </span>
      {open && effectiveTip
        ? createPortal(
            <span
              ref={plaqueRef}
              className={cx(
                "tip-plaque",
                side === "top" ? "tip-plaque--above" : "tip-plaque--below",
                tone === "value" && "tip-plaque--value",
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
            overlayPortalRoot(hostRef.current),
          )
        : null}
    </>
  );
}
