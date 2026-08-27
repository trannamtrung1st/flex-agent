import { useCallback, useState } from "react";
import { cx } from "../../lib/cx";

export type ToastNotice = {
  id: string;
  label: string;
  copy: string;
  attention?: boolean;
  leaving?: boolean;
};

export function ToastDock({ toasts }: { toasts: ToastNotice[] }) {
  return (
    <div className="toast-dock" aria-live="polite">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={cx("toast", toast.attention && "toast--attention", toast.leaving && "is-leaving")}
          role="status"
        >
          <p className="toast-copy">
            <span className="toast-label">{toast.label}</span>
            {toast.copy}
          </p>
        </div>
      ))}
    </div>
  );
}

export function useToasts(lingerMs = 4200) {
  const [toasts, setToasts] = useState<ToastNotice[]>([]);

  const pushToast = useCallback((notice: Omit<ToastNotice, "id" | "leaving">) => {
    const id = `toast-${Date.now()}-${Math.random().toString(16).slice(2)}`;
    setToasts((prev) => [...prev, { ...notice, id }]);
    window.setTimeout(() => {
      const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
      setToasts((prev) => prev.map((toast) => (toast.id === id ? { ...toast, leaving: true } : toast)));
      window.setTimeout(() => {
        setToasts((prev) => prev.filter((toast) => toast.id !== id));
      }, reduce ? 0 : 240);
    }, lingerMs);
  }, [lingerMs]);

  return { toasts, pushToast };
}
