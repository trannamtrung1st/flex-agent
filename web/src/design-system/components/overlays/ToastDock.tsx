import { createContext, useCallback, useContext, useState, type CSSProperties, type ReactNode } from "react";
import { cx } from "../../../lib/cx";

export type ToastNotice = {
  id: string;
  label: string;
  copy: string;
  attention?: boolean;
  leaving?: boolean;
};

export type ToastDockPlacement =
  | "bottom-center"
  | "bottom-start"
  | "bottom-end"
  | "top-center"
  | "top-start"
  | "top-end";

export type ToastDockProps = {
  toasts: ToastNotice[];
  placement?: ToastDockPlacement;
  /** Extra inset from the inline edge (gangway, rail). CSS length. */
  offsetInline?: string;
  /** Extra inset from the block edge (hull or action foot). CSS length. */
  offsetBlock?: string;
  className?: string;
};

export function ToastDock({
  toasts,
  placement = "top-center",
  offsetInline,
  offsetBlock,
  className,
}: ToastDockProps) {
  const style = {
    ...(offsetInline ? { "--toast-dock-offset-inline": offsetInline } : {}),
    ...(offsetBlock ? { "--toast-dock-offset-block": offsetBlock } : {}),
  } as CSSProperties;

  return (
    <div
      className={cx("toast-dock", className)}
      data-placement={placement}
      aria-live="polite"
      style={style}
    >
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={cx("toast", toast.attention && "toast--attention", toast.leaving && "is-leaving")}
          role="status"
        >
          <p className="toast-copy">
            <span className="toast-label">{toast.label}</span>
            {" "}
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

export type PushToast = (notice: Omit<ToastNotice, "id" | "leaving">) => void;

const ToastPushContext = createContext<PushToast>(() => {});

export type ToastHostProps = {
  children: ReactNode;
  placement?: ToastDockPlacement;
  offsetInline?: string;
  offsetBlock?: string;
};

/** Production shells and lab Admin mount `ToastHost`. Deck specimens may use local `useToasts`. */
export function ToastHost({
  children,
  placement = "top-center",
  offsetInline,
  offsetBlock,
}: ToastHostProps) {
  const { toasts, pushToast } = useToasts();
  return (
    <ToastPushContext.Provider value={pushToast}>
      {children}
      <ToastDock
        toasts={toasts}
        placement={placement}
        offsetInline={offsetInline}
        offsetBlock={offsetBlock}
      />
    </ToastPushContext.Provider>
  );
}

export function usePushToast(): PushToast {
  return useContext(ToastPushContext);
}
