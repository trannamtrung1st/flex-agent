import { useEffect, useId, useRef, useState } from "react";
import { Link, useBlocker, useParams } from "react-router-dom";
import { ProductionApiError } from "../api/production-api";
import { Alert } from "../components/ui/Alert";
import { Button } from "../components/ui/Button";
import { Card, CardBody, CardHeader, CardTitle } from "../components/ui/Card";
import { Dialog } from "../components/ui/Dialog";
import { ErrorSummary } from "../components/ui/ErrorSummary";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";

export interface AssessmentSetupView {
  activity_id: string;
  title: string;
  revision_id?: string;
  revision_number: number;
  memory_mode: string;
  has_activated_cohort: boolean;
  permitted_actions: string[];
  cohort_id?: string;
  overall_severity?: string;
  issues?: Array<{ category: string; severity: string; reason_code: string; recovery_hint: string }>;
  baseline_digest?: string;
  verification_status?: string;
  task_title?: string;
  timing?: {
    time_zone_id: string;
    attempt_limit: number;
    starts_at_utc: string;
    ends_at_utc: string;
    deadline_utc: string;
    per_attempt_duration_seconds?: number | null;
  };
  disabled_capabilities?: string[];
  sources?: Array<{ category: string; source_id: string; version_id: string; content_digest: string }>;
}

interface AssessmentSetupPageProps {
  loadSetup: (activityId: string) => Promise<AssessmentSetupView>;
  saveDraft: (activityId: string, title: string, expectedRevision: number) => Promise<AssessmentSetupView>;
  checkReadiness: (activityId: string) => Promise<AssessmentSetupView>;
  activateCohort?: (activityId: string, view: AssessmentSetupView) => Promise<AssessmentSetupView>;
}

type PendingAction = "load" | "save" | "ready" | "activate" | null;

function sourceSummary(view: AssessmentSetupView, category: string, title?: string) {
  const source = view.sources?.find((item) => item.category === category);
  if (!source) {
    return title ?? "Not selected";
  }

  return title ? `${title} · ${source.version_id}` : source.version_id;
}

function isAccessLoss(cause: unknown) {
  if (cause instanceof ProductionApiError && cause.outcomeCode === "assessment.denied") {
    return true;
  }

  return cause instanceof Error && /access changed|expired|denied/i.test(cause.message);
}

export function AssessmentSetupPage({
  loadSetup,
  saveDraft,
  checkReadiness,
  activateCohort,
}: AssessmentSetupPageProps) {
  const { activityId } = useParams<{ activityId: string }>();
  const titleId = useId();
  const readinessHeadingId = useId();
  const titleInputRef = useRef<HTMLInputElement>(null);
  const saveButtonRef = useRef<HTMLButtonElement>(null);
  const readinessHeadingRef = useRef<HTMLHeadingElement>(null);
  const successHeadingRef = useRef<HTMLHeadingElement>(null);
  const accessLinkRef = useRef<HTMLAnchorElement>(null);
  const confirmDescId = useId();
  const [view, setView] = useState<AssessmentSetupView | null>(null);
  const [title, setTitle] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [pending, setPending] = useState<PendingAction>("load");
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [newCohortOpen, setNewCohortOpen] = useState(false);
  const [accessChanged, setAccessChanged] = useState(false);

  const dirty = Boolean(view && title !== view.title);

  const blocker = useBlocker(dirty && pending === null);

  const applyAccessLoss = () => {
    setView(null);
    setTitle("");
    setError(null);
    setStatus(null);
    setConfirmOpen(false);
    setAccessChanged(true);
  };

  useEffect(() => {
    if (view?.has_activated_cohort) {
      successHeadingRef.current?.focus();
    }
  }, [view?.has_activated_cohort]);

  useEffect(() => {
    if (accessChanged) {
      accessLinkRef.current?.focus();
    }
  }, [accessChanged]);

  useEffect(() => {
    if (!dirty) {
      return;
    }

    const onBeforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault();
    };
    window.addEventListener("beforeunload", onBeforeUnload);
    return () => {
      window.removeEventListener("beforeunload", onBeforeUnload);
    };
  }, [dirty]);

  useEffect(() => {
    if (!activityId) {
      return;
    }

    let active = true;
    void loadSetup(activityId)
      .then((next) => {
        if (!active) {
          return;
        }

        setView(next);
        setTitle(next.title);
        setPending(null);
      })
      .catch((cause: unknown) => {
        if (!active) {
          return;
        }

        if (isAccessLoss(cause) || (cause instanceof Error && /denied/i.test(cause.message))) {
          applyAccessLoss();
        } else {
          setError("This setup is unavailable.");
        }

        setPending(null);
      });

    return () => {
      active = false;
    };
  }, [activityId, loadSetup]);

  if (pending === "load") {
    return <ProtectedLoading label="Loading Campaign setup" />;
  }

  if (accessChanged) {
    return (
      <StatusPanel title="Your access changed" variant="danger">
        <p>Protected setup values were removed. Return to Activities or sign in again.</p>
        <p>
          <Link ref={accessLinkRef} to="/activities">Back to Activities</Link>
        </p>
      </StatusPanel>
    );
  }

  if (!view || !activityId) {
    return (
      <StatusPanel title="Access denied" variant="danger">
        <p>{error ?? "This setup is unavailable."}</p>
      </StatusPanel>
    );
  }

  const blocked = view.overall_severity === "blocked";
  const warning = view.overall_severity === "warning";
  const outOfDate = view.overall_severity === "out_of_date";
  const ready = view.overall_severity === "ready";
  const canActivate = view.permitted_actions.includes("activate_cohort") && (ready || warning) && !dirty;
  const reconciling = pending === "activate" && status === "Checking activation status";
  const materialIssues = (view.issues ?? []).filter(
    (issue) => issue.severity === "blocked" || issue.severity === "warning",
  );
  const readinessHeading = blocked
    ? "Readiness blocked"
    : warning
      ? "Ready with warnings"
      : outOfDate
        ? "Readiness out of date"
        : ready
          ? "Ready to activate"
          : "Readiness";

  return (
    <div>
      <header className="page-header">
        <h1>Setup and readiness</h1>
        <p>Save an expected Campaign revision, check readiness, then deliberately activate an empty Cohort.</p>
      </header>

      <div className="sr-only" aria-live="polite">
        {status}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Assessment Campaign</CardTitle>
        </CardHeader>
        <CardBody>
          <form
            className="stack"
            onSubmit={(event) => {
              event.preventDefault();
              setPending("save");
              setStatus("Saving draft…");
              void saveDraft(activityId, title, view.revision_number)
                .then((next) => {
                  const staleReadiness = view.issues != null;
                  setView(
                    staleReadiness
                      ? { ...next, overall_severity: "out_of_date", issues: [] }
                      : next,
                  );
                  setTitle(next.title);
                  setError(null);
                  setStatus(`Draft revision saved. Revision ${String(next.revision_number)}.`);
                  saveButtonRef.current?.focus();
                })
                .catch((cause: unknown) => {
                  if (isAccessLoss(cause)) {
                    applyAccessLoss();
                    return;
                  }

                  const message = cause instanceof Error ? cause.message : "The draft could not be saved.";
                  setError(message === "This draft changed" ? "This draft changed" : "The draft could not be saved.");
                  titleInputRef.current?.focus();
                })
                .finally(() => {
                  setPending(null);
                });
            }}
          >
            <label htmlFor={titleId}>Campaign title</label>
            <input
              ref={titleInputRef}
              id={titleId}
              name="title"
              value={title}
              onChange={(event) => {
                setTitle(event.target.value);
              }}
              required
              maxLength={200}
              disabled={view.has_activated_cohort}
            />
            {error ? <ErrorSummary title="Correct the following" errors={[error]} /> : null}
            {view.has_activated_cohort ? null : (
              <Button ref={saveButtonRef} type="submit" disabled={pending !== null}>
                {pending === "save" ? "Saving…" : "Save draft"}
              </Button>
            )}
          </form>
          {dirty ? <Alert variant="info" title="Unsaved changes">Save the draft before leaving or checking readiness.</Alert> : null}
          {status?.startsWith("Draft revision saved") ? (
            <Alert variant="success" title="Draft revision saved">
              Current revision {view.revision_number}. Check readiness on this saved revision.
            </Alert>
          ) : null}
        </CardBody>
      </Card>

      <section className="page-section" aria-labelledby="sources-heading">
        <h2 id="sources-heading">Selected source revisions</h2>
        {view.sources?.length ? (
          <ul aria-label="Selected source revisions">
            {view.sources.map((source) => (
              <li key={`${source.category}-${source.version_id}`}>
                {source.category}: {source.version_id}
              </li>
            ))}
          </ul>
        ) : (
          <p>No permitted source revisions are selected.</p>
        )}
      </section>

      <section className="page-section" aria-labelledby={readinessHeadingId}>
        <h2 ref={readinessHeadingRef} id={readinessHeadingId} tabIndex={-1}>
          {readinessHeading}
        </h2>
        <p>Memory default: Stable, approved reads {view.memory_mode}.</p>
        {outOfDate ? (
          <p>This readiness result is out of date. Check readiness on the current saved revision.</p>
        ) : view.issues == null ? (
          view.has_activated_cohort ? null : (
            <p>Readiness has not been checked for this saved revision.</p>
          )
        ) : materialIssues.length > 0 ? (
          <ul aria-label="Readiness issues">
            {materialIssues.map((issue) => (
              <li key={`${issue.category}-${issue.reason_code}`}>
                <Alert
                  variant={issue.severity === "blocked" ? "danger" : issue.severity === "warning" ? "warning" : "info"}
                  title={issue.category}
                >
                  {issue.recovery_hint}
                </Alert>
              </li>
            ))}
          </ul>
        ) : (
          <p>No readiness blockers for this saved revision.</p>
        )}
        {view.has_activated_cohort ? null : (
          <Button
            type="button"
            disabled={pending !== null || dirty || !view.permitted_actions.includes("check_readiness")}
            onClick={() => {
              setPending("ready");
              setStatus("Checking readiness…");
              void checkReadiness(activityId)
                .then((next) => {
                  setView(next);
                  setError(null);
                  const blockers = next.issues?.filter((issue) => issue.severity === "blocked").length ?? 0;
                  setStatus(
                    next.overall_severity === "ready"
                      ? "Ready to activate"
                      : `Readiness ${next.overall_severity ?? "checked"}. ${String(blockers)} blockers.`,
                  );
                  readinessHeadingRef.current?.focus();
                })
                .catch((cause: unknown) => {
                  if (isAccessLoss(cause)) {
                    applyAccessLoss();
                    return;
                  }

                  setError("Readiness could not be checked.");
                })
                .finally(() => {
                  setPending(null);
                });
            }}
          >
            {pending === "ready" ? "Checking…" : "Check readiness"}
          </Button>
        )}
      </section>

      <section className="page-section" aria-labelledby="activation-heading">
        <h2 id="activation-heading">Activation</h2>
        {view.has_activated_cohort ? (
          <>
            <Alert variant={view.verification_status === "degraded" ? "warning" : "success"} title="Activated baseline">
              <h3 ref={successHeadingRef} tabIndex={-1}>
                {view.verification_status === "degraded" ? "Baseline verification is degraded" : "Cohort activated"}
              </h3>
              <p>Baseline digest {view.baseline_digest ?? "is recorded"}.</p>
            </Alert>
            {view.permitted_actions.includes("assign_participants") && view.cohort_id ? (
              <p>
                <Link to={`/activities/${view.activity_id}/cohorts/${view.cohort_id}/participants`}>
                  Assign Participants
                </Link>
              </p>
            ) : (
              <p>Assign Participants is available after the current administrator is authorized for Enrollment.</p>
            )}
            <Button type="button" variant="secondary" onClick={() => { setNewCohortOpen(true); }}>
              Change assessment configuration
            </Button>
          </>
        ) : (
          <>
            {reconciling ? (
              <Alert variant="info" title="Reconciling activation">
                The last activation response was uncertain. Authoritative status was queried before another command is offered.
              </Alert>
            ) : null}
            <Button type="button" disabled={!canActivate || pending !== null} onClick={() => {
              setConfirmOpen(true);
            }}>
              Activate cohort
            </Button>
          </>
        )}
      </section>

      <Dialog
        open={confirmOpen}
        title="Activate cohort?"
        confirmLabel="Activate cohort"
        describedBy={confirmDescId}
        confirmDisabled={!activateCohort || pending !== null}
        isConfirming={pending === "activate"}
        onCancel={() => {
          setConfirmOpen(false);
        }}
        onConfirm={() => {
          if (!activateCohort) {
            return;
          }

          setPending("activate");
          setStatus("Activating cohort…");
          void activateCohort(activityId, view)
            .then((next) => {
              setView(next);
              setConfirmOpen(false);
              setError(null);
              setStatus("Cohort activated");
              setPending(null);
            })
            .catch((cause: unknown) => {
              if (isAccessLoss(cause)) {
                applyAccessLoss();
                setPending(null);
                return;
              }

              setConfirmOpen(false);
              setError(null);
              setStatus("Checking activation status");
              void loadSetup(activityId)
                .then((next) => {
                  setView(next);
                  setTitle(next.title);
                  if (next.has_activated_cohort) {
                    setStatus("Cohort activated");
                    return;
                  }

                  setError("The cohort was not activated");
                  setStatus(null);
                })
                .catch((reloadCause: unknown) => {
                  if (isAccessLoss(reloadCause)) {
                    applyAccessLoss();
                    return;
                  }

                  setError("The cohort was not activated");
                  setStatus(null);
                })
                .finally(() => {
                  setPending(null);
                });
            });
        }}
      >
        <p id={confirmDescId}>
          Activation freezes this cohort&apos;s assessment configuration. Material changes will require a new
          Activity revision and cohort.
        </p>
        <p>
          Saved revision {view.revision_number}. Candidate cohort {view.cohort_id ?? "is not yet assigned"}.
        </p>
        <dl className="compact-summary">
          <div>
            <dt>Task</dt>
            <dd>{sourceSummary(view, "task_submission", view.task_title)}</dd>
          </div>
          <div>
            <dt>Agent</dt>
            <dd>{sourceSummary(view, "agent")}</dd>
          </div>
          <div>
            <dt>Harness</dt>
            <dd>{sourceSummary(view, "harness")}</dd>
          </div>
          <div>
            <dt>Timing</dt>
            <dd>
              {view.timing
                ? `${view.timing.time_zone_id}, ${view.timing.starts_at_utc} to ${view.timing.ends_at_utc}`
                : "Not recorded"}
            </dd>
          </div>
          <div>
            <dt>Attempts</dt>
            <dd>
              {view.timing
                ? `${String(view.timing.attempt_limit)}${view.timing.per_attempt_duration_seconds
                  ? `, ${String(view.timing.per_attempt_duration_seconds)} seconds each`
                  : ""}`
                : "Not recorded"}
            </dd>
          </div>
          <div>
            <dt>Memory</dt>
            <dd>Stable, approved reads {view.memory_mode}</dd>
          </div>
          <div>
            <dt>Disabled capabilities</dt>
            <dd>{view.disabled_capabilities?.join(", ") || "None listed"}</dd>
          </div>
          <div>
            <dt>Rubric / Evaluation</dt>
            <dd>{sourceSummary(view, "rubric_evaluation")}</dd>
          </div>
          <div>
            <dt>Review / Release</dt>
            <dd>{sourceSummary(view, "review_release")}</dd>
          </div>
        </dl>
        {materialIssues.filter((issue) => issue.severity === "warning").length > 0 ? (
          <ul>
            {materialIssues
              .filter((issue) => issue.severity === "warning")
              .map((issue) => (
                <li key={`${issue.category}-${issue.reason_code}`}>
                  {issue.category}: {issue.recovery_hint}
                </li>
              ))}
          </ul>
        ) : null}
      </Dialog>

      <Dialog
        open={newCohortOpen}
        title="Create a new cohort to make this change"
        cancelLabel="Cancel"
        hideConfirm
        onCancel={() => {
          setNewCohortOpen(false);
        }}
        onConfirm={() => {
          setNewCohortOpen(false);
        }}
      >
        <p>
          This cohort&apos;s baseline is immutable. To change a material value, create a new Activity revision and
          cohort. Existing Enrollments, Sessions, Evidence, Evaluations, and Results stay linked to this baseline.
          Creating a new cohort is not available in this slice.
        </p>
      </Dialog>

      <Dialog
        open={blocker.state === "blocked"}
        title="Unsaved changes"
        confirmLabel="Leave without saving"
        cancelLabel="Stay on page"
        confirmVariant="danger"
        tertiaryLabel="Save draft and leave"
        tertiaryDisabled={pending !== null}
        onTertiary={() => {
          setPending("save");
          setStatus("Saving draft…");
          void saveDraft(activityId, title, view.revision_number)
            .then((next) => {
              setView(next);
              setTitle(next.title);
              setError(null);
              blocker.proceed?.();
            })
            .catch((cause: unknown) => {
              if (isAccessLoss(cause)) {
                applyAccessLoss();
                return;
              }

              const message = cause instanceof Error ? cause.message : "The draft could not be saved.";
              setError(message === "This draft changed" ? "This draft changed" : "The draft could not be saved.");
              blocker.reset?.();
              titleInputRef.current?.focus();
            })
            .finally(() => {
              setPending(null);
            });
        }}
        onCancel={() => {
          blocker.reset?.();
        }}
        onConfirm={() => {
          blocker.proceed?.();
        }}
      >
        <p>Your latest changes have not been saved. Save them before leaving this page, or leave and discard them.</p>
      </Dialog>
    </div>
  );
}
