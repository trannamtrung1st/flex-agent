export type NumberStepDirection = 1 | -1;

export type NumberStepOptions = {
  min?: number;
  max?: number;
  step?: number;
};

export function toFiniteNumber(value: unknown): number | undefined {
  if (value == null || value === "") return undefined;
  const n = Number(value);
  return Number.isFinite(n) ? n : undefined;
}

export function parseNumberFieldValue(raw: string): number | null {
  const trimmed = raw.trim();
  if (trimmed === "") return null;
  const n = Number(trimmed);
  return Number.isFinite(n) ? n : null;
}

export function decimalPlacesFromStep(step: number): number {
  if (!Number.isFinite(step) || step <= 0) return 0;
  const text = String(step);
  const dot = text.indexOf(".");
  if (dot === -1) return 0;
  return text.length - dot - 1;
}

export function stepNumberFieldValue(
  raw: string,
  direction: NumberStepDirection,
  options: NumberStepOptions = {},
): string {
  const step = options.step && options.step > 0 ? options.step : 1;
  const places = decimalPlacesFromStep(step);
  const current = parseNumberFieldValue(raw);
  let next = (current ?? 0) + direction * step;
  next = Number((Math.round(next / step) * step).toFixed(places));
  if (options.min != null) next = Math.max(options.min, next);
  if (options.max != null) next = Math.min(options.max, next);
  return places > 0 ? next.toFixed(places) : String(next);
}
