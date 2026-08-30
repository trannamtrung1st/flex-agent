export const MM_SS_PATTERN = /^\d{1,3}:\d{2}$/;

export const MM_SS_PLACEHOLDER = "60:00";
export const MM_SS_WARNING_PLACEHOLDER = "10:00";
export const MM_SS_EXTENSION_PLACEHOLDER = "15:00";
export const MM_SS_HINT = `MM:SS · e.g. ${MM_SS_PLACEHOLDER}`;

export const CAMPAIGN_TITLE_PLACEHOLDER = "Enter a campaign title";
export const CALLSIGN_PLACEHOLDER = "BERTH-04";
export const COOLDOWN_PLACEHOLDER = "24H";
export const MAX_ATTEMPTS_PLACEHOLDER = "3";
export const SCORE_PLACEHOLDER = "0";
export const BOUNDED_REASON_PLACEHOLDER = "Why this evaluation cannot proceed";
export const ADJUSTED_RATIONALE_PLACEHOLDER = "Why the adjusted score is warranted";
export const DIRECT_TEXT_PLACEHOLDER = "Write or paste the submission text";
export const ACCOMMODATION_VALUE_PLACEHOLDER = MM_SS_EXTENSION_PLACEHOLDER;
export const COMPOSER_PLACEHOLDER = "Compose reply — Attempt 1, Session 07";

export function mmSsError(label: string, example: string = MM_SS_PLACEHOLDER) {
  return `${label} must read minutes:seconds — enter a value like ${example}.`;
}
