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
import { formatCampaignInstant } from "../lib/campaign-timezone";
import {
  Alert,
  BackKey,
  CeremonyDialog,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  FieldTextarea,
  FormField,
  Inline,
  Key,
  OperateArea,
  ReadoutGrid,
  ReadoutGridField,
  ReadoutGridRow,
  Stack,
  StateReadout,
  WaitPanel,
  WorkWell,
  WorkWellHead,
  WorkWellSection,
} from "../design-system";
import { CeremonyArea, CeremonyEmpty } from "../components/shell/SessionChrome";
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
      <CeremonyArea label="Assignment unavailable" title="Assignment unavailable" danger>
        <CeremonyEmpty note={error}>
          <Key variant="open" to="/my-work">Return to My work</Key>
        </CeremonyEmpty>
      </CeremonyArea>
    );
  }

  if (!assignment) {
    return (
      <CeremonyArea label="Assignment" title="Assignment">
        <WaitPanel label="Loading assignment…" />
      </CeremonyArea>
    );
  }

  const zone = timing?.effective?.time_zone_id ?? assignment.time_zone_id ?? "UTC";
  const deadline = assignment.deadline_utc;
  const formattedDeadline = deadline ? formatCampaignInstant(deadline, zone) : null;
  const eligibility = timing?.effective?.eligibility_state;
  const released = isReleasedRecord(assignment.status);
  const consequence = timing?.participant_consequence_code && timing.participant_consequence_code !== "none"
    ? timing.participant_consequence_code
    : "None";

  return (
    <OperateArea
      className="workspace-area work-plane"
      frameClassName="destination-board assignment-station-board"
      frameInset="flush"
      label="Assignment"
      title={assignment.activity_title ?? assignment.task_title ?? "Assignment"}
      description="Overview, Task and timing, Submission intake, accepted versions, and Attempt readiness. The browser is not acceptance or Attempt-start authority."
      back={<BackKey to="/my-work" label="My work" />}
      context={(
        <ReadoutGrid label="Assignment identity" columns={4} className="assignment-instruments">
          <ReadoutGridRow label="Identity">
            <ReadoutGridField term="Enrollment">{assignment.enrollment_id}</ReadoutGridField>
            <ReadoutGridField term="Campaign">{assignment.activity_title ?? "—"}</ReadoutGridField>
            <ReadoutGridField term="Task">{assignment.task_title ?? "—"}</ReadoutGridField>
            <ReadoutGridField term="Record">
              <StateReadout
                variant={released ? "sealed" : "rest"}
                solid={released}
                label={assignment.status}
                className="assignment-record"
                labelClassName="assignment-record-label"
              />
            </ReadoutGridField>
          </ReadoutGridRow>
          <ReadoutGridRow label="Timing">
            <ReadoutGridField term="Deadline" span={2}>
              {formattedDeadline
                ? `${formattedDeadline.localDisplay ?? formattedDeadline.exactUtc} (${formattedDeadline.zoneLabel})`
                : "No exclusive cutoff"}
            </ReadoutGridField>
            <ReadoutGridField term="Eligibility">{eligibility ?? "—"}</ReadoutGridField>
            <ReadoutGridField term="Accommodation">{consequence}</ReadoutGridField>
          </ReadoutGridRow>
        </ReadoutGrid>
      )}
    >
      <div className="assignment-station">
        {error ? <Alert variant="danger" title="Action did not complete">{error}</Alert> : null}

        <WorkWell
          live={false}
          label="Submission"
          head={<WorkWellHead title="Submission" ident="Intake · versioned preservation" />}
          foot={
            permitted.includes("begin_intake") ? (
              <Key
                variant="quiet"
                disabled={pending}
                onClick={() => {
                  void runMutation(() => submissionClient.beginIntake(enrollmentId, createSubmissionIdempotencyKey()));
                }}
              >
                Begin intake
              </Key>
            ) : undefined
          }
        >
          <WorkWellSection>
            {submission?.intake_available === false ? (
              <Alert variant="warning" title="Intake unavailable">
                {submission.unavailable_reason ?? "Submission intake is not available for this assignment."}
              </Alert>
            ) : null}
            {submission?.requirements ? (
              <p>
                Direct text up to {submission.requirements.max_direct_text_bytes} bytes.
                At most {submission.requirements.max_attachment_count} UTF-8 .txt or .md attachments.
              </p>
            ) : null}
            {intake ? (
              <p>
                Intake {intake.status}, revision {intake.revision}.
                {intake.items.length > 0 ? ` ${intake.items.length} item(s) received locally until the server accepts a version.` : " No items yet."}
              </p>
            ) : (
              <p>No open intake. Direct text and attachments stay local until you begin intake and submit a version.</p>
            )}
          </WorkWellSection>
          {intake && permitted.includes("complete_item") ? (
            <WorkWellSection>
              <Stack gap="3">
                <FormField id={textId} label="Direct text" layout="stack">
                  {(control) => (
                    <FieldTextarea
                      {...control}
                      rows={8}
                      resize="vertical"
                      value={directText}
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
                <FormField id={filesId} label="Attachments (.txt or .md)" layout="stack">
                  {(control) => (
                    <input
                      {...control}
                      type="file"
                      accept=".txt,.md,text/plain,text/markdown"
                      multiple
                      disabled={pending}
                      onChange={(event) => {
                        const files = Array.from(event.target.files ?? []);
                        event.target.value = "";
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
          {intake && (permitted.includes("cancel_intake") || permitted.includes("finalize_intake")) ? (
            <WorkWellSection>
              <Inline gap="2">
                {permitted.includes("cancel_intake") ? (
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
                ) : null}
                {permitted.includes("finalize_intake") ? (
                  <Key variant="transmit" disabled={pending} onClick={() => setConfirmOpen(true)}>
                    Submit version
                  </Key>
                ) : null}
              </Inline>
            </WorkWellSection>
          ) : null}
        </WorkWell>

        <WorkWell
          live={false}
          label="Accepted versions"
          head={<WorkWellHead title="Accepted versions" ident="Immutable once accepted" />}
        >
          <WorkWellSection>
            {submission && submission.version_history.length > 0 ? (
              <ol reversed>
                {[...submission.version_history].sort((a, b) => b.version_number - a.version_number).map((version) => (
                  <li key={version.version_id}>
                    Accepted version {version.version_number} remains immutable.
                    {" "}
                    {version.item_count} item(s), accepted {version.accepted_at_utc}.
                  </li>
                ))}
              </ol>
            ) : (
              <p>No accepted Submission version yet.</p>
            )}
          </WorkWellSection>
        </WorkWell>

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
      </div>

      <CeremonyDialog open={confirmOpen} onClose={() => setConfirmOpen(false)} labelledBy={confirmId}>
        <DialogPlate>
          <DialogPlateHead title="Submit this version?" titleId={confirmId} />
          <DialogPlateBody>
            <p>
              Submit version accepts one immutable Submission version. Earlier accepted versions remain inspectable.
              Local drafts that were not added to this intake are not included.
            </p>
          </DialogPlateBody>
          <DialogPlateFooter>
            <Key variant="quiet" onClick={() => setConfirmOpen(false)}>Cancel</Key>
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
          </DialogPlateFooter>
        </DialogPlate>
      </CeremonyDialog>
    </OperateArea>
  );
}
