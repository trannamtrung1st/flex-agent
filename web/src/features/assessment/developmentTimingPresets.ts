export const DEVELOPMENT_SYNTHETIC_TIMED_ACTIVITY_PRESET_V1 =
  "development.synthetic_timed.v1" as const;

export const DEVELOPMENT_SYNTHETIC_TIMED_ACTIVITY_TIMING = {
  starts_at_utc: "2026-09-01T00:00:00.000Z",
  ends_at_utc: "2026-09-30T23:59:00.000Z",
  deadline_utc: "2026-09-30T17:00:00.000Z",
  time_zone_id: "UTC",
  attempt_limit: 2,
  per_attempt_duration_seconds: 3600,
  warning_approaching_remaining_seconds: 900,
  warning_imminent_remaining_seconds: 300,
} as const;

export function developmentSyntheticTimedActivityCreateFields() {
  return {
    timing_preset_id: DEVELOPMENT_SYNTHETIC_TIMED_ACTIVITY_PRESET_V1,
    ...DEVELOPMENT_SYNTHETIC_TIMED_ACTIVITY_TIMING,
  };
}
