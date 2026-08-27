import { useSearchParams } from "react-router";

export function useDemoParam<T extends string>(allowed: readonly T[], fallback: T) {
  const [params, setParams] = useSearchParams();
  const raw = params.get("demo");
  const value = (allowed as readonly string[]).includes(raw ?? "") ? (raw as T) : fallback;

  const setValue = (next: T) => {
    const copy = new URLSearchParams(params);
    copy.set("demo", next);
    setParams(copy, { replace: true });
  };

  return [value, setValue] as const;
}

export function useStateParam<T extends string>(allowed: readonly T[], fallback: T | null = null) {
  const [params] = useSearchParams();
  const raw = params.get("state");
  if (raw && (allowed as readonly string[]).includes(raw)) return raw as T;
  return fallback;
}
