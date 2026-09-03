import { useEffect, useMemo, useState } from "react";
import { arcPath, formatClock, polar } from "../../lib/format";
import type { SessionSnapshotV1 } from "../../contracts/v1";
import { Key } from "../../design-system";

const SWEEP = 300;

export function projectedRemainingSeconds(snapshot: SessionSnapshotV1 | null, nowMs: number): number | null {
  const timing = snapshot?.timing;
  if (!timing || timing.policy === "disabled" || timing.remaining_seconds == null) {
    return null;
  }
  const sealing = snapshot?.lifecycle_state === "completing";
  const terminal = snapshot?.lifecycle_state === "completed"
    || snapshot?.lifecycle_state === "terminated"
    || snapshot?.lifecycle_state === "aborted";
  if (sealing || terminal) {
    return 0;
  }
  if (snapshot?.lifecycle_state === "paused") {
    return Math.max(0, timing.remaining_seconds);
  }
  const observed = Date.parse(snapshot?.authoritative_observed_at ?? "");
  if (!Number.isFinite(observed)) {
    return Math.max(0, timing.remaining_seconds);
  }
  const drift = Math.max(0, Math.floor((nowMs - observed) / 1000));
  return Math.max(0, timing.remaining_seconds - drift);
}

export function SessionChrono({
  snapshot,
  onSubmit,
  canSubmit,
}: {
  snapshot: SessionSnapshotV1 | null;
  onSubmit: () => void;
  canSubmit: boolean;
}) {
  const [nowMs, setNowMs] = useState(() => Date.now());
  const remaining = projectedRemainingSeconds(snapshot, nowMs);
  const budget = snapshot?.timing?.budget_seconds ?? remaining ?? 0;
  const paused = snapshot?.lifecycle_state === "paused";
  const sealing = snapshot?.lifecycle_state === "completing";
  const terminal = snapshot?.lifecycle_state === "completed"
    || snapshot?.lifecycle_state === "terminated"
    || snapshot?.lifecycle_state === "aborted";
  const shouldTick = remaining != null && !paused && !sealing && !terminal;

  useEffect(() => {
    setNowMs(Date.now());
  }, [snapshot?.authoritative_observed_at, snapshot?.timing?.remaining_seconds]);

  useEffect(() => {
    if (!shouldTick) {
      return;
    }
    const id = window.setInterval(() => setNowMs(Date.now()), 1000);
    return () => window.clearInterval(id);
  }, [shouldTick]);

  const deg = remaining == null || budget <= 0
    ? 0
    : Math.max(0, Math.min(SWEEP, (remaining / budget) * SWEEP));
  const [nx, ny] = polar(48, 48, 40, deg);
  const ticks = useMemo(() => {
    if (budget <= 0) {
      return [];
    }
    const minutes = Math.max(1, Math.round(budget / 60));
    const items = [];
    for (let m = 0; m <= minutes; m += 5) {
      const d = (m / minutes) * SWEEP;
      const [x1, y1] = polar(48, 48, 40, d);
      const [x2, y2] = polar(48, 48, m % 15 === 0 ? 32 : 35.5, d);
      items.push({ x1, y1, x2, y2, m });
    }
    return items;
  }, [budget]);

  return (
    <section className="chrono" aria-label="Session timing">
      <div className="chrono-main">
        <div className="chrono-digits-block">
          <h2 className="chrono-label">Time remaining</h2>
          <p className="chrono-digits" role="timer" aria-live="off">
            {remaining == null ? "—" : formatClock(remaining)}
          </p>
        </div>
        {remaining == null ? null : (
          <svg className="chrono-gauge" viewBox="0 0 96 96" aria-hidden="true">
            <path className="gauge-track" d={arcPath(48, 48, 40, 0, SWEEP)} />
            <path className="gauge-fill" d={deg > 0.5 ? arcPath(48, 48, 40, 0, deg) : ""} />
            <g className="gauge-ticks">
              {ticks.map((tick) => (
                <line key={tick.m} x1={tick.x1} y1={tick.y1} x2={tick.x2} y2={tick.y2} />
              ))}
            </g>
            <circle className="gauge-needle" cx={nx} cy={ny} r="3.4" />
          </svg>
        )}
        <details className="chrono-details">
          <summary>Time details</summary>
          <p>
            {paused
              ? "Paused. Remaining active time is held on the server."
              : "Display aid from the last confirmed server remaining time. Not a client clock."}
          </p>
          <p>Policy: {snapshot?.timing?.policy ?? "disabled"}</p>
          {snapshot?.timing?.budget_seconds != null ? (
            <p>Active-duration budget: {formatClock(snapshot.timing.budget_seconds)}</p>
          ) : null}
          {snapshot?.bound_submission?.summary ? (
            <p>{snapshot.bound_submission.summary}</p>
          ) : null}
        </details>
      </div>
      <div className="chrono-stage">
        <p className="stage-line">
          Stage — <span>{terminal ? "Complete" : sealing ? "Sealing" : "Examination"}</span>
        </p>
        {canSubmit ? (
          <Key disabled={terminal} onClick={onSubmit}>
            Submit Session
          </Key>
        ) : null}
      </div>
    </section>
  );
}
