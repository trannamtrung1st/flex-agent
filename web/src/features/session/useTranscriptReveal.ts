import { useEffect, useRef, useState } from "react";
import type { SessionSnapshotTranscriptItemV1 } from "../../contracts/v1";

export function transcriptItemCopy(item: SessionSnapshotTranscriptItemV1): string {
  if (item.status === "unavailable" && !item.content) {
    return "Content unavailable.";
  }
  return item.content ?? "";
}

export function useTranscriptReveal(items: SessionSnapshotTranscriptItemV1[], ready = true) {
  const [revealed, setRevealed] = useState<Record<string, string>>({});
  const hydrated = useRef(false);
  const signature = items.map((item) => `${item.item_id}:${item.status}:${item.content ?? ""}`).join("|");

  useEffect(() => {
    if (!ready || hydrated.current) {
      return;
    }
    const initial: Record<string, string> = {};
    for (const item of items) {
      initial[item.item_id] = transcriptItemCopy(item);
    }
    setRevealed(initial);
    hydrated.current = true;
  }, [items, ready]);

  useEffect(() => {
    if (!ready || !hydrated.current) {
      return;
    }
    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (reduce) {
      setRevealed(Object.fromEntries(items.map((item) => [item.item_id, transcriptItemCopy(item)])));
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
  }, [items, ready, signature]);

  return revealed;
}
