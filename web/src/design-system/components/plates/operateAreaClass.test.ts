import { operateAreaClass, registryTableHug, type OperateBay } from "./operateAreaClass";

const BAY_CLASSES: Record<OperateBay, string[]> = {
  workspace: ["workspace-area", "work-plane"],
  record: ["workspace-area", "work-plane", "record-plane"],
  registry: ["workspace-area", "work-plane", "registry-wall"],
  ceremony: ["workspace-area", "work-plane", "work-plane--ceremony"],
};

function classSet(value: string) {
  return new Set(value.split(/\s+/).filter(Boolean));
}

describe("operateAreaClass", () => {
  it("defaults to the workspace host bundle", () => {
    expect(classSet(operateAreaClass())).toEqual(new Set(BAY_CLASSES.workspace));
  });

  it.each(Object.entries(BAY_CLASSES) as Array<[OperateBay, string[]]>)(
    "emits the locked class bundle for bay %s",
    (bay, classes) => {
      expect(classSet(operateAreaClass(bay))).toEqual(new Set(classes));
    },
  );

  it("replaces the bay bundle when hostClassName is set", () => {
    expect(classSet(operateAreaClass("workspace", { hostClassName: "replacement-host" }))).toEqual(
      new Set(["replacement-host"]),
    );
    expect(operateAreaClass("workspace", { hostClassName: "replacement-host" })).not.toContain("workspace-area");
    expect(operateAreaClass("registry", { hostClassName: "other-host" })).not.toContain("workspace-area");
  });

  it("hugs registry tables for empty and short visible lists", () => {
    expect(registryTableHug(0)).toBe("registry");
    expect(registryTableHug(1)).toBe("registry");
    expect(registryTableHug(4)).toBe("registry");
    expect(registryTableHug(5)).toBeUndefined();
  });

  it("adds registry hug only on the production registry bay", () => {
    expect(operateAreaClass("registry", { hug: "registry" })).toContain("registry-wall--hug");
    expect(operateAreaClass("workspace", { hostClassName: "replacement-host", hug: "registry" })).not.toContain(
      "registry-wall--hug",
    );
    expect(operateAreaClass("workspace", { hug: "registry" })).not.toContain("registry-wall--hug");
    expect(operateAreaClass("ceremony", { hug: "registry" })).not.toContain("registry-wall--hug");
  });

  it("does not emit assignment-board hug from the generic helper", () => {
    expect(operateAreaClass("workspace")).not.toContain("assignment-board--hug");
    expect(operateAreaClass("workspace", { hostClassName: "workspace-area extra-host" })).not.toContain(
      "assignment-board--hug",
    );
    expect(operateAreaClass("registry", { hug: "registry" })).not.toContain("assignment-board--hug");
  });

  it("adds danger without dropping the bay bundle", () => {
    const classes = classSet(operateAreaClass("ceremony", { danger: true }));
    expect(classes).toEqual(new Set([...BAY_CLASSES.ceremony, "workspace-area--danger"]));
  });

  it("keeps additive className after the bay bundle", () => {
    const value = operateAreaClass("workspace", { hostClassName: "workspace-area extra-host", className: "is-released is-adjusting" });
    const classes = classSet(value);
    expect(classes).toEqual(new Set(["workspace-area", "extra-host", "is-released", "is-adjusting"]));
    expect(value.endsWith("is-released is-adjusting")).toBe(true);
  });

  it("appends danger and className after a replacement host", () => {
    const value = operateAreaClass("workspace", {
      hostClassName: "replacement-host",
      danger: true,
      className: "is-released",
    });
    expect(classSet(value)).toEqual(new Set(["replacement-host", "workspace-area--danger", "is-released"]));
  });
});
