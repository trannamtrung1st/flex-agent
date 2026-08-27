export type SurfaceChannel = {
  code: string;
  path: string;
  title: string;
  note: string;
};

export type SurfaceGroup = {
  id: string;
  label: string;
  channels: SurfaceChannel[];
};

export const SURFACE_GROUPS: SurfaceGroup[] = [
  {
    id: "participant",
    label: "Participant",
    channels: [
      {
        code: "HOM",
        path: "/participant-home",
        title: "Status Bays",
        note: "Assigned work roster grouped by enrollment state.",
      },
      {
        code: "JRN",
        path: "/participant-journey",
        title: "Assignment Station",
        note: "Instructions, consent gate, submission, and session entry.",
      },
      {
        code: "SES",
        path: "/participant-session",
        title: "Examination Console",
        note: "Timed text session with governed Agent. Demo states: live, warned, complete.",
      },
    ],
  },
  {
    id: "administrator",
    label: "Administrator",
    channels: [
      {
        code: "ADM",
        path: "/admin-console",
        title: "Administration",
        note: "Administrator shell with campaign registry, enrollment manifest, and activation ceremony.",
      },
    ],
  },
  {
    id: "reviewer",
    label: "Reviewer",
    channels: [
      {
        code: "REV",
        path: "/reviewer-console",
        title: "Review Console",
        note: "Evidence ledger, tethered transcript, adjust and release flows.",
      },
    ],
  },
  {
    id: "shared",
    label: "Shared",
    channels: [
      {
        code: "GAL",
        path: "/shared/gallery",
        title: "Component Deck",
        note: "Living catalog of Shipboard Terminal controls and shell grammar.",
      },
    ],
  },
];

export const SURFACE_COUNT = SURFACE_GROUPS.reduce((sum, group) => sum + group.channels.length, 0);

export const PROTOTYPE_SURFACE_PATHS = SURFACE_GROUPS.flatMap((group) =>
  group.channels.map((channel) => channel.path),
);

export const CATALOG_ROUTE = "/surfaces" as const;
export const CATALOG_NAV = { to: CATALOG_ROUTE, label: "Index" } as const;
