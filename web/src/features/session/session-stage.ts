import type { SessionSnapshotV1 } from "../../contracts/v1";

/** P0 live Session has Examination then Complete. Do not invent Lab's 5-stage demo. */
export const LIVE_SESSION_STAGE_TOTAL = 2;

export function liveSessionStage(snapshot: SessionSnapshotV1 | null): { stage: number; total: number } {
  const lifecycle = snapshot?.lifecycle_state;
  if (
    lifecycle === "completed"
    || lifecycle === "terminated"
    || lifecycle === "aborted"
    || lifecycle === "completing"
    || sessionAtTimeBoundary(snapshot)
  ) {
    return { stage: LIVE_SESSION_STAGE_TOTAL, total: LIVE_SESSION_STAGE_TOTAL };
  }
  return { stage: 1, total: LIVE_SESSION_STAGE_TOTAL };
}

/** Authoritative remaining budget is gone; live send must not continue. */
export function sessionAtTimeBoundary(snapshot: SessionSnapshotV1 | null): boolean {
  if (!snapshot || snapshot.timing?.policy === "disabled" || snapshot.timing?.policy === "unavailable") {
    return false;
  }
  if (snapshot.lifecycle_state !== "active" && snapshot.lifecycle_state !== "paused") {
    return false;
  }
  return snapshot.timing?.remaining_seconds === 0;
}
