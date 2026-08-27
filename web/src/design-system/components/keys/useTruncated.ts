import { useLayoutEffect, useState, type RefObject } from "react";

export function isTruncated(el: HTMLElement) {
  // Horizontal ellipsis only. Glyph overflow past line-height:1 is not clipping,
  // and a 1px width delta is subpixel rounding rather than a truncated caption.
  return el.scrollWidth - el.clientWidth > 1;
}

export function useTruncated(ref: RefObject<HTMLElement | null>, enabled = true) {
  const [truncated, setTruncated] = useState(false);

  useLayoutEffect(() => {
    if (!enabled) {
      return;
    }
    const el = ref.current;
    if (!el) return;

    const check = () => setTruncated(isTruncated(el));

    check();
    const observer = new ResizeObserver(check);
    observer.observe(el);
    return () => observer.disconnect();
  }, [enabled, ref]);

  return enabled ? truncated : false;
}
