import { layoutIdForPath } from "./route-layout-match";

describe("layoutIdForPath", () => {
  const manifest = {
    "/": "management",
    "/activities": "management",
    "/activities/:activityId/setup": "management",
    "/admin-console": "management",
    "*": "reference",
  } as const;

  it("matches exact, parameterized, nested prefix, and wildcard paths", () => {
    expect(layoutIdForPath("/", manifest)).toBe("management");
    expect(layoutIdForPath("/activities/cmp-1/setup", manifest)).toBe("management");
    expect(layoutIdForPath("/admin-console/campaigns", manifest)).toBe("management");
    expect(layoutIdForPath("/unknown-channel", manifest)).toBe("reference");
  });

  it("returns undefined when a path is omitted and there is no wildcard", () => {
    const withoutWildcard = {
      "/": "management",
      "/activities": "management",
      "/activities/:activityId/setup": "management",
      "/admin-console": "management",
    } as const;
    expect(layoutIdForPath("/missing", withoutWildcard)).toBeUndefined();
  });
});
