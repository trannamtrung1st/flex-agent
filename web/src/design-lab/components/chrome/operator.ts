import type { To } from "react-router-dom";
import type { OperatorAction, OperatorIdentity } from "../../../design-system/components/chrome/operator";

export type { OperatorAction, OperatorIdentity, OperatorRole, ShellMode } from "../../../design-system/components/chrome/operator";
export { CATALOG_NAV, CATALOG_ROUTE } from "../../data/fixtures/surfaces";

export const PARTICIPANT_HOME = "/participant-home" as const;
export const ADMINISTRATOR_HOME = "/admin-console/enrollments" as const;
export const REVIEWER_HOME = "/reviewer-console" as const;

export const PARTICIPANT_IDENTITY: OperatorIdentity = {
  shortId: "CND-8842",
  fullId: "CND-8842-19",
  role: "Participant",
  home: PARTICIPANT_HOME,
};

export const ADMINISTRATOR_IDENTITY: OperatorIdentity = {
  shortId: "ADM-7X92",
  fullId: "ADM-7X92-19",
  role: "Administrator",
  home: ADMINISTRATOR_HOME,
};

export const REVIEWER_IDENTITY: OperatorIdentity = {
  shortId: "REV-2204",
  fullId: "REV-2204-07",
  role: "Reviewer",
  home: REVIEWER_HOME,
};

export function administratorHome(campaignId?: string): To {
  if (!campaignId) return ADMINISTRATOR_HOME;
  return { pathname: ADMINISTRATOR_HOME, search: `?campaign=${encodeURIComponent(campaignId)}` };
}

export function labAccountActions(onSignOut: () => void): OperatorAction[] {
  return [
    {
      id: "profile",
      label: "Profile",
      state: "disabled",
      disabledReason: "Unavailable in this design lab.",
    },
    {
      id: "preferences",
      label: "Preferences",
      state: "disabled",
      disabledReason: "Unavailable in this design lab.",
    },
    {
      id: "signout",
      label: "Sign out",
      state: "enabled",
      intent: "signout",
      onSelect: onSignOut,
    },
  ];
}
