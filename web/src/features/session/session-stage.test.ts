import { liveSessionStage } from "./session-stage";
import type { SessionSnapshotV1 } from "../../contracts/v1";

const snapshot = {
  schema_version: "v1",
  projection_kind: "participant",
  session_id: "55555555-5555-4555-8555-555555555555",
  lifecycle_state: "active",
  session_version: 1,
  last_confirmed_sequence: "1",
  authoritative_observed_at: "2026-09-03T00:00:00Z",
  permitted_actions: ["send_message"],
  recovery_category: "none",
} as SessionSnapshotV1;

describe("liveSessionStage", () => {
  it("uses two bars and keeps Examination current while the Session is live", () => {
    expect(liveSessionStage(snapshot)).toEqual({ stage: 1, total: 2 });
  });

  it("fills both bars when the Session is sealing or terminal", () => {
    expect(liveSessionStage({ ...snapshot, lifecycle_state: "completing" })).toEqual({ stage: 2, total: 2 });
    expect(liveSessionStage({ ...snapshot, lifecycle_state: "completed" })).toEqual({ stage: 2, total: 2 });
  });
});
