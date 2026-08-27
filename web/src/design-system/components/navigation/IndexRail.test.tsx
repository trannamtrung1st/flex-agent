import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { IndexRail } from "./IndexRail";

const groups = [
  {
    id: "foundations",
    label: "Foundations",
    items: [
      { id: "color", label: "Color" },
      { id: "type", label: "Type voices" },
    ],
  },
  {
    id: "navigation",
    label: "Navigation",
    items: [{ id: "strip", label: "Command strip" }],
  },
] as const;

describe("IndexRail", () => {
  it("uses the shared gangway tick for the current index item", () => {
    render(<IndexRail groups={groups} activeId="color" />);

    const current = screen.getByRole("link", { name: "Color" });
    const idle = screen.getByRole("link", { name: "Type voices" });

    expect(current).toHaveClass("is-current");
    expect(current).toHaveAttribute("aria-current", "location");
    expect(current.querySelector(".gangway-tick")).not.toBeNull();
    expect(idle.querySelector(".gangway-tick")).not.toBeNull();
    expect(idle).not.toHaveClass("is-current");
  });

  it("scrolls the active link into view when the rail overflows", () => {
    const scrollIntoView = vi.spyOn(HTMLElement.prototype, "scrollIntoView");
    const { rerender } = render(<IndexRail groups={groups} activeId="color" />);
    const scrollport = screen.getByRole("navigation", { name: "Component index" }).querySelector<HTMLElement>(".nav-rail");
    expect(scrollport).not.toBeNull();

    Object.defineProperty(scrollport!, "scrollHeight", { configurable: true, value: 480 });
    Object.defineProperty(scrollport!, "clientHeight", { configurable: true, value: 240 });

    rerender(<IndexRail groups={groups} activeId="strip" />);

    const active = screen.getByRole("link", { name: "Command strip" });
    expect(active).toHaveAttribute("href", "#strip");
    expect(scrollIntoView).toHaveBeenCalledWith({ block: "nearest" });

    scrollIntoView.mockRestore();
  });

  it("does not scroll when the rail fits its contents", () => {
    const scrollIntoView = vi.spyOn(HTMLElement.prototype, "scrollIntoView");
    const { rerender } = render(<IndexRail groups={groups} activeId="color" />);
    const scrollport = screen.getByRole("navigation", { name: "Component index" }).querySelector<HTMLElement>(".nav-rail");
    expect(scrollport).not.toBeNull();

    Object.defineProperty(scrollport!, "scrollHeight", { configurable: true, value: 240 });
    Object.defineProperty(scrollport!, "clientHeight", { configurable: true, value: 240 });

    scrollIntoView.mockClear();
    rerender(<IndexRail groups={groups} activeId="strip" />);

    expect(scrollIntoView).not.toHaveBeenCalled();
    scrollIntoView.mockRestore();
  });
});
