import type { ReactNode } from "react";

export function AssignmentHead({
  title,
  meta,
  status,
}: {
  title: string;
  meta?: string;
  status?: ReactNode;
}) {
  return (
    <header className="assignment-head">
      <div className="assignment-ident">
        <h1 className="assignment-title">{title}</h1>
        {meta ? <p className="assignment-meta">{meta}</p> : null}
      </div>
      {status}
    </header>
  );
}
