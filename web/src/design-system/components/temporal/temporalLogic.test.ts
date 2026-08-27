import { describe, expect, it } from "vitest";
import { resolveNowAnchor, valueForNow } from "./temporalLogic";

describe("resolveNowAnchor", () => {
  it("uses a full datetime anchor when provided", () => {
    const anchor = resolveNowAnchor("2026-08-26T14:30:45");
    expect(anchor.getFullYear()).toBe(2026);
    expect(anchor.getMonth()).toBe(7);
    expect(anchor.getDate()).toBe(26);
    expect(anchor.getHours()).toBe(14);
    expect(anchor.getMinutes()).toBe(30);
    expect(anchor.getSeconds()).toBe(45);
  });

  it("uses a date-only anchor with live clock time", () => {
    const anchor = resolveNowAnchor("2026-08-26");
    const live = new Date();
    expect(anchor.getFullYear()).toBe(2026);
    expect(anchor.getMonth()).toBe(7);
    expect(anchor.getDate()).toBe(26);
    expect(anchor.getHours()).toBe(live.getHours());
    expect(anchor.getMinutes()).toBe(live.getMinutes());
  });
});

describe("valueForNow", () => {
  const anchor = new Date(2026, 7, 26, 14, 32, 47);

  it("returns an ISO date for date mode", () => {
    expect(valueForNow("date", anchor)).toBe("2026-08-26");
  });

  it("returns snapped time for time mode", () => {
    expect(valueForNow("time", anchor, { minuteStep: 5 })).toBe("14:30");
    expect(valueForNow("time", anchor, { withSeconds: true, secondStep: 15 })).toBe("14:32:45");
  });

  it("returns joined datetime for datetime mode", () => {
    expect(valueForNow("datetime", anchor, { minuteStep: 5 })).toBe("2026-08-26T14:30");
    expect(valueForNow("datetime", anchor, { withSeconds: true, secondStep: 15 })).toBe("2026-08-26T14:32:45");
  });
});
