import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { PlateFoot, type PlateFootArrangement } from "../plates/EtchedFrame";

export type DialogPlateWidth = "narrow" | "default" | "wide";

export function DialogPlate({
  width = "default",
  className,
  children,
}: {
  width?: DialogPlateWidth;
  className?: string;
  children: ReactNode;
}) {
  return (
    <div className={cx("dialog-plate", width !== "default" && `dialog-plate--${width}`, className)}>
      {children}
    </div>
  );
}

export function DialogPlateHead({
  title,
  titleId,
  marker = true,
  className,
  titleClassName,
  children,
}: {
  title: ReactNode;
  titleId: string;
  marker?: boolean;
  className?: string;
  titleClassName?: string;
  children?: ReactNode;
}) {
  return (
    <header className={className ?? "dialog-head"}>
      {marker ? <span className="warn-triangle" aria-hidden="true" /> : null}
      <h2 className={titleClassName ?? "dialog-title"} id={titleId}>
        {title}
      </h2>
      {children}
    </header>
  );
}

export function DialogPlateBody({ className, children }: { className?: string; children: ReactNode }) {
  return <div className={className ?? "dialog-body"}>{children}</div>;
}

export function DialogPlateFooter({
  className,
  children,
  arrangement = "end",
  secondary,
  primary,
}: {
  className?: string;
  children?: ReactNode;
  arrangement?: PlateFootArrangement;
  secondary?: ReactNode;
  primary?: ReactNode;
}) {
  return (
    <PlateFoot className={className ?? "dialog-foot"} arrangement={arrangement} secondary={secondary} primary={primary}>
      {children}
    </PlateFoot>
  );
}
