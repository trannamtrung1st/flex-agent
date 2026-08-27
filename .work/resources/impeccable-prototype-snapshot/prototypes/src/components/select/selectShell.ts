import type { CSSProperties } from "react";

/** Override popover width tokens on `.select-shell` when a surface needs custom sizing. */
export type SelectPopoverConfig = {
  popoverWidth?: string;
  popoverMinWidth?: string;
  popoverMaxWidth?: string;
  popoverOffsetX?: string;
  popoverMaxHeight?: string;
};

export function selectShellStyle(config?: SelectPopoverConfig): CSSProperties | undefined {
  if (!config) return undefined;
  const style: Record<string, string> = {};
  if (config.popoverWidth) style["--select-popover-width"] = config.popoverWidth;
  if (config.popoverMinWidth) style["--select-popover-min-width"] = config.popoverMinWidth;
  if (config.popoverMaxWidth) style["--select-popover-max-width"] = config.popoverMaxWidth;
  if (config.popoverOffsetX) style["--select-popover-offset-x"] = config.popoverOffsetX;
  if (config.popoverMaxHeight) style["--select-popover-max-height"] = config.popoverMaxHeight;
  return Object.keys(style).length ? (style as CSSProperties) : undefined;
}
