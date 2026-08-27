import { isProductionLayoutId } from "../design-system";
import { productionRouter } from "./production-routes";
import { PRODUCTION_ROUTE_LAYOUTS } from "./production-route-layouts";
import { collectRouteLeaves } from "./route-leaves";
import { routeLayoutMappingViolations } from "./route-layout-governance";

describe("production route layout manifest", () => {
  const leaves = collectRouteLeaves(productionRouter.routes);

  it("assigns every non-redirect leaf exactly one production layout", () => {
    const mapped = Object.keys(PRODUCTION_ROUTE_LAYOUTS);
    const renderable = leaves.filter((leaf) => !leaf.redirect && !leaf.layoutHost);
    expect(renderable.map((leaf) => leaf.path).sort()).toEqual([...mapped].sort());
    for (const leaf of renderable) {
      expect(mapped.filter((path) => path === leaf.path)).toHaveLength(1);
      expect(isProductionLayoutId(PRODUCTION_ROUTE_LAYOUTS[leaf.path as keyof typeof PRODUCTION_ROUTE_LAYOUTS])).toBe(true);
    }
  });

  it("does not give redirects an independent layout", () => {
    const redirects = leaves.filter((leaf) => leaf.redirect).map((leaf) => leaf.path);
    expect(redirects.length).toBeGreaterThan(0);
    for (const path of redirects) {
      expect(PRODUCTION_ROUTE_LAYOUTS).not.toHaveProperty(path);
    }
  });

  it("rejects reference as a production layout id", () => {
    expect(isProductionLayoutId("reference")).toBe(false);
    expect(Object.values(PRODUCTION_ROUTE_LAYOUTS)).not.toContain("reference");
  });

  it("fails when a renderable leaf is omitted from the manifest", () => {
    const omitted = Object.keys(PRODUCTION_ROUTE_LAYOUTS).filter((path) => path !== "/my-work");
    const required = leaves.filter((leaf) => !leaf.redirect && !leaf.layoutHost);
    expect(routeLayoutMappingViolations(omitted, required)).toContain("unmapped route '/my-work'");
  });
});
