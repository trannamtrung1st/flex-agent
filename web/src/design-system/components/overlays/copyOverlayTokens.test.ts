import { copyOverlayTokens } from "./AnchoredOverlay";

describe("copyOverlayTokens", () => {
  it("copies rem popover widths and skips percentage tokens without a trigger base", () => {
    const from = document.createElement("div");
    const to = document.createElement("div");
    from.style.setProperty("--select-popover-width", "17.5rem");
    from.style.setProperty("--select-popover-min-width", "100%");
    from.style.setProperty("--select-popover-max-width", "17.5rem");
    from.style.setProperty("--select-popover-max-height", "12rem");
    document.body.append(from, to);

    copyOverlayTokens(from, to);

    expect(to.style.getPropertyValue("--select-popover-width")).toBe("17.5rem");
    expect(to.style.getPropertyValue("--select-popover-min-width")).toBe("");
    expect(to.style.getPropertyValue("--select-popover-max-width")).toBe("17.5rem");
    expect(to.style.getPropertyValue("--select-popover-max-height")).toBe("12rem");
    from.remove();
    to.remove();
  });

  it("rewrites percentage min-width tokens against the trigger so rem floors survive the portal", () => {
    const from = document.createElement("div");
    const to = document.createElement("div");
    from.style.setProperty("--select-popover-min-width", "max(100%, 16rem)");
    document.body.append(from, to);

    copyOverlayTokens(from, to, 80);

    expect(to.style.getPropertyValue("--select-popover-min-width")).toBe("max(80px, 16rem)");
    from.remove();
    to.remove();
  });

  it("does not copy shell width tokens onto a datetime plate", () => {
    const from = document.createElement("div");
    const to = document.createElement("div");
    to.className = "datetime-popover datetime-popover--time";
    from.style.setProperty("--select-popover-width", "10.25rem");
    from.style.setProperty("--select-popover-max-height", "12rem");
    document.body.append(from, to);

    copyOverlayTokens(from, to);

    expect(to.style.getPropertyValue("--select-popover-width")).toBe("");
    expect(to.style.getPropertyValue("--select-popover-max-height")).toBe("12rem");
    from.remove();
    to.remove();
  });
});
