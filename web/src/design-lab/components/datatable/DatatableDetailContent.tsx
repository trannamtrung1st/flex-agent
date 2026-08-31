import type { ReactNode } from "react";
import { KeyGroup } from "../../../design-system/components/keys/KeyGroup";

/** Enrollment/Deck expand interior: term/value band plus optional keys. */
export function DatatableDetailReadouts({ children }: { children: ReactNode }) {
  return <dl className="datatable-detail-readouts">{children}</dl>;
}

export function DatatableDetailField({
  term,
  children,
}: {
  term: string;
  children: ReactNode;
}) {
  return (
    <div className="datatable-detail-field">
      <dt>{term}</dt>
      <dd>{children}</dd>
    </div>
  );
}

export function DatatableDetailKeys({ children }: { children: ReactNode }) {
  return <KeyGroup className="datatable-detail-keys">{children}</KeyGroup>;
}
