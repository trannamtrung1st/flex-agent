import { MemoryRouter } from "react-router";
import { renderHook, act } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useDemoParam, useStateParam } from "../src/lib/useDemoParam";
import type { ReactNode } from "react";

function wrap(initial: string) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <MemoryRouter initialEntries={[initial]}>{children}</MemoryRouter>;
  };
}

describe("useDemoParam", () => {
  it("falls back when the query value is unknown", () => {
    const { result } = renderHook(() => useDemoParam(["populated", "empty"] as const, "populated"), {
      wrapper: wrap("/participant-home?demo=nope"),
    });
    expect(result.current[0]).toBe("populated");
  });

  it("writes a known demo value onto search params", () => {
    const { result } = renderHook(() => useDemoParam(["populated", "empty"] as const, "populated"), {
      wrapper: wrap("/participant-home"),
    });
    act(() => result.current[1]("empty"));
    expect(result.current[0]).toBe("empty");
  });
});

describe("useStateParam", () => {
  it("reads a known session state", () => {
    const { result } = renderHook(() => useStateParam(["live", "warned"] as const, null), {
      wrapper: wrap("/participant-session?state=warned"),
    });
    expect(result.current).toBe("warned");
  });
});
