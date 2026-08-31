import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { gallerySectionItems } from "./gallerySections";
import { useGalleryScrollSpy } from "./useGalleryScrollSpy";

function mockSection(id: string, top: number) {
  const el = document.createElement("section");
  el.id = id;
  el.getBoundingClientRect = () => ({
    top,
    left: 0,
    right: 0,
    bottom: 0,
    width: 0,
    height: 100,
    x: 0,
    y: top,
    toJSON: () => ({}),
  });
  document.body.appendChild(el);
  return el;
}

describe("useGalleryScrollSpy", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
    const header = document.createElement("header");
    header.className = "page-strip";
    Object.defineProperty(header, "offsetHeight", { value: 48, configurable: true });
    document.body.appendChild(header);
    window.history.replaceState(null, "", "#colors");
    for (const item of gallerySectionItems) {
      mockSection(item.id, 2000);
    }
    vi.stubGlobal("scrollTo", vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("keeps the navigated section active until scroll settles at the spy line", async () => {
    const typography = document.getElementById("typography")!;
    typography.getBoundingClientRect = () => ({
      top: 800,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 100,
      x: 0,
      y: 800,
      toJSON: () => ({}),
    });
    document.getElementById("keys")!.getBoundingClientRect = () => ({
      top: 50,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 100,
      x: 0,
      y: 50,
      toJSON: () => ({}),
    });

    const { result } = renderHook(() => useGalleryScrollSpy());

    act(() => {
      result.current.navigate("typography");
    });
    expect(result.current.activeId).toBe("typography");
    expect(window.location.hash).toBe("#typography");

    act(() => {
      window.dispatchEvent(new Event("scroll"));
    });
    expect(result.current.activeId).toBe("typography");

    typography.getBoundingClientRect = () => ({
      top: 66,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 100,
      x: 0,
      y: 66,
      toJSON: () => ({}),
    });
    document.getElementById("keys")!.getBoundingClientRect = () => ({
      top: 200,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 100,
      x: 0,
      y: 200,
      toJSON: () => ({}),
    });

    await act(async () => {
      window.dispatchEvent(new Event("scroll"));
      await new Promise((resolve) => setTimeout(resolve, 180));
    });

    expect(result.current.activeId).toBe("typography");
    expect(window.location.hash).toBe("#typography");
  });

  it("does not release the lock to an earlier section while the target is still above the spy line", async () => {
    const typography = document.getElementById("typography")!;
    typography.getBoundingClientRect = () => ({
      top: 67,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 100,
      x: 0,
      y: 67,
      toJSON: () => ({}),
    });
    document.getElementById("type")!.getBoundingClientRect = () => ({
      top: -350,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 100,
      x: 0,
      y: -350,
      toJSON: () => ({}),
    });

    const { result } = renderHook(() => useGalleryScrollSpy());

    act(() => {
      result.current.navigate("typography");
    });

    await act(async () => {
      window.dispatchEvent(new Event("scroll"));
      await new Promise((resolve) => setTimeout(resolve, 180));
    });

    expect(result.current.activeId).toBe("typography");
    expect(window.location.hash).toBe("#typography");
  });

  it("aliases a retired composition-recipes hash to form-recipes on load", () => {
    window.history.replaceState(null, "", "#composition-recipes");
    const formRecipes = document.getElementById("form-recipes")!;
    formRecipes.getBoundingClientRect = () => ({
      top: 40,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 100,
      x: 0,
      y: 40,
      toJSON: () => ({}),
    });

    const { result } = renderHook(() => useGalleryScrollSpy());

    expect(result.current.activeId).toBe("form-recipes");
    expect(window.location.hash).toBe("#form-recipes");
  });

  it("rewrites composition-recipes to form-recipes on hashchange", () => {
    const { result } = renderHook(() => useGalleryScrollSpy());

    act(() => {
      window.history.replaceState(null, "", "#composition-recipes");
      window.dispatchEvent(new HashChangeEvent("hashchange"));
    });

    expect(result.current.activeId).toBe("form-recipes");
    expect(window.location.hash).toBe("#form-recipes");
  });

  it("scrolls to the hash target on hashchange and keeps the hash during interim scroll", async () => {
    const layoutManagement = document.getElementById("layout-management")!;
    layoutManagement.getBoundingClientRect = () => ({
      top: 800,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 100,
      x: 0,
      y: 800,
      toJSON: () => ({}),
    });
    document.getElementById("keys")!.getBoundingClientRect = () => ({
      top: 50,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 100,
      x: 0,
      y: 50,
      toJSON: () => ({}),
    });

    const { result } = renderHook(() => useGalleryScrollSpy());

    await act(async () => {
      window.history.replaceState(null, "", "#layout-management");
      window.dispatchEvent(new HashChangeEvent("hashchange"));
      await new Promise((resolve) => requestAnimationFrame(resolve));
    });

    expect(window.scrollTo).toHaveBeenCalled();
    expect(result.current.activeId).toBe("layout-management");
    expect(window.location.hash).toBe("#layout-management");

    act(() => {
      window.dispatchEvent(new Event("scroll"));
    });

    expect(result.current.activeId).toBe("layout-management");
    expect(window.location.hash).toBe("#layout-management");
  });

  it("re-anchors the hash target after viewport resize", async () => {
    const layoutManagement = document.getElementById("layout-management")!;
    layoutManagement.getBoundingClientRect = () => ({
      top: 66,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 100,
      x: 0,
      y: 66,
      toJSON: () => ({}),
    });

    window.history.replaceState(null, "", "#layout-management");
    const { result } = renderHook(() => useGalleryScrollSpy());

    await act(async () => {
      await new Promise((resolve) => requestAnimationFrame(resolve));
    });

    layoutManagement.getBoundingClientRect = () => ({
      top: 900,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 100,
      x: 0,
      y: 900,
      toJSON: () => ({}),
    });
    document.getElementById("keys")!.getBoundingClientRect = () => ({
      top: 50,
      left: 0,
      right: 0,
      bottom: 0,
      width: 0,
      height: 100,
      x: 0,
      y: 50,
      toJSON: () => ({}),
    });

    await act(async () => {
      window.dispatchEvent(new Event("resize"));
      await new Promise((resolve) => requestAnimationFrame(resolve));
      window.dispatchEvent(new Event("scroll"));
    });

    expect(result.current.activeId).toBe("layout-management");
    expect(window.location.hash).toBe("#layout-management");
    expect(window.scrollTo).toHaveBeenCalled();
  });
});
