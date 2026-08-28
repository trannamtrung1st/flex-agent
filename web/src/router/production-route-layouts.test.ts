import { isKnownProductionLocator } from "./production-route-layouts";

describe("isKnownProductionLocator", () => {
  it("treats catalogued leaves as known and omits the wildcard", () => {
    expect(isKnownProductionLocator("/")).toBe(true);
    expect(isKnownProductionLocator("/activities")).toBe(true);
    expect(isKnownProductionLocator("/activities/act-1/setup")).toBe(true);
    expect(isKnownProductionLocator("/results")).toBe(true);
    expect(isKnownProductionLocator("/not-a-destination")).toBe(false);
    expect(isKnownProductionLocator("/activities-extra")).toBe(false);
  });
});
