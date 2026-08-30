import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";

export function FormSection({
  legend,
  legendId,
  className,
  children,
}: {
  legend: ReactNode;
  legendId?: string;
  className?: string;
  children: ReactNode;
}) {
  return (
    <fieldset className={cx("form-section", className)}>
      <legend id={legendId}>{legend}</legend>
      {children}
    </fieldset>
  );
}
