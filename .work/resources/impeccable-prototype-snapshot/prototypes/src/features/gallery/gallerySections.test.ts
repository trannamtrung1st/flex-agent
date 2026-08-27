import { describe, expect, it } from "vitest";
import {
  gallerySectionIndex,
  gallerySectionItem,
  gallerySectionItems,
  gallerySections,
} from "./gallerySections";

describe("gallerySections", () => {
  it("keeps the gallery group and section order in one unique registry", () => {
    expect(gallerySections.map((group) => group.label)).toEqual([
      "Foundations",
      "Navigation",
      "Data",
      "Feedback",
      "Overlays & input",
    ]);

    const ids = gallerySections.flatMap((group) => group.items.map((item) => item.id));
    expect(ids).toEqual([
      "colors",
      "type",
      "keys",
      "pane",
      "frame",
      "strip",
      "nav-rail",
      "gangway",
      "drawer",
      "tabs",
      "footer",
      "marks",
      "select-mark",
      "readout",
      "readout-grid",
      "datatable",
      "toast",
      "tooltip",
      "advisory",
      "empty",
      "wait",
      "form",
      "datetime",
      "searchable-select",
      "multiselect",
      "menu",
      "dialog",
    ]);
    expect(new Set(ids).size).toBe(ids.length);
    expect(gallerySectionItems.map((item) => item.id)).toEqual(ids);
    expect(gallerySectionItem("datatable")).toEqual({ id: "datatable", label: "Datatable" });
    expect(gallerySectionIndex("dialog")).toBe(26);
  });
});
