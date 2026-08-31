import type { ReactNode } from "react";

/** Centers the campaigns-wall empty well when a record address is missing. */
export function CampaignsUnavailableWell({ children }: { children: ReactNode }) {
  return <div className="campaigns-unavailable">{children}</div>;
}
