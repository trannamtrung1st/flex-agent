export const OVERLAY_PLATE_CLASS = "select-popover popover-surface menu-surface";

/** 1px overlap on the placement axis so the overlay hairline covers the
 *  trigger-adjacent field bezel. Select/menu/datetime plates open above or
 *  below only; `align` does not apply a left/right overlap. Plaques keep
 *  their own gap. */
export const OVERLAY_PLATE_OFFSET = -1;

export function overlayPlateClass(...parts: Array<string | false | null | undefined>) {
  return [OVERLAY_PLATE_CLASS, ...parts.filter((part): part is string => Boolean(part))].join(" ");
}
