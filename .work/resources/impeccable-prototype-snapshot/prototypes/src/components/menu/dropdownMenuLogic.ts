export function enabledMenuItems(root: HTMLElement | null) {
  return Array.from(root?.querySelectorAll<HTMLButtonElement>("[role='menuitem']") ?? []).filter(
    (el) => !el.hidden && !el.disabled && el.getAttribute("aria-disabled") !== "true",
  );
}

export function stepMenuIndex(length: number, current: number, delta: number) {
  if (length <= 0) return -1;
  if (current < 0) return delta > 0 ? 0 : length - 1;
  return (current + delta + length) % length;
}
