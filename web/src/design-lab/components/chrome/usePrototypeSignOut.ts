import { useMemo, useState } from "react";
import { labAccountActions } from "./operator";

export function usePrototypeSignOut() {
  const [open, setOpen] = useState(false);
  const actions = useMemo(() => labAccountActions(() => setOpen(true)), []);
  return { actions, signOutOpen: open, setSignOutOpen: setOpen };
}
