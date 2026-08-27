export const MM_SS_PATTERN = /^\d{1,3}:\d{2}$/;

export const MM_SS_HINT = "MM:SS · e.g. 60:00";

export function mmSsError(label: string, example: string) {
  return `${label} must read minutes:seconds — enter a value like ${example}.`;
}
