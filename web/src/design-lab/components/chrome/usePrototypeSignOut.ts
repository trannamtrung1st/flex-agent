import { useMemo, useState } from "react";
import { useTheme } from "../../../lib/useTheme";
import { labAccountActions } from "./operator";

export function usePrototypeSignOut() {
  const [open, setOpen] = useState(false);
  const { theme, toggleTheme } = useTheme();
  const actions = useMemo(
    () => labAccountActions(theme, toggleTheme, () => setOpen(true)),
    [theme, toggleTheme],
  );
  return { actions, signOutOpen: open, setSignOutOpen: setOpen };
}
