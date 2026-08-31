import type { ComponentPropsWithoutRef } from "react";
import { Stack } from "../../../design-system/components/layout/Stack";
import type { LayoutSpace } from "../../../design-system/components/layout/types";
import { cx } from "../../../lib/cx";

type InPlateHostProps = Omit<ComponentPropsWithoutRef<typeof Stack>, "className" | "gap"> & {
  className?: string;
  gap?: LayoutSpace;
};

/** Etched-frame inset host for readout grids with a docked in-plate foot. */
export function InPlateHost({ className, gap = "none", ...rest }: InPlateHostProps) {
  return (
    <Stack
      gap={gap}
      className={cx("in-plate-host", "plate-bleed", typeof className === "string" ? className : undefined)}
      {...rest}
    />
  );
}
