import { describe, expect, it } from "vitest";
import { gallerySectionItems, gallerySections } from "./gallerySections";

describe("gallerySections", () => {
  it("lists Keys and Key group under Foundations", () => {
    const foundations = gallerySections.find((group) => group.id === "foundations");
    expect(foundations?.items.map((item) => item.id)).toEqual([
      "colors",
      "type",
      "typography",
      "keys",
      "key-group",
      "pane",
      "frame",
    ]);
  });

  it("does not keep a separate Keys index-rail group", () => {
    const ids: string[] = gallerySections.map((group) => group.id);
    expect(ids).not.toContain("keys");
    expect(ids).toContain("shells");
  });

  it("registers Compact ID between readout grid and datatable", () => {
    const data = gallerySections.find((group) => group.id === "data");
    expect(data?.items.map((item) => item.id)).toEqual([
      "marks",
      "select-mark",
      "readout",
      "readout-grid",
      "compact-id",
      "datatable",
      "datatable-scroll",
    ]);
  });

  it("assigns a unique scroll order index to every section", () => {
    const orders = gallerySectionItems.map((item) => item.id);
    expect(new Set(orders).size).toBe(orders.length);
  });

  it("places Key group before pane surfaces in scroll order", () => {
    const orders = gallerySectionItems.map((item) => item.id);
    expect(orders.indexOf("type")).toBeLessThan(orders.indexOf("typography"));
    expect(orders.indexOf("typography")).toBeLessThan(orders.indexOf("keys"));
    expect(orders.indexOf("keys")).toBeLessThan(orders.indexOf("key-group"));
    expect(orders.indexOf("key-group")).toBeLessThan(orders.indexOf("pane"));
    expect(orders.indexOf("pane")).toBeLessThan(orders.indexOf("strip"));
  });

  it("registers composition primitives after shells", () => {
    const composition = gallerySections.find((group) => group.id === "composition");
    expect(composition?.items.map((item) => item.id)).toEqual([
      "composition-stack",
      "composition-inline",
      "composition-grid",
      "composition-split",
      "composition-container",
      "composition-inset",
      "composition-recipes",
    ]);
    const orders = gallerySectionItems.map((item) => item.id);
    expect(orders.indexOf("layout-reference")).toBeLessThan(orders.indexOf("composition-stack"));
    expect(orders.indexOf("composition-recipes")).toBeLessThan(orders.indexOf("form-recipes"));
    expect(orders.indexOf("form-recipes")).toBeLessThan(orders.indexOf("form"));
  });

  it("registers File intake after Form controls", () => {
    const overlays = gallerySections.find((group) => group.id === "overlays-input");
    expect(overlays?.items.map((item) => item.id)).toEqual([
      "form-recipes",
      "form",
      "file",
      "datetime",
      "searchable-select",
      "multiselect",
      "menu",
      "dialog",
    ]);
    const orders = gallerySectionItems.map((item) => item.id);
    expect(orders.indexOf("form")).toBeLessThan(orders.indexOf("file"));
    expect(orders.indexOf("file")).toBeLessThan(orders.indexOf("datetime"));
  });

  it("lists management work-bay variants after the management shell", () => {
    const shells = gallerySections.find((group) => group.id === "shells");
    expect(shells?.items.map((item) => item.id)).toEqual([
      "layout-management",
      "layout-management-index",
      "layout-management-record",
      "layout-management-setup",
      "layout-management-empty",
      "layout-management-ceremony",
      "layout-management-loading",
      "layout-management-split",
      "layout-guided-task",
      "layout-live-session",
      "layout-reference",
    ]);
  });
});
