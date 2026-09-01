import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { gallerySections, resolveGallerySectionHash } from "./gallerySections";

const sectionIds = gallerySections.flatMap((group) => group.items.map((item) => item.id));

const SCROLL_SETTLE_MS = 150;
const NAV_LOCK_MS = 10_000;

type HashLock = { id: string; until: number };

function sectionAtSpyLine(id: string, offset: number) {
  const section = document.getElementById(id);
  return section ? section.getBoundingClientRect().top <= offset : false;
}

function scrollBehavior(): ScrollBehavior {
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth";
}

function scrollToGallerySection(id: string, headerOffset: () => number, behavior?: ScrollBehavior) {
  const section = document.getElementById(id);
  if (!section) return;
  window.scrollTo({
    top: Math.max(0, window.scrollY + section.getBoundingClientRect().top - headerOffset() + 1),
    behavior: behavior ?? scrollBehavior(),
  });
}

export function useGalleryScrollSpy() {
  const initial = useMemo(() => {
    const resolved = resolveGallerySectionHash(window.location.hash);
    return resolved ?? sectionIds[0];
  }, []);
  const [activeId, setActiveId] = useState(initial);
  const hashLock = useRef<HashLock | null>(
    initial !== sectionIds[0] ? { id: initial, until: 8.64e15 } : null,
  );

  const headerOffset = useCallback(() => {
    const header = document.querySelector<HTMLElement>("header.page-strip");
    return (header?.offsetHeight ?? 48) + 18;
  }, []);

  const lockHash = useCallback((id: string) => {
    hashLock.current = { id, until: Date.now() + NAV_LOCK_MS };
    setActiveId(id);
  }, []);

  useEffect(() => {
    const rawHash = window.location.hash.slice(1);
    const resolved = resolveGallerySectionHash(window.location.hash);
    if (resolved && rawHash !== resolved) {
      window.history.replaceState(window.history.state, "", `#${resolved}`);
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
        if (atTarget) {
          hashLock.current = null;
          if (window.location.hash !== `#${id}`) {
            window.history.replaceState(window.history.state, "", `#${id}`);
          }
          return;
        }
        if (Date.now() < until) return;
        hashLock.current = null;
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
      const rawHash = window.location.hash.slice(1);
      const resolved = resolveGallerySectionHash(window.location.hash);
      if (!resolved) return;
      if (rawHash !== resolved) {
        window.history.replaceState(window.history.state, "", `#${resolved}`);
      }
      lockHash(resolved);
      requestAnimationFrame(() => {
        scrollToGallerySection(resolved, headerOffset);
      });
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
    const onResize = () => {
      syncHeader();
      const resolved = resolveGallerySectionHash(window.location.hash);
      if (resolved) {
        lockHash(resolved);
        requestAnimationFrame(() => {
          scrollToGallerySection(resolved, headerOffset, "auto");
        });
      }
      schedule();
    };
    window.addEventListener("resize", onResize);
    window.addEventListener("scroll", schedule, { passive: true });
    window.addEventListener("hashchange", onHashChange);
    const initialTarget = document.getElementById(initial);
    if (initialTarget && initial !== sectionIds[0]) {
      requestAnimationFrame(() => {
        scrollToGallerySection(initial, headerOffset, "auto");
      });
    }
    return () => {
      cancelAnimationFrame(frame);
      window.clearTimeout(scrollReleaseTimer);
      window.removeEventListener("resize", onResize);
      window.removeEventListener("scroll", schedule);
      window.removeEventListener("hashchange", onHashChange);
    };
  }, [headerOffset, initial, lockHash]);

  const navigate = useCallback((id: string) => {
    const section = document.getElementById(id);
    if (!section) return;
    lockHash(id);
    window.history.pushState(window.history.state, "", `#${id}`);
    scrollToGallerySection(id, headerOffset);
  }, [headerOffset, lockHash]);

  return { activeId, navigate };
}
