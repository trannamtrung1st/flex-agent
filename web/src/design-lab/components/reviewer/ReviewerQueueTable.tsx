import type { ComponentProps } from "react";
import { cx } from "../../../lib/cx";
import { DataTableShell, DatatableEmpty } from "../../../design-system";

type ReviewerQueueTableShellProps = ComponentProps<typeof DataTableShell>;

export function ReviewerQueueTableShell({ className, ...rest }: ReviewerQueueTableShellProps) {
  return <DataTableShell {...rest} className={cx("queue-datatable", className)} />;
}

type ReviewerQueueEmptyProps = ComponentProps<typeof DatatableEmpty>;

export function ReviewerQueueEmpty({ className, ...rest }: ReviewerQueueEmptyProps) {
  return <DatatableEmpty {...rest} className={cx("queue-empty-plate", className)} />;
}
