import { cx } from "../../../lib/cx";
import type { EtchedFrameInset } from "./EtchedFrame";

export type OperateFrame = "record" | "registry" | "datatable" | "ceremony";

const FRAME_CLASSES: Record<OperateFrame, string> = {
  record: "record-frame",
  registry: "datatable-frame registry-frame",
  datatable: "datatable-frame",
  ceremony: "ceremony-frame",
};

const FRAME_DEFAULT_INSET: Partial<Record<OperateFrame, EtchedFrameInset>> = {
  registry: "flush",
  datatable: "flush",
};

export function operateFrameClass(frame?: OperateFrame, className?: string): string | undefined {
  const base = frame ? FRAME_CLASSES[frame] : undefined;
  return cx(base, className) || undefined;
}

export function resolveOperateFrameInset(
  frame?: OperateFrame,
  frameInset?: EtchedFrameInset,
): EtchedFrameInset | undefined {
  if (frameInset !== undefined) {
    return frameInset;
  }
  if (frame) {
    return FRAME_DEFAULT_INSET[frame] ?? "default";
  }
  return undefined;
}
