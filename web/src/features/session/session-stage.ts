import type { SessionSnapshotV1 } from "../../contracts/v1";

/** P0 live Session has Examination then Complete. Do not invent Lab's 5-stage demo. */
export const LIVE_SESSION_STAGE_TOTAL = 2;

export function liveSessionStage(snapshot: SessionSnapshotV1 | null): { stage: number; total: number } {
  const lifecycle = snapshot?.lifecycle_state;
  if (lifecycle === "completed" || lifecycle === "terminated" || lifecycle === "aborted" || lifecycle === "completing") {
    return { stage: LIVE_SESSION_STAGE_TOTAL, total: LIVE_SESSION_STAGE_TOTAL };
  }
  return { stage: 1, total: LIVE_SESSION_STAGE_TOTAL };
}
