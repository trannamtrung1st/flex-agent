import { placeFloating } from "./placeFloating";

const viewport = { width: 1000, height: 800 };

describe("placeFloating", () => {
  it("seats a centered plaque above the trigger with the requested gap", () => {
    const placed = placeFloating({
      trigger: { top: 200, left: 400, width: 80, height: 24 },
      floating: { width: 200, height: 32 },
      viewport,
      padding: 8,
      offset: 10,
      preferredSide: "top",
      align: "center",
    });
    expect(placed.side).toBe("top");
    expect(placed.top).toBe(200 - 10 - 32);
    expect(placed.left).toBe(400 + 40 - 100);
    expect(placed.connector).toBe(100);
  });

  it("shifts a centered plaque left so it stays in the viewport inset", () => {
    const placed = placeFloating({
      trigger: { top: 200, left: 900, width: 80, height: 24 },
      floating: { width: 240, height: 32 },
      viewport,
      padding: 8,
      offset: 10,
      preferredSide: "top",
      align: "center",
    });
    expect(placed.left).toBe(1000 - 8 - 240);
    expect(placed.left + 240).toBeLessThanOrEqual(992);
    expect(placed.connector).toBe(900 + 40 - placed.left);
  });

  it("flips below when the preferred top side does not fit", () => {
    const placed = placeFloating({
      trigger: { top: 20, left: 400, width: 80, height: 24 },
      floating: { width: 120, height: 40 },
      viewport,
      padding: 8,
      offset: 10,
      preferredSide: "top",
      align: "center",
    });
    expect(placed.side).toBe("bottom");
    expect(placed.top).toBe(20 + 24 + 10);
  });

  it("flips above when a bottom menu would overflow", () => {
    const placed = placeFloating({
      trigger: { top: 740, left: 100, width: 120, height: 32 },
      floating: { width: 180, height: 200 },
      viewport,
      padding: 8,
      offset: 0,
      preferredSide: "bottom",
      align: "end",
      size: true,
    });
    expect(placed.side).toBe("top");
    expect(placed.top).toBe(740 - 200);
  });

  it("matches trigger width when stretched and clamps height when neither side fits", () => {
    const placed = placeFloating({
      trigger: { top: 380, left: 50, width: 240, height: 40 },
      floating: { width: 240, height: 500 },
      viewport,
      padding: 8,
      offset: 0,
      preferredSide: "bottom",
      align: "stretch",
      size: true,
    });
    expect(placed.width).toBe(240);
    expect(placed.left).toBe(50);
    expect(placed.maxHeight).toBeLessThan(500);
    expect(placed.maxHeight).toBeGreaterThan(0);
    expect(placed.top).toBeGreaterThanOrEqual(8);
    expect(placed.top + (placed.maxHeight ?? 0)).toBeLessThanOrEqual(792);
  });

  it("narrows a stretched panel that is wider than the viewport", () => {
    const placed = placeFloating({
      trigger: { top: 40, left: 200, width: 1200, height: 32 },
      floating: { width: 1200, height: 80 },
      viewport: { width: 1000, height: 800 },
      padding: 8,
      offset: 0,
      preferredSide: "bottom",
      align: "stretch",
    });
    expect(placed.width).toBe(984);
    expect(placed.left).toBe(8);
    expect(placed.left + (placed.width ?? 0)).toBe(992);
  });

  it("clamps an end-aligned menu off the left edge", () => {
    const placed = placeFloating({
      trigger: { top: 80, left: 10, width: 40, height: 24 },
      floating: { width: 200, height: 80 },
      viewport,
      padding: 8,
      offset: 0,
      preferredSide: "bottom",
      align: "end",
    });
    expect(placed.left).toBe(8);
  });
});
