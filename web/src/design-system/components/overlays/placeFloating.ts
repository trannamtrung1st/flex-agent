export type OverlayRect = {
  top: number;
  left: number;
  width: number;
  height: number;
};

export type OverlaySide = "top" | "bottom";
export type OverlayAlign = "start" | "center" | "end" | "stretch";

export type PlaceFloatingInput = {
  trigger: OverlayRect;
  floating: { width: number; height: number };
  viewport: { width: number; height: number };
  padding?: number;
  offset?: number;
  preferredSide: OverlaySide;
  align: OverlayAlign;
  size?: boolean;
};

export type PlaceFloatingResult = {
  top: number;
  left: number;
  side: OverlaySide;
  width?: number;
  maxHeight?: number;
  connector: number;
};

function clamp(value: number, min: number, max: number) {
  return Math.min(max, Math.max(min, value));
}

function alignedLeft(trigger: OverlayRect, width: number, align: OverlayAlign) {
  if (align === "end") return trigger.left + trigger.width - width;
  if (align === "center") return trigger.left + trigger.width / 2 - width / 2;
  return trigger.left;
}

export function placeFloating({
  trigger,
  floating,
  viewport,
  padding = 8,
  offset = 0,
  preferredSide,
  align,
  size = false,
}: PlaceFloatingInput): PlaceFloatingResult {
  const maxBox = Math.max(0, viewport.width - padding * 2);
  const width = Math.min(align === "stretch" ? trigger.width : floating.width, maxBox);
  const maxLeft = viewport.width - padding - width;
  const left = clamp(alignedLeft(trigger, width, align), padding, Math.max(padding, maxLeft));

  const spaceAbove = trigger.top - padding - offset;
  const spaceBelow = viewport.height - padding - (trigger.top + trigger.height) - offset;
  const fitsTop = floating.height <= spaceAbove;
  const fitsBottom = floating.height <= spaceBelow;

  let side: OverlaySide = preferredSide;
  if (preferredSide === "top" && !fitsTop && fitsBottom) side = "bottom";
  else if (preferredSide === "bottom" && !fitsBottom && fitsTop) side = "top";
  else if (!fitsTop && !fitsBottom) side = spaceBelow >= spaceAbove ? "bottom" : "top";

  const available = side === "top" ? spaceAbove : spaceBelow;
  const maxHeight = size && floating.height > available ? Math.max(0, available) : undefined;
  const height = maxHeight ?? floating.height;

  const top = side === "top"
    ? trigger.top - offset - height
    : trigger.top + trigger.height + offset;

  const minTop = padding;
  const maxTop = viewport.height - padding - height;
  const clampedTop = clamp(top, minTop, Math.max(minTop, maxTop));

  const connector = clamp(trigger.left + trigger.width / 2 - left, 0, Math.max(0, width));

  return {
    top: clampedTop,
    left,
    side,
    width: align === "stretch" ? width : undefined,
    maxHeight,
    connector,
  };
}
