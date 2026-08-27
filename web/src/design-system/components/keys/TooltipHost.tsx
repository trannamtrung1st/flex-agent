import { createPortal } from "react-dom";
import { useCallback, useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { cx } from "../../../lib/cx";

export function TooltipHost({
  tip,
  children,
  className,
}: {
  tip?: string;
  children: ReactNode;
  className?: string;
}) {
  const hostRef = useRef<HTMLSpanElement>(null);
  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState({ top: 0, left: 0, above: true });

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

  if (!tip) {
    return className ? <span className={className}>{children}</span> : <>{children}</>;
  }

  const show = () => {
    place();
    setOpen(true);
  };
  const hide = (related?: EventTarget | null) => {
    if (related && hostRef.current?.contains(related as Node)) return;
    setOpen(false);
  };

  return (
    <>
      <span
        ref={hostRef}
        className={cx("tip-host", className)}
        onMouseEnter={show}
        onMouseLeave={() => hide()}
        onFocusCapture={show}
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
              {tip}
            </span>,
            document.body,
          )
        : null}
    </>
  );
}
