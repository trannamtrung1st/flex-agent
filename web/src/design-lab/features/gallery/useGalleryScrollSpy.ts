import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { gallerySections } from "./gallerySections";

const sectionIds = gallerySections.flatMap((group) => group.items.map((item) => item.id));

const SCROLL_SETTLE_MS = 150;
const NAV_LOCK_MS = 10_000;
const HASH_LOCK_MS = 900;

type HashLock = { id: string; until: number };

function sectionAtSpyLine(id: string, offset: number) {
  const section = document.getElementById(id);
  return section ? section.getBoundingClientRect().top <= offset : false;
}

export function useGalleryScrollSpy() {
  const initial = useMemo(() => {
    const hash = window.location.hash.slice(1);
    return sectionIds.includes(hash as (typeof sectionIds)[number]) ? hash : sectionIds[0];
  }, []);
  const [activeId, setActiveId] = useState(initial);
  const hashLock = useRef<HashLock | null>(null);

  const headerOffset = useCallback(() => {
    const header = document.querySelector<HTMLElement>("header.page-strip");
    return (header?.offsetHeight ?? 48) + 18;
  }, []);

  useEffect(() => {
    if (initial !== sectionIds[0]) {
      hashLock.current = { id: initial, until: Date.now() + HASH_LOCK_MS };
    }
    const header = document.querySelector<HTMLElement>("header.page-strip");
    const syncHeader = () => {
      if (header) document.documentElement.style.setProperty("--gallery-header-h", `${header.offsetHeight}px`);
    };
    const markCurrent = () => {
      if (hashLock.current) {
        const { id, until } = hashLock.current;
        setActiveId(id);
        const offset = headerOffset();
        const atTarget = sectionAtSpyLine(id, offset);
        if (!atTarget && Date.now() < until) return;
        if (!atTarget && Date.now() >= until) hashLock.current = null;
        if (hashLock.current && atTarget) return;
      }
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
        hashLock.current = { id, until: Date.now() + HASH_LOCK_MS };
        setActiveId(id);
      }
    };
    let frame = 0;
    let scrollReleaseTimer = 0;
    const schedule = () => {
      cancelAnimationFrame(frame);
      frame = requestAnimationFrame(markCurrent);
      if (!hashLock.current) return;
      const lockedId = hashLock.current.id;
      window.clearTimeout(scrollReleaseTimer);
      scrollReleaseTimer = window.setTimeout(() => {
        const lock = hashLock.current;
        if (!lock || lock.id !== lockedId) return;
        const offset = headerOffset();
        const arrived = sectionAtSpyLine(lockedId, offset);
        if (arrived || Date.now() >= lock.until) {
          hashLock.current = null;
          if (arrived) {
            setActiveId(lockedId);
            if (window.location.hash !== `#${lockedId}`) {
              window.history.replaceState(window.history.state, "", `#${lockedId}`);
            }
            return;
          }
          markCurrent();
        }
      }, SCROLL_SETTLE_MS);
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
    hashLock.current = { id, until: Date.now() + NAV_LOCK_MS };
    setActiveId(id);
    window.history.pushState(window.history.state, "", `#${id}`);
    window.scrollTo({
      top: Math.max(0, window.scrollY + section.getBoundingClientRect().top - headerOffset() + 1),
      behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth",
    });
  }, [headerOffset]);

  return { activeId, navigate };
}
