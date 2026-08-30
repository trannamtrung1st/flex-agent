import type { OperatorAction, OperatorIdentity } from "../../../design-system/components/chrome/operator";
import { operatorAccountActions } from "../../../design-system/components/chrome/operator";
import type { Theme } from "../../../lib/theme";

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

export function labAccountActions(
  theme: Theme,
  toggleTheme: () => void,
  onSignOut: () => void,
): OperatorAction[] {
  return operatorAccountActions(theme, toggleTheme, onSignOut, [
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
  ]);
}
