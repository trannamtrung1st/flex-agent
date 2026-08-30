import type { ReactNode } from "react";
import { Stack } from "../../design-system";

export type SubmissionVersionRow = {
  key: string;
  versionNumber: number;
  name: ReactNode;
  meta: ReactNode;
};

export function SubmissionVersionList({
  rows,
  reversed = false,
  label,
}: {
  rows: readonly SubmissionVersionRow[];
  reversed?: boolean;
  label?: string;
}) {
  return (
    <ol reversed={reversed || undefined} aria-label={label}>
      {rows.map((row) => (
        <li key={row.key} data-sequence={String(row.versionNumber)} value={row.versionNumber}>
          <Stack gap="2">
            <span>{row.name}</span>
            {row.meta}
          </Stack>
        </li>
      ))}
    </ol>
  );
}
