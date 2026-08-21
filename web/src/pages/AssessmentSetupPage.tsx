import { useEffect, useId, useRef, useState } from "react";
import { useBlocker, useParams } from "react-router-dom";
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
  sources?: Array<{ category: string; source_id: string; version_id: string; content_digest: string }>;
}

interface AssessmentSetupPageProps {
  loadSetup: (activityId: string) => Promise<AssessmentSetupView>;
  saveDraft: (activityId: string, title: string, expectedRevision: number) => Promise<AssessmentSetupView>;
  checkReadiness: (activityId: string) => Promise<AssessmentSetupView>;
  activateCohort?: (activityId: string, view: AssessmentSetupView) => Promise<AssessmentSetupView>;
}

type PendingAction = "load" | "save" | "ready" | "activate" | null;

function isAccessLoss(cause: unknown) {
  return cause instanceof Error && /access changed|expired/i.test(cause.message);
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
      </StatusPanel>
    );
  }

  if (error && !view || !view || !activityId) {
    return (
      <StatusPanel title="Access denied" variant="danger">
        <p>{error ?? "This setup is unavailable."}</p>
      </StatusPanel>
    );
  }

  const canActivate = view.permitted_actions.includes("activate_cohort") && view.overall_severity === "ready";
  const blocked = view.overall_severity === "blocked";
  const ready = view.overall_severity === "ready";

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
                  setView(next);
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

      {view.sources?.length ? (
        <section className="page-section" aria-labelledby="sources-heading">
          <h2 id="sources-heading">Selected source revisions</h2>
          <ul aria-label="Selected source revisions">
            {view.sources.map((source) => (
              <li key={`${source.category}-${source.version_id}`}>
                {source.category}: {source.version_id}
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <section className="page-section" aria-labelledby={readinessHeadingId}>
        <h2 ref={readinessHeadingRef} id={readinessHeadingId} tabIndex={-1}>
          {blocked ? "Readiness blocked" : ready ? "Ready to activate" : "Readiness"}
        </h2>
        <p>Memory default: Stable, approved reads {view.memory_mode}.</p>
        {view.issues?.length ? (
          <ul aria-label="Readiness issues">
            {view.issues.map((issue) => (
              <li key={`${issue.category}-${issue.reason_code}`}>
                <Alert variant={issue.severity === "blocked" ? "danger" : "info"} title={issue.category}>
                  {issue.recovery_hint}
                </Alert>
              </li>
            ))}
          </ul>
        ) : (
          <p>Readiness has not been checked for this saved revision.</p>
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
            <Alert variant="success" title="Activated baseline">
              <h3 ref={successHeadingRef} tabIndex={-1}>Cohort activated</h3>
              <p>Baseline digest {view.baseline_digest ?? "is recorded"}. Assign Participants is omitted until a production Enrollment destination exists.</p>
            </Alert>
            <Button type="button" variant="secondary" onClick={() => { setNewCohortOpen(true); }}>
              Change assessment configuration
            </Button>
          </>
        ) : (
          <Button type="button" disabled={!canActivate || pending !== null} onClick={() => {
            setConfirmOpen(true);
          }}>
            Activate cohort
          </Button>
        )}
      </section>

      <Dialog
        open={confirmOpen}
        title="Activate this empty cohort?"
        confirmLabel="Confirm activation"
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
              setStatus("Cohort activated");
            })
            .catch((cause: unknown) => {
              if (isAccessLoss(cause)) {
                applyAccessLoss();
                return;
              }

              setError("The cohort was not activated");
              setStatus("Checking activation status");
            })
            .finally(() => {
              setPending(null);
            });
        }}
      >
        <p>Activation freezes the fairness baseline. This cannot be edited in place.</p>
      </Dialog>

      <Dialog
        open={newCohortOpen}
        title="Create a new cohort to make this change"
        confirmLabel="Create new cohort"
        cancelLabel="Cancel"
        confirmDisabled
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
        </p>
      </Dialog>

      <Dialog
        open={blocker.state === "blocked"}
        title="Unsaved changes"
        confirmLabel="Leave without saving"
        cancelLabel="Stay on page"
        confirmVariant="danger"
        onCancel={() => {
          blocker.reset?.();
        }}
        onConfirm={() => {
          blocker.proceed?.();
        }}
      >
        <p>Leaving discards local title changes that are not part of a saved revision.</p>
      </Dialog>
    </div>
  );
}
