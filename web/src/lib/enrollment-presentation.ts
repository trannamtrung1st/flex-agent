import type { StateIndicatorVariant } from "../design-system";

export const ELIGIBILITY_COPY: Record<string, string> = {
  too_early: "Too early",
  open: "Open",
  submission_closed: "Submission closed",
  attempt_start_closed: "Attempt start closed",
  unavailable: "Unavailable",
};

export const ACCOMMODATION_CONSEQUENCE_COPY: Record<string, string> = {
  none: "None",
  deadline_replacement: "Deadline replacement",
  attempt_start_replacement: "Attempt start replacement",
  duration_replacement: "Duration replacement",
  multiple_replacements: "Multiple replacements",
};

export const ACCOMMODATION_DIMENSION_COPY: Record<string, string> = {
  submission_deadline_utc: "Submission deadline",
  attempt_start_not_before_utc: "Earliest Attempt start",
  attempt_start_before_utc: "Latest Attempt start",
  per_attempt_duration_seconds: "Per-Attempt duration",
};

export const ACCOMMODATION_STATUS_COPY: Record<string, string> = {
  pending_approval: "Approval required",
  granted: "Granted",
  rejected: "Rejected",
  revoked: "Revoked",
  superseded: "Superseded",
};

const ENROLLMENT_STATUS_COPY: Record<string, string> = {
  active: "Active",
  suspended: "Suspended",
  closed: "Closed",
  revoked: "Revoked",
  absent: "Absent",
  none: "Absent",
};

export function wordsFromCode(value: string | undefined, labels: Record<string, string> = {}, fallback = "—"): string {
  if (!value) return fallback;
  if (labels[value]) return labels[value];
  const parts = value.split("_").filter(Boolean);
  if (parts.length === 0) return fallback;
  return parts
    .map((word, index) => {
      const lower = word.toLowerCase();
      if (index === 0) return lower.charAt(0).toUpperCase() + lower.slice(1);
      return lower;
    })
    .join(" ");
}

export function enrollmentStatusCopy(status: string): string {
  return wordsFromCode(status.toLowerCase(), ENROLLMENT_STATUS_COPY, status);
}

export function canonicalUtcInstant(value?: string): string | null {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return date.toISOString().replace(/\.\d{3}Z$/, "Z");
}

export function accommodationValueExample(dimension: string | undefined, currentUtc?: string): string {
  if (dimension === "per_attempt_duration_seconds") return "900";
  return canonicalUtcInstant(currentUtc) ?? "2026-09-30T17:00:00Z";
}

const PICKER_INSTANT = /^(\d{4}-\d{2}-\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?$/;

function isUtcAlias(timeZoneId: string): boolean {
  const normalized = timeZoneId.trim().toUpperCase();
  return normalized === "UTC" || normalized === "GMT" || normalized === "ETC/UTC" || normalized === "ETC/GMT";
}

function formatUtcInstant(ms: number): string | null {
  if (Number.isNaN(ms)) return null;
  return new Date(ms).toISOString().replace(/\.\d{3}Z$/, "Z");
}

function wallPartsToUtcMs(date: string, hour: string, minute: string, second: string): number {
  return Date.UTC(
    Number(date.slice(0, 4)),
    Number(date.slice(5, 7)) - 1,
    Number(date.slice(8, 10)),
    Number(hour),
    Number(minute),
    Number(second),
  );
}

export function utcInstantToPickerValue(utcInstant: string | undefined, timeZoneId: string): string {
  const canonical = canonicalUtcInstant(utcInstant);
  if (!canonical) return "";
  const date = new Date(canonical);
  const zone = isUtcAlias(timeZoneId) ? "UTC" : timeZoneId;
  try {
    const parts = Object.fromEntries(
      new Intl.DateTimeFormat("en-GB", {
        timeZone: zone,
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        hourCycle: "h23",
      }).formatToParts(date).map((part) => [part.type, part.value]),
    );
    return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}:${parts.minute}`;
  } catch {
    return canonical.replace(/Z$/, "").slice(0, 16);
  }
}

export function pickerValueToUtcInstant(value: string, timeZoneId: string): string | null {
  const trimmed = value.trim();
  if (!trimmed) return null;
  const match = PICKER_INSTANT.exec(trimmed);
  if (!match) return canonicalUtcInstant(trimmed);
  const second = match[4] ?? "00";
  if (isUtcAlias(timeZoneId)) {
    return formatUtcInstant(wallPartsToUtcMs(match[1], match[2], match[3], second));
  }
  const wanted = wallPartsToUtcMs(match[1], match[2], match[3], second);
  let guess = wanted;
  for (let i = 0; i < 4; i++) {
    const shown = utcInstantToPickerValue(new Date(guess).toISOString(), timeZoneId);
    const shownMatch = PICKER_INSTANT.exec(shown);
    if (!shownMatch) return null;
    const delta = wanted - wallPartsToUtcMs(shownMatch[1], shownMatch[2], shownMatch[3], shownMatch[4] ?? "00");
    if (delta === 0) break;
    guess += delta;
  }
  return formatUtcInstant(guess);
}

export function accommodationCurrentUtc(
  dimension: string | undefined,
  effective: {
    submission_exclusive_end_utc: string;
    attempt_start_utc: string;
    attempt_start_exclusive_end_utc: string;
    per_attempt_duration_seconds?: number | null;
  },
): string {
  if (dimension === "attempt_start_not_before_utc") return effective.attempt_start_utc;
  if (dimension === "attempt_start_before_utc") return effective.attempt_start_exclusive_end_utc;
  if (dimension === "per_attempt_duration_seconds") {
    return effective.per_attempt_duration_seconds != null ? String(effective.per_attempt_duration_seconds) : "";
  }
  return effective.submission_exclusive_end_utc;
}

export function accommodationCurrentBoundTerm(dimension: string | undefined): string {
  if (dimension === "attempt_start_not_before_utc") return "Current earliest start";
  if (dimension === "attempt_start_before_utc") return "Current latest start";
  if (dimension === "per_attempt_duration_seconds") return "Current duration";
  return "Current exclusive end";
}

export function accommodationValueHint(
  dimension: string | undefined,
  timeZoneId?: string,
  selectedDisplay?: string,
): string {
  if (dimension === "per_attempt_duration_seconds") {
    return "Positive seconds, for example 900.";
  }
  const zone = timeZoneId?.trim() || "UTC";
  if (selectedDisplay) {
    return `${selectedDisplay}. Campaign timezone ${zone}.`;
  }
  return `Campaign timezone ${zone}.`;
}

export function enrollmentRecordVariant(status: string): { variant: StateIndicatorVariant; solid?: boolean } {
  switch (status.toLowerCase()) {
    case "active":
      return { variant: "sealed", solid: true };
    case "suspended":
      return { variant: "dim" };
    default:
      return { variant: "rest" };
  }
}

export { compactRegistryId } from "../design-system/components/datatable/compactRegistryId";
