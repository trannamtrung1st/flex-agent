import type { To } from "react-router-dom";
import type { Theme } from "../../../lib/theme";

export type ShellMode = "management" | "guided-task" | "live-session" | "reference";

export type OperatorRole = "Participant" | "Administrator" | "Reviewer";

export type OperatorIdentity = {
  shortId: string;
  fullId: string;
  role: OperatorRole;
  home: To;
};

export type OperatorAction = {
  id: string;
  label: string;
  state: "enabled" | "disabled";
  disabledReason?: string;
  intent?: "default" | "signout";
  onSelect?: () => void;
};

export function operatorAccountActions(
  theme: Theme,
  toggleTheme: () => void,
  onSignOut: () => void,
  extras: OperatorAction[] = [],
): OperatorAction[] {
  return [
    ...extras,
    {
      id: "theme",
      label: theme === "dark" ? "Switch to light theme" : "Switch to dark theme",
      state: "enabled",
      onSelect: toggleTheme,
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
