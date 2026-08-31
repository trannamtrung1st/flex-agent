export const MM_SS_PATTERN = /^\d{1,3}:\d{2}$/;

export const MM_SS_PLACEHOLDER = "60:00";
export const MM_SS_WARNING_PLACEHOLDER = "10:00";
export const MM_SS_EXTENSION_PLACEHOLDER = "15:00";
export const MM_SS_HINT = `MM:SS · e.g. ${MM_SS_PLACEHOLDER}`;

/** Default example for numeric fields (`FieldNumber`). */
export const SCORE_PLACEHOLDER = "0";

export function mmSsError(label: string, example: string = MM_SS_PLACEHOLDER) {
  return `${label} must read minutes:seconds — enter a value like ${example}.`;
}
