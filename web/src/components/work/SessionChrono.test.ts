import { projectedRemainingSeconds } from "./SessionChrono";
import type { SessionSnapshotV1 } from "../../contracts/v1";

const snapshot: SessionSnapshotV1 = {
  schema_version: "v1",
  projection_kind: "participant",
  session_id: "55555555-5555-4555-8555-555555555555",
  lifecycle_state: "active",
  session_version: 1,
  last_confirmed_sequence: "1",
  authoritative_observed_at: "2026-09-03T00:00:00Z",
  permitted_actions: ["send_message"],
  recovery_category: "none",
  timing: {
    policy: "active_duration",
    remaining_seconds: 2400,
    warning_code: "none",
    budget_seconds: 2700,
  },
};

describe("projectedRemainingSeconds", () => {
  it("subtracts elapsed wall time after the last confirmed observation", () => {
    const later = Date.parse("2026-09-03T00:00:10Z");
    expect(projectedRemainingSeconds(snapshot, later)).toBe(2390);
  });

  it("holds remaining time while paused", () => {
    expect(projectedRemainingSeconds(
      { ...snapshot, lifecycle_state: "paused" },
      Date.parse("2026-09-03T00:10:00Z"),
    )).toBe(2400);
  });

  it("does not invent remaining time when authoritative timing is unavailable", () => {
    expect(projectedRemainingSeconds(
      {
        ...snapshot,
        timing: { policy: "unavailable", remaining_seconds: null, warning_code: "none" },
      },
      Date.parse("2026-09-03T00:10:00Z"),
    )).toBeNull();
  });

  it("reports zero remaining once the Session is sealing or terminal", () => {
    expect(projectedRemainingSeconds(
      { ...snapshot, lifecycle_state: "completing" },
      Date.parse("2026-09-03T00:10:00Z"),
    )).toBe(0);
    expect(projectedRemainingSeconds(
      { ...snapshot, lifecycle_state: "completed" },
      Date.parse("2026-09-03T00:10:00Z"),
    )).toBe(0);
  });
});
