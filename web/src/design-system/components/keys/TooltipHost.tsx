import { createPortal } from "react-dom";
import { useCallback, useLayoutEffect, useRef, useState, type ReactNode, type RefObject } from "react";
import { cx } from "../../../lib/cx";
import { useTruncated } from "./useTruncated";

export function TooltipHost({
  tip,
  tipOnlyWhenTruncated,
  truncationRef,
  children,
  className,
}: {
  tip?: string;
  tipOnlyWhenTruncated?: boolean;
  truncationRef?: RefObject<HTMLElement | null>;
  children: ReactNode;
  className?: string;
}) {
  const hostRef = useRef<HTMLSpanElement>(null);
  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState({ top: 0, left: 0, above: true });
  const truncated = useTruncated(truncationRef ?? { current: null }, Boolean(tipOnlyWhenTruncated && truncationRef));
  const effectiveTip = tipOnlyWhenTruncated ? (truncated ? tip : undefined) : tip;

  const place = useCallback(() => {
    const el = hostRef.current;
    if (!el) return;
    const rect = el.getBoundingClientRect();
    const above = rect.top > 72;
    setPos({
      top: above ? rect.top - 10 : rect.bottom + 10,
      left: rect.left + rect.width / 2,
      above,
    });
  }, []);

  useLayoutEffect(() => {
    if (!open) return;
    place();
    const onReflow = () => place();
    window.addEventListener("scroll", onReflow, true);
    window.addEventListener("resize", onReflow);
    return () => {
      window.removeEventListener("scroll", onReflow, true);
      window.removeEventListener("resize", onReflow);
    };
  }, [open, place]);

  if (!effectiveTip) {
    return <span className={cx("tip-host", className)}>{children}</span>;
  }

  const show = () => {
    place();
    setOpen(true);
  };
  const hide = (related?: EventTarget | null) => {
    if (related && hostRef.current?.contains(related as Node)) return;
    setOpen(false);
  };

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
        onMouseLeave={() => hide()}
        onFocusCapture={showForFocusVisible}
        onBlurCapture={(e) => hide(e.relatedTarget)}
      >
        {children}
      </span>
      {open
        ? createPortal(
            <span
              className={cx("tip-plaque", pos.above ? "tip-plaque--above" : "tip-plaque--below")}
              style={{ top: pos.top, left: pos.left }}
              role="tooltip"
            >
              {effectiveTip}
            </span>,
            document.body,
          )
        : null}
    </>
  );
}
