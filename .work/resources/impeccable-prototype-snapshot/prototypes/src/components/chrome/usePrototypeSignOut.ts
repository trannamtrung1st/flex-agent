import { useMemo, useState } from "react";
import { prototypeAccountActions } from "./operator";

export function usePrototypeSignOut() {
  const [open, setOpen] = useState(false);
  const actions = useMemo(() => prototypeAccountActions(() => setOpen(true)), []);
  return { actions, signOutOpen: open, setSignOutOpen: setOpen };
}
