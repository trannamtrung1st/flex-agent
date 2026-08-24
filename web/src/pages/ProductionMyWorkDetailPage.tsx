import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import {
  createProductionEnrollmentClient,
  EnrollmentRateLimitedCopy,
  enrollmentFailureCopy,
  type MyWorkTimingV2,
} from "../api/production-enrollment";
import { createProductionSubmissionClient } from "../api/production-submission";
import type { MyWorkSubmissionProjection } from "../contracts/v2";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";
import { formatCampaignInstant } from "../lib/campaign-timezone";

export function ProductionMyWorkDetailPage() {
  const { enrollmentId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const enrollmentClient = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const submissionClient = useMemo(() => createProductionSubmissionClient(fetchJson), [fetchJson]);
  const [timing, setTiming] = useState<MyWorkTimingV2 | null>(null);
  const [submission, setSubmission] = useState<MyWorkSubmissionProjection | null>(null);
  const [unavailable, setUnavailable] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    Promise.all([
      enrollmentClient.getMyWorkTiming(enrollmentId),
      submissionClient.getMyWorkSubmission(enrollmentId).catch(() => null),
    ])
      .then(([timingResult, submissionResult]) => {
        if (!cancelled) {
          setTiming(timingResult);
          setSubmission(submissionResult);
        }
      })
      .catch((caught: unknown) => {
        if (!cancelled) {
          setError(enrollmentFailureCopy(caught, ""));
          setUnavailable(true);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [enrollmentClient, submissionClient, enrollmentId]);

  if (unavailable) {
    const rateLimited = error === EnrollmentRateLimitedCopy;
    return (
      <StatusPanel title={rateLimited ? "Too many requests" : "Assignment unavailable"} variant="danger">
        <p>
          {rateLimited
            ? EnrollmentRateLimitedCopy
            : "This assignment is not available. Return to My work or contact the provided support route."}
        </p>
        <p><Link to="/my-work">Return to My work</Link></p>
      </StatusPanel>
    );
  }

  if (timing === null) {
    return <ProtectedLoading label="Loading assignment…" />;
  }

  const assignment = timing.assignment;
  const zone = timing.effective?.time_zone_id ?? assignment.time_zone_id ?? "UTC";
  const deadline = timing.effective?.submission_exclusive_end_utc ?? assignment.deadline_utc ?? null;
  const formatted = deadline ? formatCampaignInstant(deadline, zone) : null;

  return (
    <div>
      <header className="page-header">
        <h1>{assignment.activity_title ?? "Assignment"}</h1>
        <p>Current state: {assignment.status}.</p>
      </header>
      {assignment.summary_available ? (
        <section className="page-section">
          <h2>Task and exact timing</h2>
          <p>{assignment.task_title}</p>
          {formatted ? (
            formatted.conversionAvailable ? (
              <p>
                Submission cutoff {formatted.exactUtc} in {formatted.zoneLabel} ({formatted.localDisplay}).
              </p>
            ) : (
              <p>
                Submission cutoff {formatted.exactUtc} ({formatted.zoneLabel}; local conversion unavailable).
              </p>
            )
          ) : (
            <p>An exact cutoff is not currently available.</p>
          )}
          {timing.effective && !timing.effective.is_authoritative ? (
            <p>This timing is descriptive only and does not grant attempt authority.</p>
          ) : null}
          {timing.participant_consequence_code !== "none" ? (
            <p>An approved timing adjustment applies to this assignment.</p>
          ) : null}
        </section>
      ) : (
        <p>The assignment is visible, but the Task summary is currently unavailable.</p>
      )}
      <section className="page-section" aria-labelledby="submission-heading">
        <h2 id="submission-heading">Submission</h2>
        {submission === null ? (
          <p>Submission requirements are currently unavailable.</p>
        ) : submission.intake_available ? (
          <>
            <p>Direct text and UTF-8 `.txt` or `.md` attachments are supported for this assignment.</p>
            {submission.active_intake ? (
              <p role="status">Current intake state: {submission.active_intake.status}.</p>
            ) : (
              <p>No active intake. Prepare materials locally, then submit a version when ready.</p>
            )}
            {submission.version_history.length > 0 ? (
              <div>
                <h3>Accepted versions</h3>
                <ol reversed>
                  {submission.version_history.map((version) => {
                    const accepted = formatCampaignInstant(version.accepted_at_utc, zone);
                    return (
                      <li key={version.version_id}>
                        Version {version.version_number}
                        {accepted.conversionAvailable
                          ? ` accepted ${accepted.localDisplay ?? accepted.exactUtc} (${accepted.zoneLabel})`
                          : ` accepted ${accepted.exactUtc}`}
                        {" — "}
                        {version.item_count} item{version.item_count === 1 ? "" : "s"}
                      </li>
                    );
                  })}
                </ol>
              </div>
            ) : (
              <p>No accepted submission versions yet.</p>
            )}
          </>
        ) : (
          <p>
            Submission intake is not available
            {submission.unavailable_reason ? ` (${submission.unavailable_reason.replaceAll("_", " ")})` : ""}.
          </p>
        )}
      </section>
      {assignment.status === "suspended" ? (
        <p>This assignment is suspended. New submission or attempt actions are not available.</p>
      ) : null}
      <p><Link to="/my-work">Return to My work</Link></p>
    </div>
  );
}
