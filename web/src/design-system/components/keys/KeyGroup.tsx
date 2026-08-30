import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { Inline } from "../layout/Inline";
import type { LayoutJustify } from "../layout/types";

export function KeyGroup({
  children,
  className,
  justify = "start",
  "aria-label": ariaLabel,
}: {
  children: ReactNode;
  className?: string;
  justify?: LayoutJustify;
  "aria-label"?: string;
}) {
  return (
    <Inline
      className={cx("key-group", className)}
      gap="2.5"
      align="center"
      justify={justify}
      wrap
      role="group"
      aria-label={ariaLabel}
    >
      {children}
    </Inline>
  );
}
