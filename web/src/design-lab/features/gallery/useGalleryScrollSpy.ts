import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { gallerySections } from "./gallerySections";

const sectionIds = gallerySections.flatMap((group) => group.items.map((item) => item.id));

export function useGalleryScrollSpy() {
  const initial = useMemo(() => {
    const hash = window.location.hash.slice(1);
    return sectionIds.includes(hash as (typeof sectionIds)[number]) ? hash : sectionIds[0];
  }, []);
  const [activeId, setActiveId] = useState(initial);
  const hashLock = useRef<{ id: string; until: number } | null>(null);

  const headerOffset = useCallback(() => {
    const header = document.querySelector<HTMLElement>("header.page-strip");
    return (header?.offsetHeight ?? 48) + 18;
  }, []);

  useEffect(() => {
    if (initial !== sectionIds[0]) {
      hashLock.current = { id: initial, until: Date.now() + 900 };
    }
    const header = document.querySelector<HTMLElement>("header.page-strip");
    const syncHeader = () => {
      if (header) document.documentElement.style.setProperty("--gallery-header-h", `${header.offsetHeight}px`);
    };
    const markCurrent = () => {
      if (hashLock.current && Date.now() < hashLock.current.until) {
        setActiveId(hashLock.current.id);
        return;
      }
      hashLock.current = null;
      let current = sectionIds[0];
      const offset = headerOffset();
      for (const id of sectionIds) {
        const section = document.getElementById(id);
        if (section && section.getBoundingClientRect().top <= offset) current = id;
      }
      setActiveId(current);
      if (window.location.hash !== `#${current}`) {
        window.history.replaceState(window.history.state, "", `#${current}`);
      }
    };
    const onHashChange = () => {
      const id = window.location.hash.slice(1);
      if (sectionIds.includes(id as (typeof sectionIds)[number])) {
        hashLock.current = { id, until: Date.now() + 900 };
        setActiveId(id);
      }
    };
    let frame = 0;
    let scrollReleaseTimer = 0;
    const schedule = () => {
      cancelAnimationFrame(frame);
      frame = requestAnimationFrame(markCurrent);
      window.clearTimeout(scrollReleaseTimer);
      if (hashLock.current) {
        scrollReleaseTimer = window.setTimeout(() => {
          hashLock.current = null;
          markCurrent();
        }, 160);
      }
    };

    syncHeader();
    window.addEventListener("resize", syncHeader);
    window.addEventListener("resize", schedule);
    window.addEventListener("scroll", schedule, { passive: true });
    window.addEventListener("hashchange", onHashChange);
    const initialTarget = document.getElementById(initial);
    if (initialTarget && initial !== sectionIds[0]) {
      requestAnimationFrame(() => {
        window.scrollTo({
          top: Math.max(0, window.scrollY + initialTarget.getBoundingClientRect().top - headerOffset()),
        });
      });
    }
    return () => {
      cancelAnimationFrame(frame);
      window.clearTimeout(scrollReleaseTimer);
      window.removeEventListener("resize", syncHeader);
      window.removeEventListener("resize", schedule);
      window.removeEventListener("scroll", schedule);
      window.removeEventListener("hashchange", onHashChange);
    };
  }, [headerOffset, initial]);

  const navigate = useCallback((id: string) => {
    const section = document.getElementById(id);
    if (!section) return;
    // Keep the requested item current for the whole smooth-scroll journey.
    // The scroll listener releases this lock after scrolling settles.
    hashLock.current = { id, until: Date.now() + 10_000 };
    setActiveId(id);
    window.history.pushState(window.history.state, "", `#${id}`);
    window.scrollTo({
      top: Math.max(0, window.scrollY + section.getBoundingClientRect().top - headerOffset()),
      behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth",
    });
  }, [headerOffset]);

  return { activeId, navigate };
}
