import type { ReactNode } from "react";
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
  const widthClass = width === "default" ? "" : ` dialog-plate--${width}`;
  return (
    <div className={`dialog-plate${widthClass}${className ? ` ${className}` : ""}`}>
      {children}
    </div>
  );
}

export function DialogPlateHead({
  title,
  titleId,
  marker = true,
  className = "dialog-head",
  titleClassName = "dialog-title",
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
    <header className={className}>
      {marker ? <span className="warn-triangle" aria-hidden="true" /> : null}
      <h2 className={titleClassName} id={titleId}>
        {title}
      </h2>
      {children}
    </header>
  );
}

export function DialogPlateBody({
  className = "dialog-body",
  children,
}: {
  className?: string;
  children: ReactNode;
}) {
  return <div className={className}>{children}</div>;
}

export function DialogPlateFooter({
  className = "dialog-foot",
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
  if (/\bceremony-foot\b/.test(className)) {
    return <footer className={className}>{children}</footer>;
  }
  return (
    <PlateFoot className={className} arrangement={arrangement} secondary={secondary} primary={primary}>
      {children}
    </PlateFoot>
  );
}
