import type { ReactNode } from "react";
import { NativeDialog } from "./NativeDialog";
import { cx } from "../../../lib/cx";

export type CeremonyDialogVariant = "default" | "ceremony" | "release";

/** Native dialog shell only. Callers compose `DialogPlate` (or a local interior) as children. */
export function CeremonyDialog({
  open,
  onClose,
  labelledBy,
  id,
  variant = "default",
  className,
  children,
}: {
  open: boolean;
  onClose: () => void;
  labelledBy: string;
  id?: string;
  variant?: CeremonyDialogVariant;
  className?: string;
  children: ReactNode;
}) {
  const dialogClass = cx(
    "dialog",
    variant === "release" && "release-dialog",
    variant === "ceremony" && "ceremony",
    className,
  );

  return (
    <NativeDialog open={open} onClose={onClose} className={dialogClass} labelledBy={labelledBy} id={id}>
      {variant === "ceremony" ? <div className="ceremony-cut">{children}</div> : children}
    </NativeDialog>
  );
}
