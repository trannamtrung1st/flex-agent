import type { ReactNode } from "react";
import { NativeDialog } from "./NativeDialog";
import { cx } from "../../../lib/cx";

/** Native dialog shell only. Callers compose `DialogPlate` (or a local interior) as children. */
export function CeremonyDialog({
  open,
  onClose,
  labelledBy,
  id,
  className,
  children,
}: {
  open: boolean;
  onClose: () => void;
  labelledBy: string;
  id?: string;
  className?: string;
  children: ReactNode;
}) {
  return (
    <NativeDialog open={open} onClose={onClose} className={cx("dialog", className)} labelledBy={labelledBy} id={id}>
      {children}
    </NativeDialog>
  );
}
