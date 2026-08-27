import { createContext, useContext, type ReactNode } from "react";
import type { ApprovedLayoutId } from "./ids";

const LayoutAssignmentContext = createContext<ApprovedLayoutId | null>(null);

export function LayoutAssignment({
  id,
  children,
}: {
  id: ApprovedLayoutId;
  children: ReactNode;
}) {
  return <LayoutAssignmentContext.Provider value={id}>{children}</LayoutAssignmentContext.Provider>;
}

export function useAssignedLayoutId(self: ApprovedLayoutId) {
  const assigned = useContext(LayoutAssignmentContext);
  if (assigned != null && assigned !== self) {
    throw new Error(`Rendered layout '${self}' where '${assigned}' is assigned`);
  }
}
