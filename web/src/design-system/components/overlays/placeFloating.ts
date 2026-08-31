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

function connector(trigger: OverlayRect, left: number, width: number) {
  return clamp(trigger.left + trigger.width / 2 - left, 0, Math.max(0, width));
}

function panelWidth(trigger: OverlayRect, floatingWidth: number, align: OverlayAlign, maxBox?: number) {
  const width = align === "stretch" ? Math.max(trigger.width, floatingWidth) : floatingWidth;
  return maxBox == null ? width : Math.min(width, maxBox);
}

export function placeFloating({
  trigger,
  floating,
  viewport,
  padding = 0,
  offset = 0,
  preferredSide,
  align,
  size = false,
}: PlaceFloatingInput): PlaceFloatingResult {
  const maxBox = Math.max(0, viewport.width - padding * 2);
  const width = panelWidth(trigger, floating.width, align, maxBox);
  const maxLeft = viewport.width - padding - width;
  const left = clamp(alignedLeft(trigger, width, align), padding, Math.max(padding, maxLeft));

  // Attach if the preferred side fits the full panel; else flip only when
  // the opposite side also fits fully. Otherwise pin flush to the viewport
  // (no inset). Covering the trigger is allowed. Do not shrink to the leftover
  // gap beside the trigger.
  const spaceAbove = trigger.top - padding - offset;
  const spaceBelow = viewport.height - padding - (trigger.top + trigger.height) - offset;
  const fitsTop = floating.height <= spaceAbove;
  const fitsBottom = floating.height <= spaceBelow;

  let side: OverlaySide = preferredSide;
  if (preferredSide === "top" && !fitsTop && fitsBottom) side = "bottom";
  else if (preferredSide === "bottom" && !fitsBottom && fitsTop) side = "top";

  const viewportBox = Math.max(0, viewport.height - padding * 2);
  let height = floating.height;
  let maxHeight: number | undefined;
  if (size && height > viewportBox) {
    height = viewportBox;
    maxHeight = height;
  }

  const attachedTop = side === "top"
    ? trigger.top - offset - height
    : trigger.top + trigger.height + offset;

  const minTop = padding;
  const maxTop = viewport.height - padding - height;
  const clampedTop = clamp(attachedTop, minTop, Math.max(minTop, maxTop));

  return {
    top: clampedTop,
    left,
    side,
    width: align === "stretch" ? width : undefined,
    maxHeight,
    connector: connector(trigger, left, width),
  };
}
