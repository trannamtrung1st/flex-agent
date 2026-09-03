import { useLayoutEffect, useState, type RefObject } from "react";

export type TruncationAxis = "inline" | "block";

export function isTruncated(el: HTMLElement, axis: TruncationAxis = "inline") {
  // Horizontal ellipsis only by default. Glyph overflow past line-height:1 is not
  // clipping, and a 1px width delta is subpixel rounding rather than a truncated caption.
  const inlineOverflow = el.scrollWidth - el.clientWidth > 1;
  if (axis === "inline") {
    return inlineOverflow;
  }
  // Line-clamp clips vertically; ignore a 2px glyph/line-box delta.
  return inlineOverflow || el.scrollHeight - el.clientHeight > 2;
}

export function useTruncated(
  ref: RefObject<HTMLElement | null>,
  enabled = true,
  axis: TruncationAxis = "inline",
) {
  const [truncated, setTruncated] = useState(false);

  useLayoutEffect(() => {
    if (!enabled) {
      return;
    }
    const el = ref.current;
    if (!el) return;

    const check = () => setTruncated(isTruncated(el, axis));

    check();
    const observer = new ResizeObserver(check);
    observer.observe(el);
    return () => observer.disconnect();
  }, [axis, enabled, ref]);

  return enabled ? truncated : false;
}
