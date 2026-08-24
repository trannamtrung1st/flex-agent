import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import {
  createEnrollmentIdempotencyKey,
  createProductionEnrollmentClient,
  enrollmentFailureCopy,
  enrollmentOutcomeCopy,
  type EnrollmentDetailV1,
  type EnrollmentTimingV2,
} from "../api/production-enrollment";
import { Dialog } from "../components/ui/Dialog";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";
import { formatCampaignInstant } from "../lib/campaign-timezone";

function Instant({ value, timeZoneId }: { value: string | null; timeZoneId: string }) {
  if (!value) {
    return <span>Not set</span>;
  }

  const formatted = formatCampaignInstant(value, timeZoneId);
  if (!formatted.conversionAvailable) {
    return (
      <span>
        {formatted.exactUtc} ({formatted.zoneLabel}; local conversion unavailable)
      </span>
    );
  }

  return (
    <span>
      {formatted.exactUtc} ({formatted.zoneLabel}; {formatted.localDisplay})
    </span>
  );
}

export function ProductionEnrollmentDetailPage() {
  const { activityId = "", cohortId = "", enrollmentId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const [detail, setDetail] = useState<EnrollmentDetailV1 | null>(null);
  const [timing, setTiming] = useState<EnrollmentTimingV2 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [dimension, setDimension] = useState("submission_deadline_utc");
  const [requestedValue, setRequestedValue] = useState("");
  const [reasonCategory, setReasonCategory] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const [fairness, setFairness] = useState(false);
  const [confirmGrant, setConfirmGrant] = useState(false);
  const [pending, setPending] = useState(false);

  const load = () => Promise.all([
    client.getEnrollment(activityId, cohortId, enrollmentId),
    client.getEnrollmentTiming(activityId, cohortId, enrollmentId),
  ]).then(([enrollmentDetail, enrollmentTiming]) => {
    setDetail(enrollmentDetail);
    setTiming(enrollmentTiming);
    setReasonCategory((current) => current || enrollmentTiming.permitted_reason_categories[0] || "");
    setDimension((current) =>
      enrollmentTiming.permitted_dimensions.includes(current)
        ? current
        : enrollmentTiming.permitted_dimensions[0] || "submission_deadline_utc");
    setError(null);
  });

  useEffect(() => {
    let cancelled = false;
    load()
      .catch(() => {
        if (!cancelled) {
          setError("This Enrollment is not available.");
        }
      });
    return () => {
      cancelled = true;
    };
  }, [activityId, client, cohortId, enrollmentId]);

  if (error) {
    return (
      <StatusPanel title="Enrollment unavailable" variant="danger">
        <p>{error}</p>
        <p><Link to={`/activities/${activityId}/cohorts/${cohortId}/participants`}>Return to Assign Participants</Link></p>
      </StatusPanel>
    );
  }

  if (detail === null || timing === null) {
    return <ProtectedLoading label="Loading Enrollment…" />;
  }

  const zone = timing.effective.time_zone_id || timing.baseline.time_zone_id;
  const actions = new Set(timing.enrollment.permitted_actions);
  const pendingItem = timing.history.find((item) => item.status === "pending_approval");

  return (
    <div>
      <header className="page-header">
        <h1>{detail.enrollment.display_label}</h1>
        <p>Status {detail.enrollment.status}. Revision {timing.enrollment.revision}.</p>
        {status ? <p role="status">{status}</p> : null}
      </header>

      <section className="page-section">
        <h2>Baseline timing</h2>
        <p>Campaign timezone {timing.baseline.time_zone_id}.</p>
        <p>Starts <Instant value={timing.baseline.starts_at_utc} timeZoneId={zone} /></p>
        <p>Attempt-start ends <Instant value={timing.baseline.ends_at_utc} timeZoneId={zone} /></p>
        <p>Submission deadline <Instant value={timing.baseline.deadline_utc} timeZoneId={zone} /></p>
      </section>

      <section className="page-section">
        <h2>Effective timing</h2>
        <p>Eligibility {timing.effective.eligibility_state}{timing.effective.is_authoritative ? "" : " (not authoritative)"}.</p>
        <p>Submission window <Instant value={timing.effective.submission_starts_at_utc} timeZoneId={zone} /> until <Instant value={timing.effective.submission_exclusive_end_utc} timeZoneId={zone} /></p>
        <p>Attempt-start window <Instant value={timing.effective.attempt_start_utc} timeZoneId={zone} /> until <Instant value={timing.effective.attempt_start_exclusive_end_utc} timeZoneId={zone} /></p>
      </section>

      {actions.has("request_accommodation") ? (
        <section className="page-section">
          <h2>Request accommodation</h2>
          <label>
            Dimension
            <select value={dimension} onChange={(event) => setDimension(event.target.value)}>
              {timing.permitted_dimensions.map((item) => (
                <option key={item} value={item}>{item}</option>
              ))}
            </select>
          </label>
          <label>
            Requested value (UTC)
            <input value={requestedValue} onChange={(event) => setRequestedValue(event.target.value)} placeholder="2026-09-30T17:00:00Z" />
          </label>
          <label>
            Reason
            <select value={reasonCategory} onChange={(event) => setReasonCategory(event.target.value)}>
              {timing.permitted_reason_categories.map((item) => (
                <option key={item} value={item}>{item}</option>
              ))}
            </select>
          </label>
          <label>
            Expires at UTC
            <input value={expiresAt} onChange={(event) => setExpiresAt(event.target.value)} />
          </label>
          <label>
            <input type="checkbox" checked={fairness} onChange={(event) => setFairness(event.target.checked)} />
            Fairness exception (requires a different approver)
          </label>
          <button type="button" onClick={() => setConfirmGrant(true)} disabled={pending || requestedValue.length === 0}>
            Request accommodation
          </button>
        </section>
      ) : null}

      {pendingItem && actions.has("approve_fairness_exception") ? (
        <section className="page-section">
          <h2>Fairness exception decision</h2>
          <p>{pendingItem.dimension} → {pendingItem.normalized_value}</p>
          <button
            type="button"
            disabled={pending}
            onClick={() => {
              setPending(true);
              client.decideAccommodation(activityId, cohortId, enrollmentId, pendingItem.accommodation_id, true, pendingItem.revision, createEnrollmentIdempotencyKey())
                .then((outcome) => {
                  setStatus(enrollmentOutcomeCopy(outcome.outcome_code, outcome.succeeded ? "Exception approved." : "Decision was not recorded."));
                  return load();
                })
                .catch((caught: unknown) => setError(enrollmentFailureCopy(caught, "Decision is not available.")))
                .finally(() => setPending(false));
            }}
          >
            Approve exception
          </button>
          <button
            type="button"
            disabled={pending}
            onClick={() => {
              setPending(true);
              client.decideAccommodation(activityId, cohortId, enrollmentId, pendingItem.accommodation_id, false, pendingItem.revision, createEnrollmentIdempotencyKey())
                .then((outcome) => {
                  setStatus(enrollmentOutcomeCopy(outcome.outcome_code, outcome.succeeded ? "Exception rejected." : "Decision was not recorded."));
                  return load();
                })
                .catch((caught: unknown) => setError(enrollmentFailureCopy(caught, "Decision is not available.")))
                .finally(() => setPending(false));
            }}
          >
            Reject exception
          </button>
        </section>
      ) : null}

      {timing.current_accommodation_id && actions.has("revoke_accommodation") ? (
        <p>
          <button
            type="button"
            disabled={pending}
            onClick={() => {
              setPending(true);
              client.revokeAccommodation(
                activityId,
                cohortId,
                enrollmentId,
                timing.current_accommodation_id!,
                timing.history.find((item) => item.accommodation_id === timing.current_accommodation_id)?.revision ?? timing.enrollment.revision,
                createEnrollmentIdempotencyKey(),
              )
                .then((outcome) => {
                  setStatus(enrollmentOutcomeCopy(outcome.outcome_code, outcome.succeeded ? "Accommodation revoked." : "Revocation was not recorded."));
                  return load();
                })
                .catch((caught: unknown) => setError(enrollmentFailureCopy(caught, "Revocation is not available.")))
                .finally(() => setPending(false));
            }}
          >
            Revoke current accommodation
          </button>
        </p>
      ) : null}

      <section className="page-section">
        <h2>Accommodation history</h2>
        <ol>
          {timing.history.map((item) => (
            <li key={item.accommodation_id}>
              {item.status}: {item.dimension} {item.normalized_value}
            </li>
          ))}
        </ol>
      </section>

      <section className="page-section">
        <h2>Enrollment history</h2>
        <ol>
          {detail.history.map((item) => (
            <li key={item.sequence}>
              {item.prior_status} to {item.new_status} ({item.reason_code})
            </li>
          ))}
        </ol>
      </section>
      <p>
        <Link to={`/activities/${activityId}/cohorts/${cohortId}/participants`}>Return to Assign Participants</Link>
      </p>

      <Dialog
        open={confirmGrant}
        title="Confirm accommodation"
        confirmLabel="Submit request"
        onCancel={() => setConfirmGrant(false)}
        isConfirming={pending}
        onConfirm={() => {
          setPending(true);
          client.grantAccommodation(activityId, cohortId, enrollmentId, {
            dimension,
            requested_value: requestedValue,
            reason_category: reasonCategory,
            expires_at_utc: expiresAt.length === 0 ? null : expiresAt,
            fairness_exception: fairness,
            expected_revision: timing.enrollment.revision,
            idempotency_key: createEnrollmentIdempotencyKey(),
          })
            .then((outcome) => {
              setConfirmGrant(false);
              setStatus(enrollmentOutcomeCopy(outcome.outcome_code, outcome.succeeded ? "Accommodation recorded." : "Accommodation was not recorded."));
              return load();
            })
            .catch((caught: unknown) => setError(enrollmentFailureCopy(caught, "Accommodation is not available.")))
            .finally(() => setPending(false));
        }}
      >
        <p>Baseline deadline: {timing.baseline.deadline_utc}.</p>
        <p>Requested {dimension}: {requestedValue}.</p>
        <p>{fairness ? "A different authorized approver must decide before this changes timing." : "This request stays inside the current policy bounds."}</p>
        <p>Prior accommodation history is preserved.</p>
      </Dialog>
    </div>
  );
}
