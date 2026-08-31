import { overlayBoxWidth, overlayTokenToPx, rewriteOverlayPercent } from "./overlayWidthTokens";

describe("overlayWidthTokens", () => {
  const viewport = { percentBasePx: 80, viewportWidth: 1000 };

  it("rewrites percentages against the trigger box, not the viewport", () => {
    expect(rewriteOverlayPercent("100%", 80)).toBe("80px");
    expect(rewriteOverlayPercent("max(100%, 16rem)", 80)).toBe("max(80px, 16rem)");
    expect(rewriteOverlayPercent("calc(100% + 2px)", 80)).toBe("calc(80px + 2px)");
  });

  it("keeps the rem floor when a hug trigger is narrower than the authored min-width", () => {
    expect(overlayTokenToPx("max(100%, 16rem)", viewport)).toBe(256);
    expect(overlayTokenToPx("max(100%, 148px)", viewport)).toBe(148);
  });

  it("grows with a trigger that is already wider than the rem floor", () => {
    expect(overlayTokenToPx("max(100%, 16rem)", { percentBasePx: 400, viewportWidth: 1000 })).toBe(400);
    expect(overlayTokenToPx("100%", { percentBasePx: 400, viewportWidth: 1000 })).toBe(400);
  });

  it("resolves context and toolbar max-width caps", () => {
    expect(overlayTokenToPx("min(28rem, 54vw)", { percentBasePx: 80, viewportWidth: 1000 })).toBe(448);
    expect(overlayTokenToPx("min(24rem, calc(100vw - 24px))", { percentBasePx: 80, viewportWidth: 1000 })).toBe(384);
  });

  it("stretches a hug select to max(trigger, token floor) instead of the mark alone", () => {
    const box = overlayBoxWidth({
      triggerWidth: 80,
      viewportWidth: 1000,
      minWidthToken: "max(100%, 16rem)",
      maxWidthToken: "min(28rem, 54vw)",
      stretch: true,
      lockMinWidthToTrigger: true,
    });
    expect(box.width).toBe(256);
    expect(box.minWidth).toBe(256);
    expect(box.maxWidth).toBe(448);
  });

  it("does not let an authored max-width shrink a plate below the trigger", () => {
    const box = overlayBoxWidth({
      triggerWidth: 800,
      viewportWidth: 1280,
      minWidthToken: "100%",
      maxWidthToken: "min(28rem, 54vw)",
      stretch: false,
      lockMinWidthToTrigger: true,
    });
    expect(box.minWidth).toBe(800);
    expect(box.maxWidth).toBe(800);
    expect(box.width).toBeUndefined();
  });

  it("lets a plaque hug content when min-width is not locked to the trigger", () => {
    const box = overlayBoxWidth({
      triggerWidth: 200,
      viewportWidth: 1000,
      minWidthToken: "",
      maxWidthToken: "",
      stretch: false,
      lockMinWidthToTrigger: false,
    });
    expect(box.width).toBeUndefined();
    expect(box.minWidth).toBeUndefined();
    expect(box.maxWidth).toBe(1000);
  });

  it("keeps field stretch matched to the trigger", () => {
    const box = overlayBoxWidth({
      triggerWidth: 420,
      viewportWidth: 1280,
      minWidthToken: "100%",
      maxWidthToken: "100%",
      stretch: true,
      lockMinWidthToTrigger: true,
    });
    expect(box.width).toBe(420);
    expect(box.minWidth).toBe(420);
    expect(box.maxWidth).toBe(420);
  });

  it("gives toolbar hug marks the 16rem floor and room to grow", () => {
    const box = overlayBoxWidth({
      triggerWidth: 72,
      viewportWidth: 1280,
      minWidthToken: "max(100%, 16rem)",
      maxWidthToken: "min(24rem, calc(100vw - 24px))",
      stretch: false,
      lockMinWidthToTrigger: true,
    });
    expect(box.minWidth).toBe(256);
    expect(box.maxWidth).toBe(384);
  });

  it("gives foot listboxes the 148px floor without shrinking to the mark", () => {
    const box = overlayBoxWidth({
      triggerWidth: 64,
      viewportWidth: 1280,
      minWidthToken: "max(100%, 148px)",
      maxWidthToken: "none",
      stretch: false,
      lockMinWidthToTrigger: true,
    });
    expect(box.minWidth).toBe(148);
    expect(box.maxWidth).toBe(1280);
  });

  it("does not cap command menus to the trigger when max-width is none", () => {
    const box = overlayBoxWidth({
      triggerWidth: 36,
      viewportWidth: 1280,
      minWidthToken: "",
      maxWidthToken: "none",
      stretch: false,
      lockMinWidthToTrigger: true,
    });
    expect(box.minWidth).toBe(36);
    expect(box.maxWidth).toBe(1280);
  });
});
