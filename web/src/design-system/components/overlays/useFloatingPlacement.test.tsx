import { act, renderHook } from "@testing-library/react";
import { createRef } from "react";
import { useFloatingPlacement } from "./AnchoredOverlay";

function stubRect(el: HTMLElement, rect: { top: number; left: number; width: number; height: number }) {
  el.getBoundingClientRect = () => ({
    ...rect,
    right: rect.left + rect.width,
    bottom: rect.top + rect.height,
    x: rect.left,
    y: rect.top,
    toJSON() {
      return this;
    },
  });
}

describe("useFloatingPlacement", () => {
  beforeEach(() => {
    Object.defineProperty(window, "innerWidth", { configurable: true, value: 1000 });
    Object.defineProperty(window, "innerHeight", { configurable: true, value: 800 });
  });

  it("keeps the open placement on scroll instead of following the trigger", () => {
    const trigger = document.createElement("button");
    const floating = document.createElement("div");
    document.body.append(trigger, floating);
    stubRect(trigger, { top: 80, left: 40, width: 100, height: 32 });
    stubRect(floating, { top: 0, left: 0, width: 160, height: 200 });

    const triggerRef = createRef<HTMLElement>();
    const floatingRef = createRef<HTMLElement>();
    triggerRef.current = trigger;
    floatingRef.current = floating;

    const { result } = renderHook(() =>
      useFloatingPlacement({
        open: true,
        triggerRef,
        floatingRef,
        preferredSide: "bottom",
        align: "start",
        size: true,
      }),
    );

    expect(result.current.side).toBe("bottom");
    expect(result.current.style.top).toBe(112);

    stubRect(trigger, { top: 760, left: 40, width: 100, height: 32 });
    act(() => {
      window.dispatchEvent(new Event("scroll"));
    });

    expect(result.current.side).toBe("bottom");
    expect(result.current.style.top).toBe(112);
    trigger.remove();
    floating.remove();
  });

  it("keeps a stretched hug select at the rem popover floor", () => {
    const trigger = document.createElement("button");
    const floating = document.createElement("div");
    const source = document.createElement("div");
    source.style.setProperty("--select-popover-min-width", "max(100%, 16rem)");
    source.style.setProperty("--select-popover-max-width", "min(28rem, 54vw)");
    document.body.append(trigger, floating, source);
    stubRect(trigger, { top: 80, left: 40, width: 80, height: 32 });
    stubRect(floating, { top: 0, left: 0, width: 80, height: 200 });

    const triggerRef = createRef<HTMLElement>();
    const floatingRef = createRef<HTMLElement>();
    const tokenSourceRef = createRef<HTMLElement>();
    triggerRef.current = trigger;
    floatingRef.current = floating;
    tokenSourceRef.current = source;

    const { result } = renderHook(() =>
      useFloatingPlacement({
        open: true,
        triggerRef,
        floatingRef,
        tokenSourceRef,
        preferredSide: "bottom",
        align: "stretch",
        size: true,
      }),
    );

    expect(result.current.style.width).toBe(256);
    expect(result.current.style.minWidth).toBe(256);
    trigger.remove();
    floating.remove();
    source.remove();
  });
});
