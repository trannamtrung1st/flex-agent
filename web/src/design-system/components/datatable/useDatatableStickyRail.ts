import { useLayoutEffect, type RefObject } from "react";

const STICKY_RAIL_VAR = "--datatable-sticky-rail";

export function useDatatableStickyRail(scrollRef: RefObject<HTMLElement | null>) {
  useLayoutEffect(() => {
    const scroll = scrollRef.current;
    if (!scroll) return;

    const sync = () => {
      const thead = scroll.querySelector("thead");
      if (!thead) return;
      const height = thead.getBoundingClientRect().height;
      if (height < 1) return;
      scroll.style.setProperty(STICKY_RAIL_VAR, `${height}px`);
    };

    sync();
    if (typeof ResizeObserver === "undefined") return;

    const observer = new ResizeObserver(sync);
    const thead = scroll.querySelector("thead");
    if (thead) observer.observe(thead);
    observer.observe(scroll);
    return () => observer.disconnect();
  });
}
