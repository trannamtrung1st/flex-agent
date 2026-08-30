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
import { campaignDeadlineCopy, formatCampaignInstant } from "../lib/campaign-timezone";
import {
  ACCOMMODATION_CONSEQUENCE_COPY,
  ELIGIBILITY_COPY,
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
  FieldFile,
  FieldTextarea,
  FormField,
  DIRECT_TEXT_PLACEHOLDER,
  GuidedTaskFoot,
  Inline,
  InstantReadout,
  Key,
  ReadoutList,
  Stack,
  StateReadout,
  WaitPlate,
  WorkWell,
  WorkWellHead,
  WorkWellSection,
} from "../design-system";
import { AssignmentSpine, type AssignmentStationView } from "../components/work/AssignmentSpine";
import { AssignmentStationLayout } from "../components/work/AssignmentStationLayout";
import { SubmissionVersionList } from "../components/work/SubmissionVersionList";
import type {
  CompleteIntakeItemCommandV2,
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

function assignmentPhaseCopy(
  view: AssignmentStationView,
  pending: boolean,
  permitted: SubmissionPermittedActionV2[],
  intakeOpen: boolean,
): string {
  if (pending) return "Working…";
  if (view === "attempt") return "Not available here";
  if (permitted.includes("begin_intake")) return "Begin intake";
  if (intakeOpen && permitted.includes("finalize_intake")) return "Submit version";
  if (intakeOpen) return "Intake receiving";
  return "Submission";
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

function formatItemCount(count: number): string {
  return count === 1 ? "1 item" : `${count.toLocaleString("en-US")} items`;
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
    <header className="assignment-head">
      <div className="assignment-ident">
        <h1 className="assignment-title">{title}</h1>
        {meta ? <p className="assignment-meta">{meta}</p> : null}
      </div>
      <dl className="status-readout" aria-label="Assignment status">
        <div className="status-item">
          <dt>Phase</dt>
          <dd>{phase}</dd>
        </div>
        <div className="status-item">
          <dt>Record</dt>
          <dd>
            <StateReadout
              variant={released ? "sealed" : "rest"}
              solid={released}
              label={record}
              className="assignment-record"
              labelClassName="assignment-record-label"
            />
          </dd>
        </div>
      </dl>
    </header>
  );
}

export function ProductionMyWorkDetailPage() {
  const { enrollmentId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const textId = useId();
  const filesId = useId();
  const confirmId = useId();
  const enrollmentClient = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const submissionClient = useMemo(() => createProductionSubmissionClient(fetchJson), [fetchJson]);
  const [assignment, setAssignment] = useState<AssignmentSummaryV1 | null>(null);
  const [timing, setTiming] = useState<MyWorkTimingV2 | null>(null);
  const [submission, setSubmission] = useState<MyWorkSubmissionV2 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [directText, setDirectText] = useState("");
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [view, setView] = useState<AssignmentStationView>("submission");

  const reload = useCallback(async () => {
    const [work, timingResult, submissionResult] = await Promise.all([
      enrollmentClient.getMyWork(enrollmentId),
      enrollmentClient.getMyWorkTiming(enrollmentId).catch(() => null),
      submissionClient.getMyWorkSubmission(enrollmentId).catch(() => null),
    ]);
    setAssignment(work.assignment);
    setTiming(timingResult);
    setSubmission(submissionResult);
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

  const permitted = actionsOf(submission);
  const intake = submission?.active_intake ?? null;

  async function runMutation(work: () => Promise<{ succeeded: boolean; outcome_code: string }>) {
    setPending(true);
    try {
      const outcome = await work();
      if (!outcome.succeeded) {
        setError(submissionFailureCopy(outcome.outcome_code));
        return false;
      }
      await reload();
      return true;
    } catch (caught: unknown) {
      setError(enrollmentFailureCopy(caught, "The Submission could not be updated."));
      return false;
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
  const eligibility = wordsFromCode(timing?.effective?.eligibility_state, ELIGIBILITY_COPY);
  const released = isReleasedRecord(assignment.status);
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
    <StateReadout
      variant={released ? "sealed" : "rest"}
      solid={released}
      label={assignment.status}
      className="assignment-record"
      labelClassName="assignment-record-label"
    />
  );

  const versions = [...(submission?.version_history ?? [])].sort((a, b) => b.version_number - a.version_number);

  const submissionActions = view === "submission" && (
    permitted.includes("begin_intake")
    || permitted.includes("cancel_intake")
    || permitted.includes("finalize_intake")
  ) ? (() => {
    const beginKey = permitted.includes("begin_intake") ? (
      <Key
        variant="begin"
        disabled={pending}
        onClick={() => {
          void runMutation(() => submissionClient.beginIntake(enrollmentId, createSubmissionIdempotencyKey()));
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
    const submitKey = intake && permitted.includes("finalize_intake") ? (
      <Key variant="transmit" disabled={pending} onClick={() => setConfirmOpen(true)}>
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
          phase={assignmentPhaseCopy(view, pending, permitted, Boolean(intake))}
          record={assignment.status}
          released={released}
        />
      )}
      actions={submissionActions}
      overlays={(
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
                  ).then(() => setConfirmOpen(false));
                }}
              >
                Submit version
              </Key>
              }
            />
          </DialogPlate>
        </CeremonyDialog>
      )}
    >
      {view === "attempt" ? (
        <WorkWell
          live={false}
          label="Attempt"
          head={<WorkWellHead title="Attempt" ident="Separate committed server command" />}
        >
          <WorkWellSection>
            <Alert variant="info" title="Start Attempt is not available from this SPA">
              Attempt start remains a separate committed server command. No production Attempt-start HTTP contract is exposed to this application yet, so this surface will not invent a ready Session.
            </Alert>
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
                  Intake {intake.status}, revision {intake.revision}.
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
                <ul className="intake-item-list" aria-label="Received intake items">
                  {intake.items.map((item) => (
                    <li className="intake-item-row" key={item.item_id}>
                      <span>{intakeItemLabel(item.category, item.filename)}</span>
                      <span>{formatByteLimit(item.byte_count)}</span>
                    </li>
                  ))}
                </ul>
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
