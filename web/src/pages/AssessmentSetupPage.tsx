import { useCallback, useEffect, useId, useRef, useState } from "react";
import { flushSync } from "react-dom";
import { useBlocker, useBeforeUnload, useParams, type BlockerFunction } from "react-router-dom";
import {
  type AssessmentSetupView,
  isAssessmentAccessLoss,
} from "../api/production-assessment";
import {
  BackKey,
  CeremonyDialog,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  Key,
  OperateArea,
  usePushToast,
} from "../design-system";
import { CeremonyArea, CeremonyUnavailable, CeremonyWait } from "../components/shell/SessionChrome";
import { SetupCeremonyStation } from "../features/assessment/SetupCeremonyStation";
import { SetupUnsavedLeaveDialog } from "../features/assessment/SetupUnsavedLeaveDialog";
import { focusSetupSummary, isSetupTitleDirty, setupBlockers, setupNextAction } from "../features/assessment/setupStation";

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
  const leaveId = useId();
  const [view, setView] = useState<AssessmentSetupView | null>(null);
  const [title, setTitle] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState<"load" | "save" | "ready" | "activate" | null>("load");
  const [confirmOpen, setConfirmOpen] = useState(false);
  const pushToast = usePushToast();

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

  const dirty = view ? isSetupTitleDirty(view, title) : false;
  const canSave = Boolean(view?.permitted_actions.includes("save_draft") && !view?.has_activated_cohort);
  const allowNavigationRef = useRef(false);

  useEffect(() => {
    allowNavigationRef.current = false;
  }, [activityId]);

  const shouldBlockNavigation = useCallback<BlockerFunction>(
    ({ currentLocation, nextLocation }) =>
      !allowNavigationRef.current
      && dirty
      && currentLocation.pathname !== nextLocation.pathname,
    [dirty],
  );
  const blocker = useBlocker(shouldBlockNavigation);
  const blockerRef = useRef(blocker);
  blockerRef.current = blocker;

  useBeforeUnload(
    useCallback((event) => {
      if (!dirty || allowNavigationRef.current) return;
      event.preventDefault();
      // Chromium still requires this IDL setter for the leave prompt.
      // eslint-disable-next-line @typescript-eslint/no-deprecated -- BeforeUnloadEvent.returnValue
      event.returnValue = "";
    }, [dirty]),
  );

  const saveDraftNow = useCallback(async (): Promise<boolean> => {
    if (!view) return false;
    setPending("save");
    try {
      const next = await saveDraft(activityId, title, view.revision_number);
      flushSync(() => {
        setView(next);
        setTitle(next.title);
        setError(null);
      });
      pushToast({ label: "Draft", copy: "This revision is saved." });
      return true;
    } catch (caught: unknown) {
      if (isAssessmentAccessLoss(caught)) throw caught;
      flushSync(() => {
        setError("This draft could not be saved. Reconcile before retrying.");
      });
      focusSetupSummary(titleId);
      return false;
    } finally {
      setPending(null);
    }
  }, [activityId, pushToast, saveDraft, title, titleId, view]);

  const stayOnPage = useCallback(() => {
    allowNavigationRef.current = false;
    const current = blockerRef.current;
    if (current.state === "blocked") {
      current.reset();
    }
  }, []);

  const proceedBlockedNavigation = useCallback(() => {
    queueMicrotask(() => {
      const current = blockerRef.current;
      if (current.state === "blocked") {
        current.proceed();
      }
    });
  }, []);

  const leaveWithoutSaving = useCallback(() => {
    if (!view) return;
    allowNavigationRef.current = true;
    flushSync(() => {
      setTitle(view.title);
    });
    proceedBlockedNavigation();
  }, [proceedBlockedNavigation, view]);

  const saveAndLeave = useCallback(() => {
    void saveDraftNow().then((saved) => {
      const current = blockerRef.current;
      if (saved) {
        allowNavigationRef.current = true;
        proceedBlockedNavigation();
        return;
      }
      allowNavigationRef.current = false;
      if (current.state === "blocked") {
        current.reset();
      }
    });
  }, [proceedBlockedNavigation, saveDraftNow]);

  if (pending === "load" && !view) {
    return (
      <CeremonyArea label="Setup" title="Setup and readiness">
        <CeremonyWait label="Loading setup…" />
      </CeremonyArea>
    );
  }

  if (!view) {
    return (
      <CeremonyUnavailable
        title="Setup unavailable"
        note={error ?? "Setup is not available."}
        danger
        recovery={{ label: "Return to Activities", to: "/activities" }}
      />
    );
  }

  const busy = pending !== null;
  const nextAction = setupNextAction(view, title, pending);

  return (
    <OperateArea
      className="workspace-area work-plane record-plane record-plane--setup"
      frameClassName="record-frame"
      label="Setup and readiness"
      title={view.has_activated_cohort ? "Activated cohort" : "Setup and readiness"}
      description={nextAction}
      back={<BackKey to="/activities" label="Activities" />}
    >
      <SetupCeremonyStation
        view={view}
        title={title}
        pending={pending}
        error={error}
        titleId={titleId}
        onTitleChange={setTitle}
        onSave={() => {
          void saveDraftNow();
        }}
        onCheck={() => {
          setPending("ready");
          void checkReadiness(activityId)
            .then((next) => {
              flushSync(() => {
                setView(next);
                setError(null);
              });
              if (setupBlockers(next).length > 0) {
                focusSetupSummary(titleId);
              }
            })
            .catch((caught: unknown) => {
              if (isAssessmentAccessLoss(caught)) throw caught;
              flushSync(() => {
                setError("Readiness could not be checked.");
              });
              focusSetupSummary(titleId);
            })
            .finally(() => setPending(null));
        }}
        onRequestActivate={() => setConfirmOpen(true)}
      />
      <CeremonyDialog open={confirmOpen} onClose={() => setConfirmOpen(false)} labelledBy={confirmId}>
        <DialogPlate>
          <DialogPlateHead title="Activate this cohort?" titleId={confirmId} />
          <DialogPlateBody>
            <p>Activation freezes the baseline. This cannot be undone from the browser.</p>
          </DialogPlateBody>
          <DialogPlateFooter
            arrangement="split"
            secondary={<Key variant="quiet" onClick={() => setConfirmOpen(false)}>Cancel</Key>}
            primary={
              <Key
                variant="activate"
                size="large"
                disabled={busy}
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
                      flushSync(() => {
                        setError("Activation did not complete. Reconcile before retrying.");
                        setConfirmOpen(false);
                      });
                      focusSetupSummary(titleId);
                    })
                    .finally(() => setPending(null));
                }}
              >
                Activate cohort
              </Key>
            }
          />
        </DialogPlate>
      </CeremonyDialog>
      <SetupUnsavedLeaveDialog
        open={blocker.state === "blocked"}
        busy={busy}
        canSave={canSave}
        titleId={leaveId}
        onClose={stayOnPage}
        onSaveAndLeave={saveAndLeave}
        onLeaveWithoutSaving={leaveWithoutSaving}
      />
    </OperateArea>
  );
}
