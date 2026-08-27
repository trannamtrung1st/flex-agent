export type TrimmedTextValidationOptions = {
  minLength?: number;
  emptyMessage?: string;
  minLengthMessage?: (minLength: number) => string;
};

export function trimmedTextError(value: string, options: TrimmedTextValidationOptions = {}): string | undefined {
  const minLength = options.minLength ?? 1;
  const emptyMessage = options.emptyMessage ?? "This field is required.";
  const minLengthMessage = options.minLengthMessage ?? ((min) => `Enter at least ${min} characters.`);
  const trimmed = value.trim();

  if (!trimmed) return emptyMessage;
  if (trimmed.length < minLength) return minLengthMessage(minLength);
  return undefined;
}

export const BOUNDED_REASON_MIN = 8;

export function boundedReasonError(value: string): string | undefined {
  return trimmedTextError(value, {
    minLength: BOUNDED_REASON_MIN,
    emptyMessage: "Enter a bounded reason.",
    minLengthMessage: (min) => `Enter at least ${min} characters.`,
  });
}

export function clearValidationErrorOnValid(
  currentError: string,
  nextValue: string,
  validate: (value: string) => string | undefined,
): string {
  return currentError && !validate(nextValue) ? "" : currentError;
}
