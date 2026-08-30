import {
  enrollmentRecordVariant,
  enrollmentStatusCopy,
  wordsFromCode,
  canonicalUtcInstant,
  accommodationValueExample,
  accommodationValueHint,
  accommodationCurrentUtc,
  pickerValueToUtcInstant,
  utcInstantToPickerValue,
} from "./enrollment-presentation";

describe("enrollment copy", () => {
  it("names eligibility and lifecycle codes for operators", () => {
    expect(wordsFromCode("too_early", { too_early: "Too early" })).toBe("Too early");
    expect(enrollmentStatusCopy("active")).toBe("Active");
    expect(wordsFromCode("restriction_removed")).toBe("Restriction removed");
    expect(canonicalUtcInstant("2026-09-01T12:00:00Z")).toBe("2026-09-01T12:00:00Z");
    expect(accommodationValueExample("submission_deadline_utc", "2026-09-01T12:00:00Z")).toBe("2026-09-01T12:00:00Z");
    expect(accommodationValueExample("per_attempt_duration_seconds")).toBe("900");
    expect(accommodationValueHint("submission_deadline_utc", "UTC")).toMatch(/Campaign timezone UTC/);
    expect(accommodationValueHint("per_attempt_duration_seconds")).toBe("Positive seconds, for example 900.");
  });
});

describe("accommodation current bound", () => {
  const effective = {
    submission_exclusive_end_utc: "2026-09-01T12:00:00Z",
    attempt_start_utc: "2026-08-15T09:00:00Z",
    attempt_start_exclusive_end_utc: "2026-08-20T09:00:00Z",
    per_attempt_duration_seconds: 900,
  };

  it("seeds the picker from the permitted dimension's effective instant", () => {
    expect(accommodationCurrentUtc("submission_deadline_utc", effective)).toBe("2026-09-01T12:00:00Z");
    expect(accommodationCurrentUtc("attempt_start_not_before_utc", effective)).toBe("2026-08-15T09:00:00Z");
    expect(accommodationCurrentUtc("attempt_start_before_utc", effective)).toBe("2026-08-20T09:00:00Z");
    expect(accommodationCurrentUtc("per_attempt_duration_seconds", effective)).toBe("900");
  });
});

describe("accommodation picker conversion", () => {
  it("round-trips a UTC exclusive end through the datetime picker value", () => {
    expect(utcInstantToPickerValue("2026-09-01T12:00:00Z", "UTC")).toBe("2026-09-01T12:00");
    expect(pickerValueToUtcInstant("2026-09-01T12:00", "UTC")).toBe("2026-09-01T12:00:00Z");
  });

  it("edits named campaign timezones as local wall time and submits UTC", () => {
    expect(utcInstantToPickerValue("2026-09-30T17:00:00Z", "America/New_York")).toBe("2026-09-30T13:00");
    expect(pickerValueToUtcInstant("2026-09-30T13:00", "America/New_York")).toBe("2026-09-30T17:00:00Z");
  });
});

describe("enrollmentRecordVariant", () => {
  it("marks active enrollments as sealed", () => {
    expect(enrollmentRecordVariant("active")).toEqual({ variant: "sealed", solid: true });
    expect(enrollmentRecordVariant("ACTIVE")).toEqual({ variant: "sealed", solid: true });
  });

  it("marks suspended enrollments as dim", () => {
    expect(enrollmentRecordVariant("suspended")).toEqual({ variant: "dim" });
  });

  it("uses rest for other lifecycle states", () => {
    expect(enrollmentRecordVariant("closed")).toEqual({ variant: "rest" });
    expect(enrollmentRecordVariant("revoked")).toEqual({ variant: "rest" });
  });
});
