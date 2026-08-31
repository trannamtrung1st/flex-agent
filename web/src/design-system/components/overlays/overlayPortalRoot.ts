/** Modal dialogs occupy the top layer; in-app overlays stay in `#root` so hull chrome can stack above them. */
export function overlayPortalRoot(host: HTMLElement | null): HTMLElement {
  const dialog = host?.closest("dialog");
  if (dialog instanceof HTMLDialogElement) return dialog;
  const root = host?.closest("#root");
  if (root instanceof HTMLElement) return root;
  return document.getElementById("root") ?? document.body;
}
