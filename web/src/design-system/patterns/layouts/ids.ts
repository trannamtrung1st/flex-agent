export const APPROVED_LAYOUT_IDS = ["management", "guided-task", "live-session", "reference"] as const;

export type ApprovedLayoutId = (typeof APPROVED_LAYOUT_IDS)[number];

export const PRODUCTION_LAYOUT_IDS = ["management", "guided-task", "live-session"] as const;

export type ProductionLayoutId = (typeof PRODUCTION_LAYOUT_IDS)[number];

export function isApprovedLayoutId(value: string): value is ApprovedLayoutId {
  return (APPROVED_LAYOUT_IDS as readonly string[]).includes(value);
}

export function isProductionLayoutId(value: string): value is ProductionLayoutId {
  return (PRODUCTION_LAYOUT_IDS as readonly string[]).includes(value);
}
