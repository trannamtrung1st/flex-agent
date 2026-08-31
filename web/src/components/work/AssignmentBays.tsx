import type { ReactNode } from "react";

export function AssignmentBays({ children }: { children: ReactNode }) {
  return <div className="assignment-bays">{children}</div>;
}

export function AssignmentBay({
  headingId,
  label,
  children,
}: {
  headingId: string;
  label: string;
  children: ReactNode;
}) {
  return (
    <section className="assignment-bay" aria-labelledby={headingId}>
      <h2 className="assignment-bay-head" id={headingId}>
        {label}
      </h2>
      {children}
    </section>
  );
}
