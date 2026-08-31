import { useEffect, useRef, type KeyboardEvent, type ReactNode } from "react";
import { Key } from "../keys/Key";

/** Hull chrome and generic layout hosts. Domain surfaces sit inside these. */
export const BULKHEAD_INERT_SELECTOR =
  ".command-strip, .console-foot, .layout-management__shell, .composition-split";

function bulkheadFocusable(root: HTMLElement) {
  return [
    ...root.querySelectorAll<HTMLElement>(
      "a[href], button:not([disabled]), textarea, input, select, [tabindex]:not([tabindex='-1'])",
    ),
  ].filter(
    (el) =>
      el.tabIndex >= 0
      && !el.closest("[hidden]")
      && el.getAttribute("aria-hidden") !== "true",
  );
}

export function Bulkhead({
  id,
  open,
  onClose,
  side = "leading",
  wide,
  title,
  titleId,
  children,
  footer,
}: {
  id?: string;
  open: boolean;
  onClose: () => void;
  side?: "leading" | "trailing";
  wide?: boolean;
  title: string;
  titleId: string;
  children: ReactNode;
  footer?: ReactNode;
}) {
  const panelRef = useRef<HTMLElement>(null);
  const returnFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (!open) return;
    returnFocusRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    document.body.classList.add("is-bulkhead-open");
    const inert = [
      ...document.querySelectorAll<HTMLElement>(
        BULKHEAD_INERT_SELECTOR,
      ),
    ];
    inert.forEach((el) => el.setAttribute("inert", ""));

    const onDocumentKeyDown = (e: globalThis.KeyboardEvent) => {
      if (e.key !== "Escape") return;
      e.preventDefault();
      onClose();
    };
    document.addEventListener("keydown", onDocumentKeyDown);

    const raf = requestAnimationFrame(() => {
      const root = panelRef.current?.closest(".bulkhead") as HTMLElement | null;
      if (!root) return;
      const first = bulkheadFocusable(root)[0];
      first?.focus();
    });

    return () => {
      cancelAnimationFrame(raf);
      document.removeEventListener("keydown", onDocumentKeyDown);
      document.body.classList.remove("is-bulkhead-open");
      inert.forEach((el) => el.removeAttribute("inert"));
      returnFocusRef.current?.focus();
      returnFocusRef.current = null;
    };
  }, [open, onClose]);

  const onKeyDown = (e: KeyboardEvent) => {
    if (!open) return;
    if (e.key === "Escape") {
      e.preventDefault();
      onClose();
      return;
    }
    if (e.key !== "Tab") return;
    const root = panelRef.current?.closest(".bulkhead") as HTMLElement | null;
    if (!root) return;
    const items = bulkheadFocusable(root);
    if (!items.length) return;
    const first = items[0];
    const last = items[items.length - 1];
    if (e.shiftKey && e.target === first) {
      e.preventDefault();
      last.focus();
    } else if (!e.shiftKey && e.target === last) {
      e.preventDefault();
      first.focus();
    }
  };

  return (
    <div
      id={id}
      className={`bulkhead bulkhead--${side}${wide ? " bulkhead--wide" : ""}${open ? " is-open" : ""}`}
      hidden={!open}
      aria-hidden={!open}
      onKeyDown={onKeyDown}
    >
      <button
        type="button"
        className="bulkhead-scrim"
        tabIndex={-1}
        aria-label={`Close ${title}`}
        onClick={onClose}
      />
      <aside
        ref={panelRef}
        className="bulkhead-panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
      >
        <header className="bulkhead-head">
          <span className="bulkhead-title" id={titleId}>
            {title}
          </span>
          <Key variant="quiet" size="compact" onClick={onClose}>
            Close
          </Key>
        </header>
        <div className="bulkhead-body">{children}</div>
        {footer ? <footer className="bulkhead-foot">{footer}</footer> : null}
      </aside>
    </div>
  );
}
