import { useCallback, useEffect, useId, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import {
  createProductionEnrollmentClient,
  enrollmentFailureCopy,
  type AssignmentSummaryV1,
  type MyWorkTimingV2,
} from "../api/production-enrollment";
import {
  createProductionSubmissionClient,
  createSubmissionIdempotencyKey,
  submissionFailureCopy,
  type MyWorkSubmissionV2,
} from "../api/production-submission";
import { AssignmentRecordReadout } from "../components/work/AssignmentRecordReadout";
import { AssignmentHead } from "../components/work/AssignmentHead";
import { AssignmentStatusReadout } from "../components/work/AssignmentStatusReadout";
import { IntakeItemList } from "../components/work/IntakeItemList";
import { campaignDeadlineCopy, formatCampaignInstant } from "../lib/campaign-timezone";
import {
  ACCOMMODATION_CONSEQUENCE_COPY,
  assignmentEligibilityCopy,
  enrollmentStatusCopy,
  wordsFromCode,
} from "../lib/enrollment-presentation";
import {
  Alert,
  CeremonyDialog,
  CompactId,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  ErrorSummary,
  FieldFile,
  FieldTextarea,
  FormField,
  GuidedTaskFoot,
  Inline,
  InstantReadout,
  Key,
  ReadoutList,
  Stack,
  usePushToast,
  WaitPlate,
  WorkWell,
  WorkWellHead,
  WorkWellSection,
} from "../design-system";
import { AcknowledgmentGate } from "../components/work/AcknowledgmentGate";
import { DIRECT_TEXT_PLACEHOLDER } from "../content/fieldCopy";
import { AssignmentSpine, type AssignmentStationView } from "../components/work/AssignmentSpine";
import { AssignmentStationLayout } from "../components/work/AssignmentStationLayout";
import { SubmissionVersionList } from "../components/work/SubmissionVersionList";
import type {
  CompleteIntakeItemCommandV2,
  MyWorkAttemptReadinessV2,
  SubmissionMaterialCategoryV2,
  SubmissionPermittedActionV2,
} from "../contracts/v2";

function actionsOf(submission: MyWorkSubmissionV2 | null): SubmissionPermittedActionV2[] {
  return submission?.active_intake?.permitted_actions ?? submission?.permitted_actions ?? [];
}

function attachmentCategory(filename: string): SubmissionMaterialCategoryV2 {
  return filename.toLowerCase().endsWith(".md") ? "text_markdown_attachment" : "text_plain_attachment";
}

async function readUtf8File(file: File): Promise<string> {
  const buffer = await file.arrayBuffer();
  const decoder = new TextDecoder("utf-8", { fatal: true });
  return decoder.decode(buffer);
}

function isReleasedRecord(status: string): boolean {
  return /releas|seal/i.test(status);
}

function attemptIsInProgress(attempt: MyWorkAttemptReadinessV2 | null): boolean {
  if (!attempt) return false;
  return (
    attempt.readiness_state === "active_conflict"
    || Boolean(attempt.active_session_id)
    || Boolean(attempt.active_attempt_id)
    || attempt.permitted_actions.includes("continue_attempt")
  );
}

function assignmentPhaseCopy(
  view: AssignmentStationView,
  pending: boolean,
  permitted: SubmissionPermittedActionV2[],
  intakeOpen: boolean,
  intakeItemCount = 0,
  inProgress = false,
): string {
  if (pending) return "Working…";
  if (inProgress) return "Attempt in progress";
  if (view === "attempt") return "Attempt";
  if (permitted.includes("begin_intake")) return "Begin intake";
  if (intakeOpen && permitted.includes("finalize_intake") && intakeItemCount > 0) return "Submit version";
  if (intakeOpen) return "Intake receiving";
  return "Submission";
}

function formatAttemptDuration(seconds?: number | null): string {
  if (seconds == null) return "No per-Attempt duration limit";
  if (seconds % 3600 === 0) {
    const hours = seconds / 3600;
    return hours === 1 ? "1 hour" : `${hours} hours`;
  }
  if (seconds % 60 === 0) {
    const minutes = seconds / 60;
    return minutes === 1 ? "1 minute" : `${minutes} minutes`;
  }
  return seconds === 1 ? "1 second" : `${seconds} seconds`;
}

function formatItemCount(count: number): string {
  return count === 1 ? "1 item" : `${count.toLocaleString("en-US")} items`;
}

function formatAttemptWindow(timing: MyWorkTimingV2 | null): string {
  const effective = timing?.effective;
  if (!effective) return "Start-window facts are not available from the server.";
  const zone = effective.time_zone_id;
  const opens = campaignDeadlineCopy(formatCampaignInstant(effective.attempt_start_utc, zone));
  const closes = campaignDeadlineCopy(formatCampaignInstant(effective.attempt_start_exclusive_end_utc, zone));
  return `Opens ${opens}. Exclusive end ${closes}.`;
}

function boundVersionSummary(attempt: MyWorkAttemptReadinessV2): string {
  const latest = [...attempt.bound_version_candidates].sort((a, b) => b.version_number - a.version_number)[0];
  if (!latest) return "No accepted Submission version is available to bind.";
  return `Submission Version ${latest.version_number}, ${formatItemCount(latest.item_count)}.`;
}

function acknowledgmentStateCopy(
  attempt: MyWorkAttemptReadinessV2,
  ackedByNotice: Record<string, boolean>,
): string {
  if (attempt.required_notices.length === 0) return "No required acknowledgments.";
  const recorded = attempt.required_notices.every((notice) => ackedByNotice[notice.notice_id]);
  return recorded ? "Required acknowledgments recorded" : "Required acknowledgments not yet recorded";
}

function formatByteLimit(bytes: number): string {
  if (bytes >= 1_048_576 && bytes % 1_048_576 === 0) {
    const megabytes = bytes / 1_048_576;
    return megabytes === 1 ? "1 MB" : `${megabytes} MB`;
  }
  if (bytes >= 1024 && bytes % 1024 === 0) {
    return `${bytes / 1024} KB`;
  }
  return `${bytes.toLocaleString("en-US")} bytes`;
}

function intakeItemLabel(category: string, filename?: string | null): string {
  if (filename) return filename;
  if (category === "direct_text") return "Direct text";
  if (category === "text_markdown_attachment") return "Markdown attachment";
  if (category === "text_plain_attachment") return "Text attachment";
  return "Material";
}

function AssignmentHeading({
  title,
  meta,
  phase,
  record,
  released,
}: {
  title: string;
  meta?: string;
  phase: string;
  record: string;
  released: boolean;
}) {
  return (
    <AssignmentHead
      title={title}
      meta={meta}
      status={(
        <AssignmentStatusReadout
          phase={phase}
          record={(
            <AssignmentRecordReadout
              variant={released ? "sealed" : "rest"}
              solid={released}
              label={record}
            />
          )}
        />
      )}
    />
  );
}

export function ProductionMyWorkDetailPage() {
  const { enrollmentId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const textId = useId();
  const filesId = useId();
  const confirmId = useId();
  const startConfirmId = useId();
  const ackId = useId();
  const enrollmentClient = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const submissionClient = useMemo(() => createProductionSubmissionClient(fetchJson), [fetchJson]);
  const [assignment, setAssignment] = useState<AssignmentSummaryV1 | null>(null);
  const [timing, setTiming] = useState<MyWorkTimingV2 | null>(null);
  const [submission, setSubmission] = useState<MyWorkSubmissionV2 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [directText, setDirectText] = useState("");
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [startConfirmOpen, setStartConfirmOpen] = useState(false);
  const [view, setView] = useState<AssignmentStationView>("submission");
  const [attempt, setAttempt] = useState<MyWorkAttemptReadinessV2 | null>(null);
  const [ackedByNotice, setAckedByNotice] = useState<Record<string, boolean>>({});
  const [startKey, setStartKey] = useState<string | null>(null);
  const [startOccupied, setStartOccupied] = useState(false);
  const pushToast = usePushToast();

  const reload = useCallback(async () => {
    const [work, timingResult, submissionResult, attemptResult] = await Promise.all([
      enrollmentClient.getMyWork(enrollmentId),
      enrollmentClient.getMyWorkTiming(enrollmentId).catch(() => null),
      submissionClient.getMyWorkSubmission(enrollmentId).catch(() => null),
      submissionClient.getAttemptReadiness(enrollmentId).catch(() => null),
    ]);
    setAssignment(work.assignment);
    setTiming(timingResult);
    setSubmission(submissionResult);
    setAttempt(attemptResult);
    setError(null);
  }, [enrollmentClient, enrollmentId, submissionClient]);

  useEffect(() => {
    const signal = { cancelled: false };
    void reload().catch((caught: unknown) => {
      if (!signal.cancelled) {
        setError(enrollmentFailureCopy(caught, "This assignment is not available."));
      }
    });
    return () => {
      signal.cancelled = true;
    };
  }, [reload]);

  useEffect(() => {
    if (attemptIsInProgress(attempt)) {
      setView("attempt");
    }
  }, [attempt]);

  const permitted = actionsOf(submission);
  const intake = submission?.active_intake ?? null;

  async function runMutation(
    work: () => Promise<{ succeeded: boolean; outcome_code: string }>,
    receipt?: { label: string; copy: string },
  ) {
    setPending(true);
    try {
      const outcome = await work();
      if (!outcome.succeeded) {
        setError(submissionFailureCopy(outcome.outcome_code));
        return false;
      }
      await reload();
      if (receipt) pushToast(receipt);
      return true;
    } catch (caught: unknown) {
      setError(enrollmentFailureCopy(caught, "The Submission could not be updated."));
      return false;
    } finally {
      setPending(false);
    }
  }

  async function reconcileStart() {
    if (!attempt || !startKey) return;
    setPending(true);
    try {
      const outcome = await submissionClient.reconcileAttempt(
        enrollmentId,
        startKey,
        attempt.start_command_digest,
      );
      setStartOccupied(false);
      setStartKey(null);
      if (!outcome.succeeded) {
        setError(submissionFailureCopy(outcome.outcome_code));
        await reload();
        return;
      }
      await reload();
      pushToast({ label: "Attempt", copy: "Attempt start was reconciled from the server." });
    } catch (caught: unknown) {
      setError(enrollmentFailureCopy(caught, "The Attempt start outcome is uncertain. Entitlement was not changed locally."));
    } finally {
      setPending(false);
    }
  }

  async function confirmStartAttempt() {
    if (!attempt) return;
    setPending(true);
    setStartOccupied(true);
    try {
      for (const notice of attempt.required_notices) {
        const ack = await submissionClient.acknowledgeNotice(
          enrollmentId,
          notice.notice_id,
          notice.source_version_id,
          "affirmed",
          createSubmissionIdempotencyKey(),
        );
        if (!ack.succeeded) {
          setError(submissionFailureCopy(ack.outcome_code));
          setStartOccupied(false);
          return;
        }
      }
      const key = startKey ?? createSubmissionIdempotencyKey();
      setStartKey(key);
      const started = await submissionClient.startAttempt(enrollmentId, key, attempt.start_command_digest);
      if (!started.succeeded) {
        setError(submissionFailureCopy(started.outcome_code));
        setStartOccupied(false);
        setStartKey(null);
        return;
      }
      await reload();
      setStartConfirmOpen(false);
      setStartOccupied(false);
      pushToast({ label: "Attempt", copy: "Attempt is in progress. Entitlement was consumed on the server." });
    } catch (caught: unknown) {
      setError(enrollmentFailureCopy(caught, "The Attempt start outcome is uncertain. Entitlement was not changed locally."));
    } finally {
      setPending(false);
    }
  }

  if (error && !assignment) {
    return (
      <AssignmentStationLayout
        instruments={null}
        heading={(
          <AssignmentHeading
            title="Assignment unavailable"
            phase="Unavailable"
            record="Unavailable"
            released={false}
          />
        )}
        actions={(
          <GuidedTaskFoot arrangement="end">
            <Key variant="quiet" to="/my-work">Return to My work</Key>
          </GuidedTaskFoot>
        )}
      >
        <WorkWell live={false} label="Assignment unavailable">
          <WorkWellSection>
            <p>{error}</p>
          </WorkWellSection>
        </WorkWell>
      </AssignmentStationLayout>
    );
  }

  if (!assignment) {
    return (
      <AssignmentStationLayout
        instruments={null}
        heading={<AssignmentHeading title="Assignment" phase="Loading" record="—" released={false} />}
      >
        <WorkWell live={false} label="Assignment">
          <WorkWellSection>
            <WaitPlate inset label="Loading assignment…" />
          </WorkWellSection>
        </WorkWell>
      </AssignmentStationLayout>
    );
  }

  const zone = timing?.effective?.time_zone_id ?? assignment.time_zone_id ?? "UTC";
  const deadline = assignment.deadline_utc;
  const formattedDeadline = deadline ? formatCampaignInstant(deadline, zone) : null;
  const eligibility = assignmentEligibilityCopy(
    timing?.effective?.eligibility_state,
    Boolean(intake),
    permitted.includes("begin_intake"),
  );
  const released = isReleasedRecord(assignment.status);
  const recordLabel = enrollmentStatusCopy(assignment.status);
  const consequence = wordsFromCode(
    timing?.participant_consequence_code && timing.participant_consequence_code !== "none"
      ? timing.participant_consequence_code
      : "none",
    ACCOMMODATION_CONSEQUENCE_COPY,
    "None",
  );
  const title = assignment.task_title ?? assignment.activity_title ?? "Assignment";
  const meta = assignment.activity_title && assignment.activity_title !== title
    ? assignment.activity_title
    : undefined;
  const recordMark = (
    <AssignmentRecordReadout
      variant={released ? "sealed" : "rest"}
      solid={released}
      label={recordLabel}
    />
  );

  const versions = [...(submission?.version_history ?? [])].sort((a, b) => b.version_number - a.version_number);
  const inProgress = attemptIsInProgress(attempt);

  const submissionActions = view === "submission" && (
    (!inProgress && permitted.includes("begin_intake")) || Boolean(intake)
  ) ? (() => {
    const beginKey = !inProgress && permitted.includes("begin_intake") ? (
      <Key
        variant="begin"
        disabled={pending}
        onClick={() => {
          void runMutation(
            () => submissionClient.beginIntake(enrollmentId, createSubmissionIdempotencyKey()),
            { label: "Intake", copy: "Intake is open." },
          );
        }}
      >
        Begin intake
      </Key>
    ) : null;
    const cancelKey = intake && permitted.includes("cancel_intake") ? (
      <Key
        variant="quiet"
        disabled={pending}
        onClick={() => {
          void runMutation(() =>
            submissionClient.cancelIntake(enrollmentId, intake.intake_id, {
              schema_version: "v2",
              expected_revision: intake.revision,
              idempotency_key: createSubmissionIdempotencyKey(),
            }),
          );
        }}
      >
        Cancel intake
      </Key>
    ) : null;
    const submitBlock = intake
      ? intake.items.length === 0
        ? "Add direct text or an attachment before submitting this version."
        : permitted.includes("finalize_intake")
          ? undefined
          : "Submit version is not permitted for this intake."
      : undefined;
    const submitKey = intake ? (
      <Key
        variant="transmit"
        ariaLabel="Submit version"
        disabled={pending || Boolean(submitBlock)}
        disabledReason={!pending ? submitBlock : undefined}
        onClick={() => setConfirmOpen(true)}
      >
        Submit version
      </Key>
    ) : null;

    if (cancelKey && submitKey) {
      return <GuidedTaskFoot arrangement="split" secondary={cancelKey} primary={submitKey} />;
    }

    return (
      <GuidedTaskFoot arrangement="end">
        {beginKey}
        {cancelKey}
        {submitKey}
      </GuidedTaskFoot>
    );
  })() : undefined;

  const noticesRequired = (attempt?.required_notices.length ?? 0) > 0;
  const noticesAcknowledged = Boolean(
    attempt
    && attempt.required_notices.every((notice) => ackedByNotice[notice.notice_id]),
  );
  const canStart = Boolean(
    attempt?.permitted_actions.includes("start_attempt") && (!noticesRequired || noticesAcknowledged),
  );
  const canContinue = Boolean(attempt?.permitted_actions.includes("continue_attempt") && attempt.active_session_id);
  const attemptActions = view === "attempt" ? (
    <GuidedTaskFoot arrangement="end">
      {startOccupied && startKey ? (
        <Key
          variant="quiet"
          disabled={pending}
          onClick={() => {
            if (!attempt || !startKey) return;
            void reconcileStart();
          }}
        >
          Reconcile start
        </Key>
      ) : null}
      {canContinue ? (
        <Key variant="begin" to={`/sessions/${attempt?.active_session_id}`}>
          Continue Attempt
        </Key>
      ) : null}
      {attempt?.permitted_actions.includes("start_attempt") ? (
        <Key
          variant="begin"
          disabled={pending || startOccupied || !canStart}
          disabledReason={noticesRequired && !noticesAcknowledged ? "Record required acknowledgments before starting." : undefined}
          onClick={() => setStartConfirmOpen(true)}
        >
          Start Attempt
        </Key>
      ) : null}
    </GuidedTaskFoot>
  ) : undefined;

  return (
    <AssignmentStationLayout
      instruments={(
        <>
          <ReadoutList
            rows={[
              { term: "Enrollment", value: <CompactId tabbable value={assignment.enrollment_id} /> },
              { term: "Campaign", value: assignment.activity_title ?? "—" },
              { term: "Task", value: assignment.task_title ?? "—" },
              {
                term: "Deadline",
                value: formattedDeadline ? campaignDeadlineCopy(formattedDeadline) : "No exclusive cutoff",
              },
              { term: "Record", value: recordMark },
            ]}
          />
          <AssignmentSpine view={view} onSelect={setView} />
        </>
      )}
      heading={(
        <AssignmentHeading
          title={title}
          meta={meta || undefined}
          phase={assignmentPhaseCopy(view, pending, permitted, Boolean(intake), intake?.items.length ?? 0, inProgress)}
          record={recordLabel}
          released={released}
        />
      )}
      actions={view === "attempt" ? attemptActions : submissionActions}
      overlays={(
        <>
        <CeremonyDialog open={confirmOpen} onClose={() => setConfirmOpen(false)} labelledBy={confirmId}>
          <DialogPlate>
            <DialogPlateHead title="Submit this version?" titleId={confirmId} />
            <DialogPlateBody>
              <p>
                Submit version accepts one immutable Submission version. Earlier accepted versions remain inspectable.
                Local drafts that were not added to this intake are not included.
              </p>
            </DialogPlateBody>
            <DialogPlateFooter
              arrangement="split"
              secondary={<Key variant="quiet" onClick={() => setConfirmOpen(false)}>Cancel</Key>}
              primary={
              <Key
                variant="transmit"
                disabled={pending || !intake}
                onClick={() => {
                  if (!intake) return;
                  void runMutation(() =>
                    submissionClient.finalizeIntake(enrollmentId, intake.intake_id, {
                      schema_version: "v2",
                      expected_revision: intake.revision,
                      idempotency_key: createSubmissionIdempotencyKey(),
                    }),
                    { label: "Submission", copy: "This version is preserved. Earlier versions remain on record." },
                  ).then(() => setConfirmOpen(false));
                }}
              >
                Submit version
              </Key>
              }
            />
          </DialogPlate>
        </CeremonyDialog>
        <CeremonyDialog open={startConfirmOpen} onClose={() => setStartConfirmOpen(false)} labelledBy={startConfirmId}>
          <DialogPlate>
            <DialogPlateHead title="Start this Attempt?" titleId={startConfirmId} />
            <DialogPlateBody>
              {attempt ? (
                <Stack gap="4">
                  <ReadoutList
                    rows={[
                      {
                        term: "Attempt",
                        value: `Attempt ${attempt.next_ordinal} of ${attempt.baseline_attempt_limit}`,
                      },
                      {
                        term: "Entitlement",
                        value: attempt.entitlement_source === "retry"
                          ? "Separately authorized retry entitlement"
                          : "Baseline entitlement",
                      },
                      {
                        term: "Duration",
                        value: formatAttemptDuration(timing?.effective?.per_attempt_duration_seconds),
                      },
                      {
                        term: "Start window",
                        value: formatAttemptWindow(timing),
                      },
                      {
                        term: "Submission",
                        value: boundVersionSummary(attempt),
                      },
                      {
                        term: "Acknowledgments",
                        value: acknowledgmentStateCopy(attempt, ackedByNotice),
                      },
                    ]}
                  />
                  <p>
                    If start succeeds, this Attempt is consumed and the selected Submission version is fixed for this Session.
                  </p>
                </Stack>
              ) : (
                <p>Authoritative Attempt readiness is required before start.</p>
              )}
            </DialogPlateBody>
            <DialogPlateFooter
              arrangement="split"
              secondary={<Key variant="quiet" onClick={() => setStartConfirmOpen(false)}>Cancel</Key>}
              primary={(
                <Key
                  variant="begin"
                  disabled={pending}
                  ariaLabel={attempt ? `Start Attempt ${attempt.next_ordinal}` : "Start Attempt"}
                  onClick={() => void confirmStartAttempt()}
                >
                  {attempt ? `Start Attempt ${attempt.next_ordinal}` : "Start Attempt"}
                </Key>
              )}
            />
          </DialogPlate>
        </CeremonyDialog>
        </>
      )}
    >
      {view === "attempt" ? (
        <WorkWell
          live={false}
          label="Attempt"
          head={<WorkWellHead title="Attempt" ident="Readiness · exact start" />}
        >
          <WorkWellSection>
            <Stack gap="4">
              {error ? <ErrorSummary errors={[error]} /> : null}
              {startOccupied ? <WaitPlate inset label="Starting Attempt…" /> : null}
              {attempt ? (
                <>
                  <p>
                    Remaining entitlement: {attempt.remaining_entitlement} of {attempt.baseline_attempt_limit}.
                    Next ordinal {attempt.next_ordinal}. Source {attempt.entitlement_source}.
                  </p>
                  <p>Readiness: {wordsFromCode(attempt.readiness_state)}.</p>
                  {attempt.active_session_id ? (
                    <p>
                      Committed Session locator: <CompactId tabbable value={attempt.active_session_id} />.
                      Live Session commands are not available from this application.
                    </p>
                  ) : null}
                  {attempt.bound_version_candidates.length > 0 ? (
                    <p>
                      Exact accepted Submission versions: {attempt.bound_version_candidates.map((item) => item.version_number).join(", ")}.
                    </p>
                  ) : (
                    <Alert variant="warning" title="Accepted material required">
                      Start requires at least one accepted Submission version on this assignment.
                    </Alert>
                  )}
                  {attempt.required_notices.map((notice) => (
                    <AcknowledgmentGate
                      key={notice.notice_id}
                      id={`${ackId}-${notice.notice_id}`}
                      checked={ackedByNotice[notice.notice_id] ?? false}
                      onChange={(checked) => {
                        setAckedByNotice((current) => ({ ...current, [notice.notice_id]: checked }));
                      }}
                    >
                      I acknowledge the required {wordsFromCode(notice.notice_type)} for this exact notice version.
                    </AcknowledgmentGate>
                  ))}
                </>
              ) : (
                <Alert variant="info" title="Attempt readiness unavailable">
                  Authoritative Attempt readiness could not be loaded for this assignment.
                </Alert>
              )}
            </Stack>
          </WorkWellSection>
        </WorkWell>
      ) : (
        <WorkWell
          live={false}
          label="Submission"
          head={<WorkWellHead title="Submission" ident="Intake · versioned preservation" />}
        >
          <WorkWellSection>
            <Stack gap="4">
              {error ? <Alert variant="danger" title="Action did not complete">{error}</Alert> : null}
              {submission?.intake_available === false ? (
                <Alert variant="warning" title="Intake unavailable">
                  {submission.unavailable_reason ?? "Submission intake is not available for this assignment."}
                </Alert>
              ) : null}
              {submission?.requirements ? (
                <p>
                  Direct text up to {formatByteLimit(submission.requirements.max_direct_text_bytes)}.
                  At most {submission.requirements.max_attachment_count} UTF-8 .txt or .md attachments.
                </p>
              ) : null}
              {intake ? (
                <p>
                  Intake {wordsFromCode(intake.status).toLowerCase()}, revision {intake.revision}.
                  {intake.items.length > 0
                    ? ` ${formatItemCount(intake.items.length)} received locally until the server accepts a version.`
                    : " No items yet."}
                </p>
              ) : (
                <p>No open intake. Direct text and attachments stay local until you begin intake and submit a version.</p>
              )}
              <p>
                Eligibility: {eligibility}. Accommodation: {consequence}.
              </p>
              {intake && intake.items.length > 0 ? (
                <IntakeItemList
                  label="Received intake items"
                  items={intake.items.map((item) => ({
                    id: item.item_id,
                    label: intakeItemLabel(item.category, item.filename),
                    detail: formatByteLimit(item.byte_count),
                  }))}
                />
              ) : null}
            </Stack>
          </WorkWellSection>
          {intake && permitted.includes("complete_item") ? (
            <WorkWellSection>
              <Stack gap="4">
                <FormField id={textId} label="Direct text" layout="stack">
                  {(control) => (
                    <FieldTextarea
                      {...control}
                      rows={8}
                      resize="vertical"
                      value={directText}
                      placeholder={DIRECT_TEXT_PLACEHOLDER}
                      disabled={pending}
                      onChange={(event) => setDirectText(event.target.value)}
                    />
                  )}
                </FormField>
                <Inline gap="2">
                  <Key
                    variant="quiet"
                    disabled={pending || directText.trim().length === 0}
                    onClick={() => {
                      const command: CompleteIntakeItemCommandV2 = {
                        schema_version: "v2",
                        category: "direct_text",
                        content: directText,
                        expected_revision: intake.revision,
                        idempotency_key: createSubmissionIdempotencyKey(),
                      };
                      void runMutation(() => submissionClient.completeItem(enrollmentId, intake.intake_id, command))
                        .then((ok) => {
                          if (ok) setDirectText("");
                        });
                    }}
                  >
                    Add direct text
                  </Key>
                </Inline>
                <FormField id={filesId} label="Attachments (.txt or .md)" layout="stack" labelAssociatesControl={false}>
                  {(control, meta) => (
                    <FieldFile
                      id={control.id}
                      labelledBy={meta.labelId}
                      mode="multiple"
                      accept=".txt,.md,text/plain,text/markdown"
                      hint="UTF-8 .txt or .md"
                      files={[]}
                      maxFiles={submission?.requirements?.max_attachment_count}
                      disabled={pending}
                      describedBy={control["aria-describedby"]}
                      invalid={control["aria-invalid"]}
                      onFilesChange={(files) => {
                        void (async () => {
                          let revision = intake.revision;
                          const intakeId = intake.intake_id;
                          for (const file of files) {
                            try {
                              const content = await readUtf8File(file);
                              const command: CompleteIntakeItemCommandV2 = {
                                schema_version: "v2",
                                category: attachmentCategory(file.name),
                                filename: file.name,
                                declared_mime_type: file.type || null,
                                content,
                                expected_revision: revision,
                                idempotency_key: createSubmissionIdempotencyKey(),
                              };
                              const ok = await runMutation(() => submissionClient.completeItem(enrollmentId, intakeId, command));
                              if (!ok) return;
                              const latest = await submissionClient.getMyWorkSubmission(enrollmentId);
                              revision = latest.active_intake?.revision ?? revision;
                            } catch {
                              setError("The material is not valid UTF-8 text.");
                              return;
                            }
                          }
                        })();
                      }}
                    />
                  )}
                </FormField>
              </Stack>
            </WorkWellSection>
          ) : null}
          <WorkWellSection>
            {versions.length > 0 ? (
              <SubmissionVersionList
                reversed
                label="Accepted submission versions"
                rows={versions.map((version) => ({
                  key: version.version_id,
                  versionNumber: version.version_number,
                  name: `Accepted version ${version.version_number} remains immutable.`,
                  meta: (
                    <>
                      <span>{formatItemCount(version.item_count)}</span>
                      <InstantReadout value={version.accepted_at_utc} timeZone={zone} />
                    </>
                  ),
                }))}
              />
            ) : (
              <p>No accepted Submission version yet.</p>
            )}
          </WorkWellSection>
        </WorkWell>
      )}
    </AssignmentStationLayout>
  );
}
