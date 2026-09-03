import { useEffect, useRef, useState, useSyncExternalStore } from "react";
import type { SessionSnapshotTranscriptItemV1 } from "../../contracts/v1";

export function transcriptItemCopy(item: SessionSnapshotTranscriptItemV1): string {
  if (item.status === "unavailable" && !item.content) {
    return "Content unavailable.";
  }
  return item.content ?? "";
}

function copies(items: SessionSnapshotTranscriptItemV1[]): Record<string, string> {
  return Object.fromEntries(items.map((item) => [item.item_id, transcriptItemCopy(item)]));
}

function subscribeReducedMotion(onChange: () => void) {
  const media = window.matchMedia("(prefers-reduced-motion: reduce)");
  media.addEventListener("change", onChange);
  return () => media.removeEventListener("change", onChange);
}

export function useTranscriptReveal(items: SessionSnapshotTranscriptItemV1[], ready = true) {
  const [revealed, setRevealed] = useState<Record<string, string>>({});
  const seeded = useRef(false);
  const signature = items.map((item) => `${item.item_id}:${item.status}:${item.content ?? ""}`).join("|");
  const reduceMotion = useSyncExternalStore(
    subscribeReducedMotion,
    () => window.matchMedia("(prefers-reduced-motion: reduce)").matches,
    () => false,
  );

  useEffect(() => {
    if (!ready) {
      seeded.current = false;
      return;
    }

    if (!seeded.current) {
      seeded.current = true;
      setRevealed(copies(items));
      if (reduceMotion) {
        return;
      }
    }

    if (reduceMotion) {
      return;
    }

    const id = window.setInterval(() => {
      setRevealed((previous) => {
        let changed = false;
        const next = { ...previous };
        for (const item of items) {
          const target = transcriptItemCopy(item);
          const current = next[item.item_id] ?? "";
          if (item.author !== "agent") {
            if (current !== target) {
              next[item.item_id] = target;
              changed = true;
            }
            continue;
          }
          if (current === target) {
            continue;
          }
          if (!target.startsWith(current)) {
            next[item.item_id] = target;
            changed = true;
            continue;
          }
          next[item.item_id] = target.slice(0, Math.min(target.length, current.length + 2));
          changed = true;
        }
        return changed ? next : previous;
      });
    }, 24);

    return () => window.clearInterval(id);
  }, [items, ready, reduceMotion, signature]);

  if (!ready || reduceMotion || Object.keys(revealed).length === 0) {
    return copies(items);
  }

  return revealed;
}
