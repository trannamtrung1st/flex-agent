import { useCallback, useEffect, useState, type RefObject } from "react";

export function useSearchableSelect({
  disabled,
  triggerRef,
  searchRef,
  focusOnOpen = "raf",
}: {
  disabled?: boolean;
  triggerRef: RefObject<HTMLElement | null>;
  searchRef: RefObject<HTMLInputElement | null>;
  focusOnOpen?: "raf" | "layout";
}) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [focusIdx, setFocusIdx] = useState(-1);

  const close = useCallback((returnFocus = false) => {
    setOpen(false);
    setSearch("");
    setFocusIdx(-1);
    if (returnFocus) triggerRef.current?.focus();
  }, [triggerRef]);

  const openPanel = useCallback(() => {
    if (disabled) return;
    setOpen(true);
    if (focusOnOpen === "raf") requestAnimationFrame(() => searchRef.current?.focus());
  }, [disabled, focusOnOpen, searchRef]);

  useEffect(() => {
    if (open && focusOnOpen === "layout") searchRef.current?.focus();
  }, [focusOnOpen, open, searchRef]);

  return { open, search, setSearch, focusIdx, setFocusIdx, close, openPanel };
}
