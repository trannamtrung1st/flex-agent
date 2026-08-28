import { useEffect, useId, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import {
  type AssessmentSetupView,
  isAssessmentAccessLoss,
} from "../api/production-assessment";
import {
  Alert,
  BackKey,
  CeremonyDialog,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  ErrorSummary,
  FieldInput,
  FormField,
  Inline,
  Key,
  OperateArea,
  ReadoutGrid,
  ReadoutGridField,
  ReadoutGridRow,
  StateReadout,
  WaitPanel,
  WorkWell,
  WorkWellHead,
  WorkWellSection,
} from "../design-system";
import { CeremonyArea, CeremonyEmpty } from "../components/shell/SessionChrome";

export interface AssessmentSetupPageProps {
  loadSetup: (activityId: string) => Promise<AssessmentSetupView>;
  saveDraft: (activityId: string, title: string, expectedRevision: number) => Promise<AssessmentSetupView>;
  checkReadiness: (activityId: string) => Promise<AssessmentSetupView>;
  activateCohort: (activityId: string, view: AssessmentSetupView) => Promise<AssessmentSetupView>;
}

export function AssessmentSetupPage({
  loadSetup,
  saveDraft,
  checkReadiness,
  activateCohort,
}: AssessmentSetupPageProps) {
  const { activityId = "" } = useParams();
  const titleId = useId();
  const confirmId = useId();
  const [view, setView] = useState<AssessmentSetupView | null>(null);
  const [title, setTitle] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState<"load" | "save" | "ready" | "activate" | null>("load");
  const [confirmOpen, setConfirmOpen] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void loadSetup(activityId)
      .then((next) => {
        if (cancelled) return;
        setView(next);
        setTitle(next.title);
        setError(null);
        setPending(null);
      })
      .catch((caught: unknown) => {
        if (cancelled) return;
        if (isAssessmentAccessLoss(caught)) throw caught;
        setError("Setup is not available.");
        setPending(null);
      });
    return () => {
      cancelled = true;
    };
  }, [activityId, loadSetup]);

  const canSave = view?.permitted_actions.includes("save_draft") === true && !view.has_activated_cohort;
  const canReady = view?.permitted_actions.includes("check_readiness") === true && !view.has_activated_cohort;
  const canActivate = view?.permitted_actions.includes("activate_cohort") === true && !view.has_activated_cohort;

  const blockers = useMemo(
    () => (view?.issues ?? []).filter((issue) => issue.severity === "blocker"),
    [view],
  );

  if (pending === "load" && !view) {
    return (
      <CeremonyArea label="Setup" title="Setup and readiness">
        <WaitPanel label="Loading setup…" />
      </CeremonyArea>
    );
  }

  if (!view) {
    return (
      <CeremonyArea label="Setup unavailable" title="Setup unavailable" danger>
        <CeremonyEmpty note={error ?? "Setup is not available."}>
          <Key variant="open" to="/activities">Return to Activities</Key>
        </CeremonyEmpty>
      </CeremonyArea>
    );
  }

  return (
    <OperateArea
      className="workspace-area work-plane"
      frameClassName="record-frame"
      label="Setup and readiness"
      title={view.has_activated_cohort ? "Activated cohort" : "Setup and readiness"}
      description={view.has_activated_cohort
        ? "This cohort baseline is immutable. Assignment uses the authorized Participants destination."
        : "Save a draft, check readiness, then deliberately activate one cohort. The browser is not activation authority."}
      back={<BackKey to="/activities" label="Activities" />}
      context={(
        <ReadoutGrid label="Campaign identity" columns={4} className="assignment-instruments">
          <ReadoutGridRow label="Identity">
            <ReadoutGridField term="Campaign">{view.title}</ReadoutGridField>
            <ReadoutGridField term="Revision">{String(view.revision_number)}</ReadoutGridField>
            <ReadoutGridField term="Memory">{view.memory_mode}</ReadoutGridField>
            <ReadoutGridField term="Record">
              <StateReadout
                variant={view.has_activated_cohort ? "sealed" : "rest"}
                solid={view.has_activated_cohort}
                label={view.has_activated_cohort ? "Activated" : "Draft"}
                className="assignment-record"
                labelClassName="assignment-record-label"
              />
            </ReadoutGridField>
          </ReadoutGridRow>
        </ReadoutGrid>
      )}
    >
      <div className="assignment-station">
        {error ? (
          <ErrorSummary headingId={`${titleId}-summary`} title="Correct these items" errors={[error]} />
        ) : null}
        {view.has_activated_cohort ? (
          <Alert variant="success" title="Cohort activated">
            <p>Baseline {view.baseline_digest ?? "recorded"}. Verification {view.verification_status ?? "pending"}.</p>
            {view.cohort_id ? (
              <Key variant="open" to={`/activities/${view.activity_id}/cohorts/${view.cohort_id}/enrollments`}>
                Assign Participants
              </Key>
            ) : null}
          </Alert>
        ) : null}
        <WorkWell
          live={false}
          label="Configuration"
          head={<WorkWellHead title="Configuration" ident="Draft title remains local until the server accepts it." />}
          foot={
            <Inline gap="2">
              {canSave ? (
                <Key
                  variant="quiet"
                  disabled={pending !== null}
                  onClick={() => {
                    setPending("save");
                    void saveDraft(activityId, title, view.revision_number)
                      .then((next) => {
                        setView(next);
                        setTitle(next.title);
                        setError(null);
                      })
                      .catch((caught: unknown) => {
                        if (isAssessmentAccessLoss(caught)) throw caught;
                        setError("This draft could not be saved. Reconcile before retrying.");
                      })
                      .finally(() => setPending(null));
                  }}
                >
                  Save draft
                </Key>
              ) : null}
              {canReady ? (
                <Key
                  variant="quiet"
                  disabled={pending !== null}
                  onClick={() => {
                    setPending("ready");
                    void checkReadiness(activityId)
                      .then((next) => {
                        setView(next);
                        setError(null);
                      })
                      .catch((caught: unknown) => {
                        if (isAssessmentAccessLoss(caught)) throw caught;
                        setError("Readiness could not be checked.");
                      })
                      .finally(() => setPending(null));
                  }}
                >
                  Check readiness
                </Key>
              ) : null}
              {canActivate ? (
                <Key variant="transmit" disabled={pending !== null} onClick={() => setConfirmOpen(true)}>
                  Activate cohort
                </Key>
              ) : null}
            </Inline>
          }
        >
          <WorkWellSection>
            <FormField id={titleId} layout="stack" label="Campaign title">
              {(control) => (
                <FieldInput
                  {...control}
                  value={title}
                  width="wide"
                  disabled={!canSave || pending !== null}
                  onChange={(event) => setTitle(event.target.value)}
                />
              )}
            </FormField>
            {blockers.length > 0 ? (
              <Alert variant="warning" title="Readiness blockers">
                <ul>
                  {blockers.map((issue) => (
                    <li key={`${issue.category}-${issue.reason_code}`}>{issue.recovery_hint}</li>
                  ))}
                </ul>
              </Alert>
            ) : null}
          </WorkWellSection>
        </WorkWell>
      </div>
      <CeremonyDialog open={confirmOpen} onClose={() => setConfirmOpen(false)} labelledBy={confirmId}>
        <DialogPlate>
          <DialogPlateHead title="Activate this cohort?" titleId={confirmId} />
          <DialogPlateBody>
            <p>Activation freezes the baseline. This cannot be undone from the browser.</p>
          </DialogPlateBody>
          <DialogPlateFooter>
            <Key variant="quiet" onClick={() => setConfirmOpen(false)}>Cancel</Key>
            <Key
              variant="transmit"
              disabled={pending !== null}
              onClick={() => {
                setPending("activate");
                void activateCohort(activityId, view)
                  .then((next) => {
                    setView(next);
                    setConfirmOpen(false);
                    setError(null);
                  })
                  .catch((caught: unknown) => {
                    if (isAssessmentAccessLoss(caught)) throw caught;
                    setError("Activation did not complete. Reconcile before retrying.");
                    setConfirmOpen(false);
                  })
                  .finally(() => setPending(null));
              }}
            >
              Activate cohort
            </Key>
          </DialogPlateFooter>
        </DialogPlate>
      </CeremonyDialog>
    </OperateArea>
  );
}
