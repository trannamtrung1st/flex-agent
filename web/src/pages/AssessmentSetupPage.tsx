import { useEffect, useId, useState } from "react";
import { useParams } from "react-router-dom";
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
  revision_number: number;
  memory_mode: string;
  has_activated_cohort: boolean;
  permitted_actions: string[];
  overall_severity?: string;
  issues?: Array<{ category: string; severity: string; reason_code: string; recovery_hint: string }>;
  baseline_digest?: string;
}

interface AssessmentSetupPageProps {
  loadSetup: (activityId: string) => Promise<AssessmentSetupView>;
  saveDraft: (activityId: string, title: string, expectedRevision: number) => Promise<AssessmentSetupView>;
  checkReadiness: (activityId: string) => Promise<AssessmentSetupView>;
  activateCohort?: (activityId: string) => Promise<AssessmentSetupView>;
}

export function AssessmentSetupPage({
  loadSetup,
  saveDraft,
  checkReadiness,
  activateCohort,
}: AssessmentSetupPageProps) {
  const { activityId } = useParams<{ activityId: string }>();
  const titleId = useId();
  const [view, setView] = useState<AssessmentSetupView | null>(null);
  const [title, setTitle] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState<"load" | "save" | "ready" | "activate" | null>("load");
  const [confirmOpen, setConfirmOpen] = useState(false);

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
      .catch(() => {
        if (active) {
          setError("This setup is unavailable.");
          setPending(null);
        }
      });

    return () => {
      active = false;
    };
  }, [activityId, loadSetup]);

  if (pending === "load") {
    return <ProtectedLoading label="Loading assessment setup…" />;
  }

  if (error || !view || !activityId) {
    return (
      <StatusPanel title="Access denied" variant="danger">
        <p>{error ?? "This setup is unavailable."}</p>
      </StatusPanel>
    );
  }

  const canActivate = view.permitted_actions.includes("activate_cohort") && view.overall_severity === "ready";
  const dirty = title !== view.title;

  return (
    <div>
      <header className="page-header">
        <h1>Setup and readiness</h1>
        <p>Save an expected Campaign revision, check readiness, then deliberately activate an empty Cohort.</p>
      </header>

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
              void saveDraft(activityId, title, view.revision_number)
                .then((next) => {
                  setView(next);
                  setTitle(next.title);
                  setError(null);
                })
                .catch(() => {
                  setError("The draft could not be saved.");
                })
                .finally(() => {
                  setPending(null);
                });
            }}
          >
            <label htmlFor={titleId}>Campaign title</label>
            <input
              id={titleId}
              name="title"
              value={title}
              onChange={(event) => {
                setTitle(event.target.value);
              }}
              required
              maxLength={200}
            />
            {error ? <ErrorSummary title="Correct the following" errors={[error]} /> : null}
            <Button type="submit" disabled={pending !== null}>
              {pending === "save" ? "Saving…" : "Save draft"}
            </Button>
          </form>
        </CardBody>
      </Card>

      <section className="page-section" aria-labelledby="readiness-heading">
        <h2 id="readiness-heading">Readiness</h2>
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
        <Button
          type="button"
          disabled={pending !== null || dirty}
          onClick={() => {
            setPending("ready");
            void checkReadiness(activityId)
              .then((next) => {
                setView(next);
                setError(null);
              })
              .catch(() => {
                setError("Readiness could not be checked.");
              })
              .finally(() => {
                setPending(null);
              });
          }}
        >
          {pending === "ready" ? "Checking…" : "Check readiness"}
        </Button>
      </section>

      <section className="page-section" aria-labelledby="activation-heading">
        <h2 id="activation-heading">Activation</h2>
        {view.has_activated_cohort ? (
          <Alert variant="success" title="Cohort activated">
            Baseline digest {view.baseline_digest ?? "is recorded"}. Assign Participants is omitted until a production
            Enrollment destination exists.
          </Alert>
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
          void activateCohort(activityId)
            .then((next) => {
              setView(next);
              setConfirmOpen(false);
            })
              .catch(() => {
                setError("Activation did not complete. Reconcile before retrying.");
              })
              .finally(() => {
                setPending(null);
              });
        }}
      >
        <p>Activation freezes the fairness baseline. This cannot be edited in place.</p>
      </Dialog>
    </div>
  );
}
