import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";

export function FrozenLine({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return <p className={cx("frozen-line", className)}>{children}</p>;
}
