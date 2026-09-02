import { isKnownProductionLocator, PRODUCTION_ROUTE_LAYOUTS } from "./production-route-layouts";
import { layoutIdForPath } from "./route-layout-match";

describe("isKnownProductionLocator", () => {
  it("assigns guided-task to the assignment locator", () => {
    expect(layoutIdForPath("/my-work/enr-1", PRODUCTION_ROUTE_LAYOUTS)).toBe("guided-task");
    expect(layoutIdForPath("/my-work", PRODUCTION_ROUTE_LAYOUTS)).toBe("management");
    expect(layoutIdForPath("/sessions/55555555-5555-4555-8555-555555555555", PRODUCTION_ROUTE_LAYOUTS)).toBe("live-session");
    expect(layoutIdForPath("/sessions/55555555-5555-4555-8555-555555555555/operations", PRODUCTION_ROUTE_LAYOUTS)).toBe("management");
    expect(layoutIdForPath("/sessions/55555555-5555-4555-8555-555555555555/transcript", PRODUCTION_ROUTE_LAYOUTS)).toBe("management");
  });

  it("treats catalogued leaves as known and omits the wildcard", () => {
    expect(isKnownProductionLocator("/")).toBe(true);
    expect(isKnownProductionLocator("/activities")).toBe(true);
    expect(isKnownProductionLocator("/activities/new")).toBe(true);
    expect(isKnownProductionLocator("/activities/act-1/setup")).toBe(true);
    expect(isKnownProductionLocator("/results")).toBe(true);
    expect(isKnownProductionLocator("/not-a-destination")).toBe(false);
    expect(isKnownProductionLocator("/activities-extra")).toBe(false);
  });
});
