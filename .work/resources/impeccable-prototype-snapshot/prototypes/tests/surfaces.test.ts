import { describe, expect, it } from "vitest";
import { router } from "../src/app/router";
import {
  CATALOG_ROUTE,
  PROTOTYPE_SURFACE_PATHS,
  SURFACE_COUNT,
  SURFACE_GROUPS,
} from "../src/data/fixtures/surfaces";

describe("prototype surface registry", () => {
  it("lists six unique demonstration channels", () => {
    expect(SURFACE_COUNT).toBe(6);
    expect(PROTOTYPE_SURFACE_PATHS).toHaveLength(6);
    expect(new Set(PROTOTYPE_SURFACE_PATHS).size).toBe(6);
  });

  it("keeps fixture paths aligned with the router", () => {
    const routerPaths = router.routes
      .map((route) => route.path)
      .filter((path): path is string => Boolean(path && path !== "*" && path !== "/" && path !== CATALOG_ROUTE));

    expect([...PROTOTYPE_SURFACE_PATHS].sort()).toEqual([...routerPaths].sort());
  });

  it("uses stable channel codes per path", () => {
    const codes = SURFACE_GROUPS.flatMap((group) => group.channels.map((channel) => channel.code));
    expect(codes).toEqual(["HOM", "JRN", "SES", "ADM", "REV", "GAL"]);
  });
});
