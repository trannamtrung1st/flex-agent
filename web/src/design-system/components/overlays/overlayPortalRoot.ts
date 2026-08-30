/** Modal dialogs occupy the top layer; body-portaled overlays paint behind them. */
export function overlayPortalRoot(host: HTMLElement | null): HTMLElement {
  const dialog = host?.closest("dialog");
  if (dialog instanceof HTMLDialogElement) return dialog;
  return document.body;
}
