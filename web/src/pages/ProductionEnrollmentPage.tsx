import { useEffect, useId, useMemo, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import {
  createEnrollmentIdempotencyKey,
  createProductionEnrollmentClient,
  EnrollmentRateLimitedCopy,
  enrollmentFailureCopy,
  enrollmentOutcomeCopy,
  type EnrollmentSummaryV1,
} from "../api/production-enrollment";
import { Alert } from "../components/ui/Alert";
import { Button } from "../components/ui/Button";
import { Dialog } from "../components/ui/Dialog";
import { ErrorSummary } from "../components/ui/ErrorSummary";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";

const reasonFor = {
  suspend: "temporary_restriction",
  restore: "restriction_removed",
  close: "activity_or_enrollment_end",
  revoke: "access_revoked",
} as const;

export function ProductionEnrollmentPage() {
  const { activityId = "", cohortId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const [candidates, setCandidates] = useState<Array<{ actor_id: string; display_label: string }>>([]);
  const [enrollments, setEnrollments] = useState<EnrollmentSummaryV1[]>([]);
  const [selected, setSelected] = useState("");
  const [pending, setPending] = useState<string | null>(null);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [confirm, setConfirm] = useState<{ enrollment: EnrollmentSummaryV1; operation: keyof typeof reasonFor } | null>(null);
  const assignCommandRef = useRef<{ participantActorId: string; key: string } | null>(null);
  const lifecycleCommandRef = useRef<{
    enrollmentId: string;
    operation: string;
    expectedRevision: number;
    key: string;
  } | null>(null);
  const headingId = useId();

  useEffect(() => {
    let cancelled = false;
    Promise.all([
      client.listCandidates(activityId, cohortId),
      client.listEnrollments(activityId, cohortId),
    ])
      .then(([candidatePage, enrollmentPage]) => {
        if (cancelled) {
          return;
        }
        setCandidates(candidatePage.items);
        setEnrollments(enrollmentPage.items);
        setError(null);
      })
      .catch((caught) => {
        if (!cancelled) {
          setError(enrollmentFailureCopy(caught, "This Enrollment workspace is not available."));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setReady(true);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [activityId, client, cohortId]);

  if (!ready) {
    return <ProtectedLoading label="Loading Enrollment workspace…" />;
  }

  if (error && enrollments.length === 0 && candidates.length === 0) {
    return (
      <StatusPanel
        title={error === EnrollmentRateLimitedCopy ? "Too many requests" : "Enrollment unavailable"}
        variant="danger"
      >
        <p>{error}</p>
        <p><Link to="/activities">Return to Activities</Link></p>
      </StatusPanel>
    );
  }

  return (
    <div>
      <header className="page-header">
        <h1 id={headingId}>Assign Participants</h1>
        <p>Assign one eligible Participant to this activated Cohort. The frozen baseline is not changed.</p>
      </header>
      {error ? <ErrorSummary title="Assignment could not complete" errors={[error]} /> : null}
      {status ? <Alert variant="success" title="Enrollment updated">{status}</Alert> : null}

      <section className="page-section" aria-labelledby="candidate-heading">
        <h2 id="candidate-heading">Eligible participants</h2>
        {candidates.length === 0 ? (
          <p>No eligible Participants are available for this Organization.</p>
        ) : (
          <div>
            <label htmlFor="participant-select">Participant</label>
            <select
              id="participant-select"
              value={selected}
              onChange={(event) => {
                const next = event.target.value;
                if (assignCommandRef.current?.participantActorId !== next) {
                  assignCommandRef.current = null;
                }
                setSelected(next);
              }}
            >
              <option value="">Select a Participant</option>
              {candidates.map((candidate) => (
                <option key={candidate.actor_id} value={candidate.actor_id}>{candidate.display_label}</option>
              ))}
            </select>
            <Button
              type="button"
              disabled={!selected || pending !== null}
              onClick={() => {
                setPending("assign");
                const retained = assignCommandRef.current?.participantActorId === selected
                  ? assignCommandRef.current
                  : { participantActorId: selected, key: createEnrollmentIdempotencyKey() };
                assignCommandRef.current = retained;
                client.assign(activityId, cohortId, selected, retained.key)
                  .then(async (outcome) => {
                    if (!outcome.succeeded) {
                      if (
                        outcome.outcome_code === "enrollment.conflict"
                        || outcome.outcome_code === "enrollment.unavailable"
                        || outcome.outcome_code === "enrollment.idempotency_conflict"
                      ) {
                        assignCommandRef.current = null;
                      }
                      setError(outcome.outcome_code === "enrollment.conflict"
                        ? "This Participant already has a live Enrollment in another Cohort."
                        : enrollmentOutcomeCopy(outcome.outcome_code, "The assignment could not be completed."));
                      return;
                    }
                    assignCommandRef.current = null;
                    setStatus(outcome.outcome_code === "enrollment.assignment.deduplicated"
                      ? "This Participant is already assigned to this Cohort."
                      : "Participant assigned.");
                    setError(null);
                    setEnrollments((await client.listEnrollments(activityId, cohortId)).items);
                  })
                  .catch((caught) => {
                    setError(enrollmentFailureCopy(caught, "The assignment could not be completed."));
                  })
                  .finally(() => { setPending(null); });
              }}
            >
              {pending === "assign" ? "Assigning…" : "Assign Participant"}
            </Button>
          </div>
        )}
      </section>

      <section className="page-section" aria-labelledby="enrollment-heading">
        <h2 id="enrollment-heading">Current enrollments</h2>
        {enrollments.length === 0 ? (
          <p>No Enrollments are recorded for this Cohort.</p>
        ) : (
          <ul>
            {enrollments.map((enrollment) => (
              <li key={enrollment.enrollment_id}>
                <p>
                  <Link to={`/activities/${activityId}/cohorts/${cohortId}/enrollments/${enrollment.enrollment_id}`}>
                    {enrollment.display_label}
                  </Link>
                  {" · "}
                  {enrollment.status}
                </p>
                {enrollment.permitted_actions.includes("suspend_enrollment") ? (
                  <Button type="button" variant="secondary" onClick={() => { setConfirm({ enrollment, operation: "suspend" }); }}>Suspend</Button>
                ) : null}
                {enrollment.permitted_actions.includes("restore_enrollment") ? (
                  <Button type="button" variant="secondary" onClick={() => { setConfirm({ enrollment, operation: "restore" }); }}>Restore</Button>
                ) : null}
                {enrollment.permitted_actions.includes("close_enrollment") ? (
                  <Button type="button" variant="secondary" onClick={() => { setConfirm({ enrollment, operation: "close" }); }}>Close</Button>
                ) : null}
                {enrollment.permitted_actions.includes("revoke_enrollment") ? (
                  <Button type="button" variant="secondary" onClick={() => { setConfirm({ enrollment, operation: "revoke" }); }}>Revoke</Button>
                ) : null}
              </li>
            ))}
          </ul>
        )}
      </section>

      <Dialog
        open={confirm !== null}
        title={confirm ? `${confirm.operation.charAt(0).toUpperCase()}${confirm.operation.slice(1)} this Enrollment?` : "Confirm"}
        confirmLabel={confirm ? `Confirm ${confirm.operation}` : "Confirm"}
        confirmVariant={confirm?.operation === "close" || confirm?.operation === "revoke" ? "danger" : "primary"}
        onCancel={() => { setConfirm(null); }}
        onConfirm={() => {
          const current = confirm;
          if (!current) {
            return;
          }
          setPending(current.operation);
          const identity = {
            enrollmentId: current.enrollment.enrollment_id,
            operation: current.operation,
            expectedRevision: current.enrollment.revision,
          };
          const retained = lifecycleCommandRef.current
            && lifecycleCommandRef.current.enrollmentId === identity.enrollmentId
            && lifecycleCommandRef.current.operation === identity.operation
            && lifecycleCommandRef.current.expectedRevision === identity.expectedRevision
            ? lifecycleCommandRef.current
            : { ...identity, key: createEnrollmentIdempotencyKey() };
          lifecycleCommandRef.current = retained;
          client.mutate(
            activityId,
            cohortId,
            current.enrollment.enrollment_id,
            current.operation,
            reasonFor[current.operation],
            current.enrollment.revision,
            retained.key,
          )
            .then(async (outcome) => {
              if (!outcome.succeeded) {
                if (outcome.outcome_code === "enrollment.stale_revision") {
                  lifecycleCommandRef.current = null;
                  setEnrollments((await client.listEnrollments(activityId, cohortId)).items);
                  setConfirm(null);
                  setError("This Enrollment changed. Review the current state before trying again.");
                  return;
                }
                setError(enrollmentOutcomeCopy(outcome.outcome_code, "The Enrollment could not be updated."));
                return;
              }
              lifecycleCommandRef.current = null;
              setStatus("Enrollment updated.");
              setEnrollments((await client.listEnrollments(activityId, cohortId)).items);
              setConfirm(null);
            })
            .catch((caught) => {
              setError(enrollmentFailureCopy(caught, "The Enrollment could not be updated."));
            })
            .finally(() => { setPending(null); });
        }}
      >
        {confirm ? (
          <p>
            This changes {confirm.enrollment.display_label} to {confirm.operation} with reason {reasonFor[confirm.operation]}.
            {confirm.operation === "close" || confirm.operation === "revoke"
              ? " This terminal change keeps history and removes the Assignment from current My work."
              : confirm.operation === "suspend"
                ? " The Participant will still see this Assignment as Suspended, without Open assignment."
                : " The Participant will see this Assignment as Active again."}
          </p>
        ) : null}
      </Dialog>
    </div>
  );
}
