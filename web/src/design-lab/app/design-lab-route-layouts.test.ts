import { isApprovedLayoutId, isProductionLayoutId } from "../../design-system";
import { designLabRoutes } from "./router";
import { DESIGN_LAB_ROUTE_LAYOUTS } from "./design-lab-route-layouts";
import { collectRouteLeaves } from "../../router/route-leaves";
import { routeLayoutMappingViolations } from "../../router/route-layout-governance";

describe("design-lab route layout manifest", () => {
  const leaves = collectRouteLeaves(designLabRoutes);

  it("maps every layout host and non-redirect leaf once", () => {
    const mapped = Object.keys(DESIGN_LAB_ROUTE_LAYOUTS);
    const assignable = leaves.filter((leaf) => !leaf.redirect && (leaf.layoutHost || !leaf.path.startsWith("/admin-console/")));
    const adminChildren = leaves.filter((leaf) => leaf.path.startsWith("/admin-console/") && !leaf.layoutHost);
    expect(adminChildren.every((leaf) => leaf.redirect || !mapped.includes(leaf.path))).toBe(true);
    expect([...new Set(assignable.map((leaf) => leaf.path))].sort()).toEqual([...mapped].sort());
  });

  it("keeps the closed family and lab-only reference routes", () => {
    expect(DESIGN_LAB_ROUTE_LAYOUTS["/surfaces"]).toBe("reference");
    expect(DESIGN_LAB_ROUTE_LAYOUTS["/shared/gallery"]).toBe("reference");
    expect(DESIGN_LAB_ROUTE_LAYOUTS["*"]).toBe("reference");
    expect(DESIGN_LAB_ROUTE_LAYOUTS["/participant-home"]).toBe("management");
    expect(DESIGN_LAB_ROUTE_LAYOUTS["/admin-console"]).toBe("management");
    expect(DESIGN_LAB_ROUTE_LAYOUTS["/reviewer-console"]).toBe("management");
    expect(DESIGN_LAB_ROUTE_LAYOUTS["/participant-journey"]).toBe("guided-task");
    expect(DESIGN_LAB_ROUTE_LAYOUTS["/participant-session"]).toBe("live-session");
    for (const id of Object.values(DESIGN_LAB_ROUTE_LAYOUTS)) {
      expect(isApprovedLayoutId(id)).toBe(true);
    }
    expect(isProductionLayoutId("reference")).toBe(false);
  });

  it("does not map redirects independently", () => {
    for (const leaf of leaves.filter((item) => item.redirect)) {
      expect(DESIGN_LAB_ROUTE_LAYOUTS).not.toHaveProperty(leaf.path);
    }
  });

  it("fails when a layout host is omitted from the manifest", () => {
    const omitted = Object.keys(DESIGN_LAB_ROUTE_LAYOUTS).filter((path) => path !== "/participant-session");
    const required = leaves.filter((leaf) => !leaf.redirect && (leaf.layoutHost || !leaf.path.startsWith("/admin-console/")));
    const unique = [...new Map(required.map((leaf) => [leaf.path, leaf])).values()];
    expect(routeLayoutMappingViolations(omitted, unique)).toContain("unmapped route '/participant-session'");
  });
});
