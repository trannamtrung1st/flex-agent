import type { To } from "react-router-dom";

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
