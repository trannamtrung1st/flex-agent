import { placeFloating } from "./placeFloating";

const viewport = { width: 1000, height: 800 };

describe("placeFloating", () => {
  it("seats a centered plaque above the trigger with the requested gap", () => {
    const placed = placeFloating({
      trigger: { top: 200, left: 400, width: 80, height: 24 },
      floating: { width: 200, height: 32 },
      viewport,
      offset: 10,
      preferredSide: "top",
      align: "center",
    });
    expect(placed.side).toBe("top");
    expect(placed.top).toBe(200 - 10 - 32);
    expect(placed.left).toBe(400 + 40 - 100);
    expect(placed.connector).toBe(100);
  });

  it("shifts a centered plaque to the viewport edge with no inset", () => {
    const placed = placeFloating({
      trigger: { top: 200, left: 900, width: 80, height: 24 },
      floating: { width: 240, height: 32 },
      viewport,
      offset: 10,
      preferredSide: "top",
      align: "center",
    });
    expect(placed.left).toBe(760);
    expect(placed.left + 240).toBe(1000);
    expect(placed.connector).toBe(900 + 40 - placed.left);
  });

  it("flips below when the preferred top side does not fit", () => {
    const placed = placeFloating({
      trigger: { top: 20, left: 400, width: 80, height: 24 },
      floating: { width: 120, height: 40 },
      viewport,
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
      offset: 0,
      preferredSide: "bottom",
      align: "end",
      size: true,
    });
    expect(placed.side).toBe("top");
    expect(placed.top).toBe(740 - 200);
  });

  it("pins flush to the viewport bottom when neither side fits, covering the trigger", () => {
    const trigger = { top: 380, left: 50, width: 240, height: 40 };
    const placed = placeFloating({
      trigger,
      floating: { width: 240, height: 500 },
      viewport,
      offset: 0,
      preferredSide: "bottom",
      align: "stretch",
      size: true,
    });
    expect(placed.width).toBe(240);
    expect(placed.left).toBe(50);
    expect(placed.side).toBe("bottom");
    expect(placed.maxHeight).toBeUndefined();
    expect(placed.top).toBe(300);
    expect(placed.top + 500).toBe(800);
    expect(placed.top).toBeLessThan(trigger.top + trigger.height);
  });

  it("caps height to the viewport when the panel is taller than the viewport", () => {
    const placed = placeFloating({
      trigger: { top: 380, left: 50, width: 240, height: 40 },
      floating: { width: 240, height: 900 },
      viewport,
      offset: 0,
      preferredSide: "bottom",
      align: "stretch",
      size: true,
    });
    expect(placed.maxHeight).toBe(800);
    expect(placed.top).toBe(0);
    expect(placed.top + (placed.maxHeight ?? 0)).toBe(800);
  });

  it("pins the same way without a maxHeight when size is false", () => {
    const trigger = { top: 380, left: 50, width: 240, height: 40 };
    const placed = placeFloating({
      trigger,
      floating: { width: 240, height: 500 },
      viewport,
      offset: 0,
      preferredSide: "bottom",
      align: "stretch",
    });
    expect(placed.side).toBe("bottom");
    expect(placed.maxHeight).toBeUndefined();
    expect(placed.top).toBe(300);
    expect(placed.top).toBeLessThan(trigger.top + trigger.height);
  });

  it("lets a stretched panel keep a min-width larger than the hug trigger", () => {
    const placed = placeFloating({
      trigger: { top: 40, left: 50, width: 80, height: 32 },
      floating: { width: 256, height: 200 },
      viewport,
      offset: 0,
      preferredSide: "bottom",
      align: "stretch",
    });
    expect(placed.width).toBe(256);
    expect(placed.left).toBe(50);
  });

  it("narrows a stretched panel that is wider than the viewport to the viewport width", () => {
    const placed = placeFloating({
      trigger: { top: 40, left: 200, width: 1200, height: 32 },
      floating: { width: 1200, height: 80 },
      viewport: { width: 1000, height: 800 },
      offset: 0,
      preferredSide: "bottom",
      align: "stretch",
    });
    expect(placed.width).toBe(1000);
    expect(placed.left).toBe(0);
    expect(placed.left + (placed.width ?? 0)).toBe(1000);
  });

  it("overlaps the trigger-adjacent edge by 1px on the open axis only", () => {
    const trigger = { top: 200, left: 80, width: 120, height: 32 };
    const floating = { width: 180, height: 80 };
    const below = placeFloating({
      trigger,
      floating,
      viewport,
      offset: -1,
      preferredSide: "bottom",
      align: "start",
    });
    expect(below.side).toBe("bottom");
    expect(below.top).toBe(200 + 32 - 1);
    expect(below.left).toBe(80);

    const above = placeFloating({
      trigger,
      floating,
      viewport,
      offset: -1,
      preferredSide: "top",
      align: "end",
    });
    expect(above.side).toBe("top");
    expect(above.top).toBe(200 + 1 - 80);
    expect(above.left).toBe(80 + 120 - 180);
  });

  it("clamps an end-aligned menu to the left viewport edge", () => {
    const placed = placeFloating({
      trigger: { top: 80, left: 10, width: 40, height: 24 },
      floating: { width: 200, height: 80 },
      viewport,
      offset: 0,
      preferredSide: "bottom",
      align: "end",
    });
    expect(placed.left).toBe(0);
  });
});
