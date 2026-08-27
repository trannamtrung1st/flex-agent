export const breakpoints = {
  dialog: 480,
  compact: 720,
  session: 760,
  gallery: 900,
  reviewerDrawer: 960,
  pageScroll: 1080,
  adminDrawer: 1080,
  wideGrid: 1180,
} as const;

export type BreakpointName = keyof typeof breakpoints;

export function maxWidthQuery(name: BreakpointName) {
  return `(max-width: ${breakpoints[name]}px)`;
}
