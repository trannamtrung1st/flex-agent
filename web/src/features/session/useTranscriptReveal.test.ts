import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { SessionSnapshotTranscriptItemV1 } from "../../contracts/v1";
import { useTranscriptReveal } from "./useTranscriptReveal";

function agentItem(content: string): SessionSnapshotTranscriptItemV1 {
  return {
    item_id: "item.agent.1",
    author: "agent",
    status: "complete",
    sequence_start: "1",
    sequence_end: "1",
    content,
  };
}

describe("useTranscriptReveal", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.stubGlobal("matchMedia", (query: string) => ({
      matches: false,
      media: query,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
      onchange: null,
    }));
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it("keeps an already-loaded Agent message complete instead of replaying it", () => {
    const restored = [agentItem("Authoritative restored reply.")];
    const { result, rerender } = renderHook(
      ({ items }) => useTranscriptReveal(items, true),
      { initialProps: { items: restored } },
    );

    expect(result.current["item.agent.1"]).toBe("Authoritative restored reply.");

    act(() => {
      vi.advanceTimersByTime(48);
    });
    rerender({ items: restored });

    expect(result.current["item.agent.1"]).toBe("Authoritative restored reply.");
  });

  it("typewrites later Agent deltas after the restored transcript is seeded", () => {
    const restored = [agentItem("Hi")];
    const { result, rerender } = renderHook(
      ({ items }) => useTranscriptReveal(items, true),
      { initialProps: { items: restored } },
    );

    rerender({ items: [agentItem("Hi there")] });
    act(() => {
      vi.advanceTimersByTime(24);
    });

    expect(result.current["item.agent.1"]).toBe("Hi t");
  });
});
