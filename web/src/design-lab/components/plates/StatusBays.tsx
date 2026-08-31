import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { Stack } from "../../../design-system/components/layout/Stack";

export function StatusBays({
  dense,
  children,
}: {
  dense?: boolean;
  children: ReactNode;
}) {
  return <div className={cx("bays", dense && "bays--dense")}>{children}</div>;
}

export function StatusBay({
  id,
  label,
  empty,
  children,
}: {
  id: string;
  label: string;
  empty?: string;
  children?: ReactNode;
}) {
  const headingId = `bay-${id}`;
  return (
    <Stack as="section" className="bay" gap="none" aria-labelledby={headingId}>
      <h2 className="bay-head" id={headingId}>
        {label}
      </h2>
      <Stack gap="4" className="bay-plates">
        {children ?? (empty ? <p className="bay-empty">{empty}</p> : null)}
      </Stack>
    </Stack>
  );
}
