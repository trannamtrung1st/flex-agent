import type { IndexRailGroup } from "../../components/navigation";

export const gallerySections = [
  {
    id: "foundations",
    label: "Foundations",
    items: [
      { id: "colors", label: "Colors" },
      { id: "type", label: "Type voices" },
      { id: "keys", label: "Keys" },
      { id: "pane", label: "Pane" },
      { id: "frame", label: "Etched frame" },
    ],
  },
  {
    id: "navigation",
    label: "Navigation",
    items: [
      { id: "strip", label: "Command strip" },
      { id: "nav-rail", label: "Nav rail" },
      { id: "gangway", label: "Gangway" },
      { id: "drawer", label: "Bulkhead drawer" },
      { id: "tabs", label: "Panel tabs" },
      { id: "footer", label: "Console footer" },
    ],
  },
  {
    id: "data",
    label: "Data",
    items: [
      { id: "marks", label: "Instrument marks" },
      { id: "select-mark", label: "Select mark" },
      { id: "readout", label: "Readout rows" },
      { id: "readout-grid", label: "Readout grid" },
      { id: "datatable", label: "Datatable" },
    ],
  },
  {
    id: "feedback",
    label: "Feedback",
    items: [
      { id: "toast", label: "Toast" },
      { id: "tooltip", label: "Tooltip" },
      { id: "advisory", label: "Advisory" },
      { id: "empty", label: "Empty state" },
      { id: "wait", label: "Wait & progress" },
    ],
  },
  {
    id: "overlays-input",
    label: "Overlays & input",
    items: [
      { id: "form", label: "Form controls" },
      { id: "datetime", label: "Date & time" },
      { id: "searchable-select", label: "Search select" },
      { id: "multiselect", label: "Search multiselect" },
      { id: "menu", label: "Option menu" },
      { id: "dialog", label: "Dialog" },
    ],
  },
] as const satisfies readonly IndexRailGroup[];

export type GallerySectionId =
  (typeof gallerySections)[number]["items"][number]["id"];
export type GallerySectionItem =
  (typeof gallerySections)[number]["items"][number];

export const gallerySectionItems: readonly GallerySectionItem[] = gallerySections.reduce<GallerySectionItem[]>(
  (items, group) => {
    items.push(...group.items);
    return items;
  },
  [],
);

export function gallerySectionItem(id: GallerySectionId) {
  const item = gallerySectionItems.find((candidate) => candidate.id === id);
  if (!item) throw new Error(`Unknown gallery section: ${id}`);
  return item;
}

export function gallerySectionIndex(id: GallerySectionId) {
  return gallerySectionItems.findIndex((item) => item.id === id);
}
