import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { Inline } from "../layout/Inline";

export function KeyGroup({
  children,
  className,
  "aria-label": ariaLabel,
}: {
  children: ReactNode;
  className?: string;
  "aria-label"?: string;
}) {
  return (
    <Inline
      className={cx("key-group", className)}
      gap="2.5"
      align="center"
      wrap
      role="group"
      aria-label={ariaLabel}
    >
      {children}
    </Inline>
  );
}
