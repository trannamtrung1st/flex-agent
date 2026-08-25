import { useEffect, useMemo, useRef, useState, type ChangeEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { ProductionApiError, useProductionApi } from "../api/production-api";
import {
  createProductionEnrollmentClient,
  EnrollmentRateLimitedCopy,
  enrollmentFailureCopy,
  type MyWorkTimingV2,
} from "../api/production-enrollment";
import {
  createProductionSubmissionClient,
  createSubmissionIdempotencyKey,
  submissionFailureCopy,
  type MyWorkSubmissionV2,
  type AcceptedVersionDetailV2,
  type ProtectedItemPreviewV2,
} from "../api/production-submission";
import { Button } from "../components/ui/Button";
import { Dialog } from "../components/ui/Dialog";
import { ErrorSummary } from "../components/ui/ErrorSummary";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { SafeContent } from "../components/ui/SafeContent";
import { StatusPanel } from "../components/ui/StatusPanel";
import { formatCampaignInstant } from "../lib/campaign-timezone";

interface LocalFile {
  id: string;
  name: string;
  text: string;
}

export function ProductionMyWorkDetailPage() {
  const { enrollmentId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const enrollmentClient = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const submissionClient = useMemo(() => createProductionSubmissionClient(fetchJson), [fetchJson]);
  const [timing, setTiming] = useState<MyWorkTimingV2 | null>(null);
  const [submission, setSubmission] = useState<MyWorkSubmissionV2 | null>(null);
  const [unavailable, setUnavailable] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [directText, setDirectText] = useState("");
  const [files, setFiles] = useState<LocalFile[]>([]);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [pending, setPending] = useState(false);
  const [intakeStatus, setIntakeStatus] = useState<string | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [preview, setPreview] = useState<ProtectedItemPreviewV2 | null>(null);
  const [previewItems, setPreviewItems] = useState<AcceptedVersionDetailV2["items"]>([]);
  const [previewItem, setPreviewItem] = useState<{ versionId: string; itemId: string } | null>(null);
  const [previewVersionId, setPreviewVersionId] = useState<string | null>(null);
  const [previewUnavailable, setPreviewUnavailable] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const [dropActive, setDropActive] = useState(false);
  const [reconcileMode, setReconcileMode] = useState<
    "after-accept" | "after-cancel-success" | "after-cancel-uncertain" | null
  >(null);
  const errorSummaryRef = useRef<HTMLDivElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const submitButtonRef = useRef<HTMLButtonElement>(null);
  const submitGenerationRef = useRef(0);
  const knownAcceptedVersionIdsRef = useRef<ReadonlySet<string>>(new Set());

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
          setIntakeStatus(submissionResult?.active_intake?.status ?? null);
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

  useEffect(() => {
    return () => {
      setDirectText("");
      setFiles([]);
      setPreview(null);
      setPreviewItems([]);
      setPreviewItem(null);
      setPreviewUnavailable(false);
    };
  }, [enrollmentId]);

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
  const hasAccepted = (submission?.version_history.length ?? 0) > 0;
  const canPrepare = Boolean(submission?.intake_available);
  const localIssue = directText.trim().length === 0 && files.length === 0
    ? "Add direct text or a UTF-8 .txt or .md file before submitting a version."
    : null;
  const blockedIntake = intakeStatus === "reconciling" || intakeStatus === "cancelling";
  const reconciling = intakeStatus === "reconciling";
  const canCancel = Boolean(
    !reconciling
    && submission?.active_intake
    && ["receiving", "received", "validating"].includes(submission.active_intake.status),
  );

  function rememberActiveIntake(
    intakeId: string,
    revision: number,
    status: string,
    submissionId?: string | null,
  ) {
    setSubmission((current) => {
      if (current == null) {
        return current;
      }

      const previous = current.active_intake;
      return {
        ...current,
        active_intake: {
          intake_id: intakeId,
          submission_id: submissionId ?? previous?.submission_id ?? "",
          status: status as NonNullable<MyWorkSubmissionV2["active_intake"]>["status"],
          revision,
          created_at_utc: previous?.created_at_utc ?? new Date().toISOString(),
          updated_at_utc: new Date().toISOString(),
          complete_receipt_at_utc: previous?.complete_receipt_at_utc ?? null,
          items: previous?.items ?? [],
          permitted_actions: ["cancel_intake", "return_to_my_work"],
        },
      };
    });
  }

  async function refreshSubmission() {
    const next = await submissionClient.getMyWorkSubmission(enrollmentId);
    setSubmission(next);
    setIntakeStatus(next.active_intake?.status ?? null);
  }

  async function submitVersion() {
    const generation = ++submitGenerationRef.current;
    setPending(true);
    setConfirmOpen(false);
    setErrors([]);
    setIntakeStatus("receiving");
    try {
      const began = await submissionClient.beginIntake(enrollmentId, createSubmissionIdempotencyKey());
      if (generation !== submitGenerationRef.current) {
        return;
      }
      if (!began.succeeded || !began.intake_id || began.revision == null) {
        throw new ProductionApiError(409, submissionFailureCopy(began.outcome_code), began.outcome_code);
      }

      let revision = began.revision;
      const intakeId = began.intake_id;
      rememberActiveIntake(intakeId, revision, began.status ?? "receiving", began.submission_id);
      setIntakeStatus(began.status ?? "receiving");
      if (directText.trim().length > 0) {
        const completed = await submissionClient.completeItem(enrollmentId, intakeId, {
          schema_version: "v2",
          category: "direct_text",
          content: directText,
          expected_revision: revision,
          idempotency_key: createSubmissionIdempotencyKey(),
        });
        if (generation !== submitGenerationRef.current) {
          return;
        }
        if (!completed.succeeded || completed.revision == null) {
          throw new ProductionApiError(409, submissionFailureCopy(completed.outcome_code), completed.outcome_code);
        }
        revision = completed.revision;
        rememberActiveIntake(intakeId, revision, completed.status ?? "received", began.submission_id);
        setIntakeStatus(completed.status ?? "received");
      }

      for (const file of files) {
        const category = file.name.toLowerCase().endsWith(".md")
          ? "text_markdown_attachment"
          : "text_plain_attachment";
        const completed = await submissionClient.completeItem(enrollmentId, intakeId, {
          schema_version: "v2",
          category,
          filename: file.name,
          declared_mime_type: category === "text_markdown_attachment" ? "text/markdown" : "text/plain",
          content: file.text,
          expected_revision: revision,
          idempotency_key: createSubmissionIdempotencyKey(),
        });
        if (generation !== submitGenerationRef.current) {
          return;
        }
        if (!completed.succeeded || completed.revision == null) {
          throw new ProductionApiError(409, submissionFailureCopy(completed.outcome_code), completed.outcome_code);
        }
        revision = completed.revision;
        rememberActiveIntake(intakeId, revision, completed.status ?? "received", began.submission_id);
        setIntakeStatus(completed.status ?? "received");
      }

      if (generation !== submitGenerationRef.current) {
        return;
      }
      setIntakeStatus("validating");
      rememberActiveIntake(intakeId, revision, "validating", began.submission_id);
      const finalized = await submissionClient.finalizeIntake(enrollmentId, intakeId, {
        schema_version: "v2",
        expected_revision: revision,
        idempotency_key: createSubmissionIdempotencyKey(),
      });
      if (generation !== submitGenerationRef.current) {
        return;
      }
      if (!finalized.succeeded) {
        throw new ProductionApiError(409, submissionFailureCopy(finalized.outcome_code), finalized.outcome_code);
      }

      try {
        await refreshSubmission();
        setDirectText("");
        setFiles([]);
        setPreview(null);
        setPreviewItems([]);
        setPreviewItem(null);
        setIntakeStatus("accepted");
      } catch {
        setIntakeStatus("reconciling");
        setReconcileMode("after-accept");
        setErrors([
          "The server accepted this version, but the assignment view could not be refreshed. Wait and try again before submitting another version.",
        ]);
        requestAnimationFrame(() => {
          document.getElementById("submission-error-summary")?.focus();
        });
      }
    } catch (caught: unknown) {
      if (generation !== submitGenerationRef.current) {
        return;
      }
      const message = caught instanceof ProductionApiError
        ? submissionFailureCopy(caught.outcomeCode)
        : "The submission could not be accepted. No earlier version was changed.";
      setErrors([message]);
      try {
        await refreshSubmission();
      } catch {
        setIntakeStatus(null);
      }
      requestAnimationFrame(() => {
        document.getElementById("submission-error-summary")?.focus();
      });
    } finally {
      if (generation === submitGenerationRef.current) {
        setPending(false);
      }
    }
  }

  async function cancelActiveIntake() {
    const active = submission?.active_intake;
    if (!active) {
      return;
    }

    submitGenerationRef.current += 1;
    setPending(false);
    setCancelling(true);
    setErrors([]);
    setIntakeStatus("cancelling");
    knownAcceptedVersionIdsRef.current = new Set(
      submission?.version_history.map((item) => item.version_id) ?? [],
    );
    try {
      const cancelled = await submissionClient.cancelIntake(enrollmentId, active.intake_id, {
        schema_version: "v2",
        expected_revision: active.revision,
        idempotency_key: createSubmissionIdempotencyKey(),
      });
      if (!cancelled.succeeded) {
        throw new ProductionApiError(409, submissionFailureCopy(cancelled.outcome_code), cancelled.outcome_code);
      }
      try {
        await refreshSubmission();
        setReconcileMode(null);
        setIntakeStatus(cancelled.status ?? "cancelled");
      } catch {
        setReconcileMode("after-cancel-success");
        setIntakeStatus("reconciling");
        setErrors([
          "This intake was cancelled, but the assignment view could not be refreshed. Wait and try again before submitting another version.",
        ]);
        requestAnimationFrame(() => {
          document.getElementById("submission-error-summary")?.focus();
        });
      }
    } catch (caught: unknown) {
      try {
        const next = await submissionClient.getMyWorkSubmission(enrollmentId);
        setSubmission(next);
        setReconcileMode(null);
        setErrors([]);
        if (next.active_intake) {
          setIntakeStatus(next.active_intake.status);
        } else if (next.version_history.some((item) => !knownAcceptedVersionIdsRef.current.has(item.version_id))) {
          setIntakeStatus("accepted");
        } else {
          setIntakeStatus("cancelled");
        }
      } catch {
        setReconcileMode("after-cancel-uncertain");
        setIntakeStatus("reconciling");
        const message = caught instanceof ProductionApiError
          ? submissionFailureCopy(caught.outcomeCode)
          : "The intake could not be cancelled.";
        setErrors([
          `${message} Refresh the assignment to see the current intake state.`,
        ]);
        requestAnimationFrame(() => {
          document.getElementById("submission-error-summary")?.focus();
        });
      }
    } finally {
      setCancelling(false);
    }
  }

  async function retryRefresh() {
    try {
      const next = await submissionClient.getMyWorkSubmission(enrollmentId);
      setSubmission(next);
      setErrors([]);
      if (next.active_intake) {
        setIntakeStatus(next.active_intake.status);
        setReconcileMode(null);
        return;
      }

      if (reconcileMode === "after-accept") {
        setDirectText("");
        setFiles([]);
        setPreview(null);
        setPreviewItems([]);
        setPreviewItem(null);
        setIntakeStatus("accepted");
      } else if (reconcileMode === "after-cancel-success") {
        setIntakeStatus("cancelled");
      } else if (reconcileMode === "after-cancel-uncertain") {
        if (next.version_history.some((item) => !knownAcceptedVersionIdsRef.current.has(item.version_id))) {
          setDirectText("");
          setFiles([]);
          setPreview(null);
          setPreviewItems([]);
          setPreviewItem(null);
          setIntakeStatus("accepted");
        } else {
          setIntakeStatus("cancelled");
        }
      } else if (next.version_history.length > 0) {
        setIntakeStatus("accepted");
      } else {
        setIntakeStatus("cancelled");
      }
      setReconcileMode(null);
    } catch {
      setErrors([
        reconcileMode === "after-cancel-success"
          ? "This intake was cancelled, but the assignment view could not be refreshed. Wait and try again before submitting another version."
          : reconcileMode === "after-cancel-uncertain"
            ? "The assignment view could not be refreshed. Wait and try again to see the current intake state."
            : "The server accepted this version, but the assignment view could not be refreshed. Wait and try again before submitting another version.",
      ]);
      requestAnimationFrame(() => {
        document.getElementById("submission-error-summary")?.focus();
      });
    }
  }

  async function openPreview(versionId: string, itemId?: string) {
    setPreviewUnavailable(false);
    try {
      const current = await submissionClient.getMyWorkSubmission(enrollmentId);
      const version = current.version_history.find((item) => item.version_id === versionId);
      if (!version) {
        setPreview(null);
        setPreviewItems([]);
        setPreviewItem(null);
        setPreviewUnavailable(true);
        requestAnimationFrame(() => {
          document.getElementById("preview-unavailable")?.focus();
        });
        return;
      }

      const detail = await submissionClient.getAcceptedVersion(enrollmentId, versionId);
      setPreviewVersionId(versionId);
      setPreviewItems(detail.items);
      const selectedId = itemId ?? detail.items[0]?.item_id;
      if (!selectedId) {
        setPreview(null);
        setPreviewUnavailable(true);
        requestAnimationFrame(() => {
          document.getElementById("preview-unavailable")?.focus();
        });
        return;
      }
      const content = await submissionClient.getItemPreview(enrollmentId, versionId, selectedId);
      setPreview(content);
      setPreviewItem({ versionId, itemId: selectedId });
    } catch {
      setPreview(null);
      setPreviewItems([]);
      setPreviewItem(null);
      setPreviewUnavailable(true);
      requestAnimationFrame(() => {
        document.getElementById("preview-unavailable")?.focus();
      });
    }
  }

  async function downloadPreviewItem() {
    if (!previewItem) {
      return;
    }

    try {
      const response = await fetch(
        submissionClient.downloadItemUrl(enrollmentId, previewItem.versionId, previewItem.itemId),
        { credentials: "same-origin" },
      );
      if (!response.ok) {
        setPreview(null);
        setPreviewItem(null);
        setPreviewUnavailable(true);
        requestAnimationFrame(() => {
          document.getElementById("preview-unavailable")?.focus();
        });
        return;
      }

      const blob = await response.blob();
      const objectUrl = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = objectUrl;
      link.download = preview?.filename || "submission-item.txt";
      link.click();
      URL.revokeObjectURL(objectUrl);
    } catch {
      setPreview(null);
      setPreviewItem(null);
      setPreviewUnavailable(true);
    }
  }

  function addLocalFiles(selected: File[]) {
    Promise.all(selected.map(async (file) => ({
      id: crypto.randomUUID(),
      name: file.name,
      text: await file.text(),
    }))).then((next) => {
      setFiles((current) => [...current, ...next]);
    }).catch(() => {
      setErrors(["A selected file could not be read as text."]);
    });
  }

  function onChooseFiles(event: ChangeEvent<HTMLInputElement>) {
    addLocalFiles(Array.from(event.currentTarget.files ?? []));
    event.currentTarget.value = "";
  }

  function removeFile(fileId: string) {
    setFiles((current) => {
      const index = current.findIndex((item) => item.id === fileId);
      const next = current.filter((item) => item.id !== fileId);
      requestAnimationFrame(() => {
        const focusId = next[index]?.id ?? next[index - 1]?.id;
        if (focusId) {
          document.getElementById(`remove-file-${focusId}`)?.focus();
        } else {
          fileInputRef.current?.focus();
        }
      });
      return next;
    });
  }

  return (
    <div className="submission-intake">
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
        <div ref={errorSummaryRef}>
          <ErrorSummary headingId="submission-error-summary" errors={errors} />
        </div>
        {submission === null ? (
          <p>Submission requirements are currently unavailable.</p>
        ) : canPrepare ? (
          <>
            <p>
              Direct text and UTF-8 `.txt` or `.md` attachments are supported.
              Limits: {submission.requirements?.max_direct_text_bytes ?? 1048576} bytes of direct text,
              {" "}{submission.requirements?.max_attachment_count ?? 10} attachments.
            </p>
            {reconciling ? (
              <>
                <p role="status">Reconciling this intake. Wait for the current state before submitting another version.</p>
                <Button type="button" variant="secondary" onClick={() => { void retryRefresh(); }}>
                  Refresh assignment
                </Button>
              </>
            ) : intakeStatus ? (
              <p role="status">Current intake state: {intakeStatus}.</p>
            ) : (
              <p>No active intake. Prepare materials locally, then submit a version when ready.</p>
            )}
            <div className="form-field">
              <label htmlFor="direct-text">Direct text</label>
              <textarea
                id="direct-text"
                value={directText}
                onChange={(event) => {
                setDirectText(event.target.value);
              }}
                rows={8}
                disabled={pending}
              />
            </div>
            <div
              className={`file-drop${dropActive ? " is-active" : ""}`}
              onDragOver={(event) => {
                event.preventDefault();
                setDropActive(true);
              }}
              onDragLeave={() => {
                setDropActive(false);
              }}
              onDrop={(event) => {
                event.preventDefault();
                setDropActive(false);
                addLocalFiles(Array.from(event.dataTransfer.files));
              }}
            >
              <div className="form-field">
                <label htmlFor="choose-files">Attachments</label>
                <input
                  ref={fileInputRef}
                  id="choose-files"
                  type="file"
                  accept=".txt,.md,text/plain,text/markdown"
                  multiple
                  disabled={pending}
                  onChange={onChooseFiles}
                />
                <p className="hint">Optional: drop UTF-8 .txt or .md files here, or use Choose files.</p>
              </div>
            </div>
            {files.length > 0 ? (
              <ul className="attachment-list">
                {files.map((file) => (
                  <li key={file.id} className="attachment-row">
                    <span>{file.name}</span>
                    <Button
                      id={`remove-file-${file.id}`}
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => {
                        removeFile(file.id);
                      }}
                    >
                      Remove {file.name}
                    </Button>
                  </li>
                ))}
              </ul>
            ) : (
              <p>No local attachments selected.</p>
            )}
            <Button
              ref={submitButtonRef}
              variant="primary"
              disabled={pending || cancelling || blockedIntake || Boolean(localIssue)}
              onClick={() => {
                setConfirmOpen(true);
              }}
            >
              Submit version
            </Button>
            {canCancel ? (
              <Button
                type="button"
                variant="secondary"
                disabled={cancelling}
                onClick={() => {
                  void cancelActiveIntake();
                }}
              >
                Cancel intake
              </Button>
            ) : null}
            {localIssue ? <p className="hint">{localIssue}</p> : null}
          </>
        ) : (
          <p>
            Submission intake is not available
            {submission.unavailable_reason ? ` (${submission.unavailable_reason.replaceAll("_", " ")})` : ""}.
          </p>
        )}
        {submission !== null && submission.version_history.length > 0 ? (
          <div>
            <h3>Accepted versions</h3>
            <ol reversed className="version-history">
              {submission.version_history.map((version) => {
                const accepted = formatCampaignInstant(version.accepted_at_utc, zone);
                return (
                  <li key={version.version_id}>
                    <span>
                      Version {version.version_number}
                      {accepted.conversionAvailable
                        ? ` accepted ${accepted.localDisplay ?? accepted.exactUtc} (${accepted.zoneLabel})`
                        : ` accepted ${accepted.exactUtc}`}
                      {" — "}
                      {version.item_count} item{version.item_count === 1 ? "" : "s"}
                    </span>
                    <Button type="button" variant="secondary" size="sm" onClick={() => {
                      void openPreview(version.version_id);
                    }}>
                      Preview version {version.version_number}
                    </Button>
                  </li>
                );
              })}
            </ol>
          </div>
        ) : submission !== null ? (
          <p>No accepted submission versions yet.</p>
        ) : null}
        {previewUnavailable ? (
          <p id="preview-unavailable" tabIndex={-1}>This content is not available.</p>
        ) : null}
        {preview ? (
          <section className="page-section" aria-labelledby="preview-heading">
            <h3 id="preview-heading">Exact preview</h3>
            {previewItems.length > 1 ? (
              <ul className="attachment-list">
                {previewItems.map((item) => (
                  <li key={item.item_id} className="attachment-row">
                    <span>{item.filename ?? item.category.replaceAll("_", " ")}</span>
                    {item.preview_authorized ? (
                      <Button
                        type="button"
                        variant="secondary"
                        size="sm"
                        onClick={() => {
                          if (previewVersionId) {
                            void openPreview(previewVersionId, item.item_id);
                          }
                        }}
                      >
                        Preview {item.filename ?? item.category.replaceAll("_", " ")}
                      </Button>
                    ) : null}
                  </li>
                ))}
              </ul>
            ) : null}
            <SafeContent className="protected-preview">
              <pre>{preview.content}</pre>
            </SafeContent>
            {previewItem ? (
              <p>
                <Button type="button" variant="secondary" onClick={() => { void downloadPreviewItem(); }}>
                  Download exact item
                </Button>
              </p>
            ) : null}
          </section>
        ) : null}
      </section>
      {assignment.status === "suspended" ? (
        <p>This assignment is suspended. New submission or attempt actions are not available.</p>
      ) : null}
      <p><Link to="/my-work">Return to My work</Link></p>
      <Dialog
        open={confirmOpen}
        title={hasAccepted ? "Submit a new version?" : "Submit this version?"}
        confirmLabel="Submit version"
        cancelLabel="Keep editing"
        initialFocus="title"
        isConfirming={pending}
        onConfirm={() => {
          void submitVersion();
        }}
        onCancel={() => {
          setConfirmOpen(false);
        }}
      >
        <p>
          {hasAccepted
            ? "A new accepted version will be added. Earlier versions stay unchanged."
            : "This creates an accepted submission version. You can submit a later version if still permitted."}
        </p>
      </Dialog>
    </div>
  );
}
