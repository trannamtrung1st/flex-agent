import type { IndexRailGroup } from "../../../design-system/components/navigation";

export const gallerySections = [
  {
    id: "foundations",
    label: "Foundations",
    items: [
      { id: "colors", label: "Colors" },
      { id: "type", label: "Type voices" },
      { id: "typography", label: "Typography" },
      { id: "keys", label: "Keys" },
      { id: "key-group", label: "Key group" },
      { id: "pane", label: "Pane" },
      { id: "work-well", label: "Work well" },
      { id: "frame", label: "Etched frame" },
      { id: "assignment-plate", label: "Assignment plate" },
    ],
  },
  {
    id: "navigation",
    label: "Navigation",
    items: [
      { id: "strip", label: "Command strip" },
      { id: "nav-rail", label: "Nav rail" },
      { id: "nav-groups", label: "Nav groups" },
      { id: "gangway", label: "Gangway" },
      { id: "breadcrumbs", label: "Breadcrumbs" },
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
      { id: "compact-id", label: "Compact ID" },
      { id: "item-list", label: "Item list" },
      { id: "datatable", label: "Datatable" },
      { id: "datatable-scroll", label: "Datatable scroll" },
    ],
  },
  {
    id: "feedback",
    label: "Feedback",
    items: [
      { id: "toast", label: "Toast" },
      { id: "tooltip", label: "Tooltip" },
      { id: "advisory", label: "Advisory" },
      { id: "alert", label: "Alert" },
      { id: "error-summary", label: "Error summary" },
      { id: "empty", label: "Empty state" },
      { id: "wait", label: "Wait & progress" },
      { id: "wait-panel", label: "Wait panel" },
    ],
  },
  {
    id: "shells",
    label: "Shells",
    items: [
      { id: "layout-management", label: "Management" },
      { id: "layout-management-index", label: "Management index" },
      { id: "layout-management-record", label: "Management record" },
      { id: "layout-management-setup", label: "Management setup" },
      { id: "layout-management-empty", label: "Management empty" },
      { id: "layout-management-ceremony", label: "Management ceremony" },
      { id: "layout-management-loading", label: "Management loading" },
      { id: "layout-management-split", label: "Management split" },
      { id: "layout-guided-task", label: "Guided task" },
      { id: "layout-live-session", label: "Live session" },
      { id: "layout-reference", label: "Reference" },
    ],
  },
  {
    id: "composition",
    label: "Composition",
    items: [
      { id: "composition-stack", label: "Stack" },
      { id: "composition-inline", label: "Inline" },
      { id: "composition-grid", label: "Grid" },
      { id: "composition-split", label: "Split bay" },
      { id: "composition-container", label: "Container" },
      { id: "composition-inset", label: "Inset" },
    ],
  },
  {
    id: "overlays-input",
    label: "Overlays & input",
    items: [
      { id: "form-recipes", label: "Form recipes" },
      { id: "form", label: "Form controls" },
      { id: "file", label: "File intake" },
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

/** Retired deck hashes that should open a current section. */
export const gallerySectionHashAliases = {
  "composition-recipes": "form-recipes",
} as const satisfies Partial<Record<string, GallerySectionId>>;

const gallerySectionIdSet = new Set<GallerySectionId>(
  gallerySectionItems.map((item) => item.id),
);

export function resolveGallerySectionHash(hash: string): GallerySectionId | null {
  const id = hash.startsWith("#") ? hash.slice(1) : hash;
  if (!id) return null;
  if (gallerySectionIdSet.has(id as GallerySectionId)) return id as GallerySectionId;
  const alias = gallerySectionHashAliases[id as keyof typeof gallerySectionHashAliases];
  return alias ?? null;
}
