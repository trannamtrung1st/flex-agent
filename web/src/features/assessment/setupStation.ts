import type { AssessmentSetupView } from "../../api/production-assessment";
import type { StateIndicatorVariant } from "../../design-system";

export type SetupTrackId = "local" | "draft" | "readiness" | "cohort";

export type SetupTrack = {
  id: SetupTrackId;
  term: string;
  value: string;
  now: boolean;
  variant: StateIndicatorVariant;
  solid: boolean;
};

export type SetupStationPending = "load" | "save" | "ready" | "activate" | null;

export function isSetupTitleDirty(view: AssessmentSetupView, title: string) {
  return title !== view.title;
}

export function setupMemoryCopy(mode: string) {
  if (mode.trim().toLowerCase() === "dynamic") {
    return "Dynamic";
  }
  return "Stable — new long-term learning disabled";
}

export function setupBlockers(view: AssessmentSetupView) {
  return (view.issues ?? []).filter((issue) => issue.severity === "blocker");
}

export function setupIsReady(view: AssessmentSetupView) {
  return view.issues !== undefined && setupBlockers(view).length === 0;
}

export function setupTracks(
  view: AssessmentSetupView,
  title: string,
  pending: SetupStationPending,
): SetupTrack[] {
  const dirty = isSetupTitleDirty(view, title);
  const blockers = setupBlockers(view);
  const readinessChecked = view.issues !== undefined;
  const ready = view.has_activated_cohort || (readinessChecked && blockers.length === 0);
  const warnings = !view.has_activated_cohort
    && ready
    && (view.issues ?? []).some((issue) => issue.severity === "warning");
  const now = setupNowTrack(view, dirty, blockers.length > 0, ready, pending);

  const readinessValue = blockers.length > 0
    ? "Blocked"
    : warnings
      ? "Warnings"
      : ready
        ? "Ready"
        : "Not checked";

  return [
    {
      id: "local",
      term: "Local",
      value: dirty ? "Unsaved" : "Seated",
      now: now === "local",
      variant: dirty ? "live" : "rest",
      solid: dirty,
    },
    {
      id: "draft",
      term: "Draft",
      value: `Revision ${view.revision_number}`,
      now: now === "draft",
      variant: "rest",
      solid: false,
    },
    {
      id: "readiness",
      term: "Readiness",
      value: readinessValue,
      now: now === "readiness",
      variant: blockers.length > 0 ? "live" : ready ? "sealed" : "dim",
      solid: ready,
    },
    {
      id: "cohort",
      term: "Cohort",
      value: view.has_activated_cohort ? "Activated" : "Unactivated",
      now: now === "cohort",
      variant: view.has_activated_cohort ? "sealed" : now === "cohort" ? "live" : "rest",
      solid: view.has_activated_cohort,
    },
  ];
}

export function setupNextAction(
  view: AssessmentSetupView,
  title: string,
  pending: SetupStationPending,
): string {
  const dirty = isSetupTitleDirty(view, title);
  const blockers = setupBlockers(view);
  const ready = setupIsReady(view);
  const canSave = view.permitted_actions.includes("save_draft") && !view.has_activated_cohort;
  const canReady = view.permitted_actions.includes("check_readiness") && !view.has_activated_cohort;
  const canActivate = view.permitted_actions.includes("activate_cohort") && !view.has_activated_cohort && ready && !dirty;

  if (view.has_activated_cohort) {
    return "This cohort baseline is immutable. Assignment uses the authorized Participants destination.";
  }
  if (pending === "activate") {
    return "Activation is reconciling. Wait for authoritative state.";
  }
  if (pending === "ready") {
    return "Checking readiness for this saved revision.";
  }
  if (pending === "save") {
    return "Saving this draft as an Activity revision.";
  }
  if (canActivate) {
    return "Activate this cohort. The browser is not activation authority.";
  }
  if (blockers.length > 0) {
    return `Correct readiness blockers on revision ${view.revision_number}, then check again.`;
  }
  if (dirty && canSave) {
    return "Save this draft, then check readiness.";
  }
  if (canReady) {
    return `Check readiness on revision ${view.revision_number}, then activate this cohort.`;
  }
  if (canSave) {
    return "Save a draft, then check readiness.";
  }
  return "Setup actions are not available for this revision.";
}

function setupNowTrack(
  view: AssessmentSetupView,
  dirty: boolean,
  blocked: boolean,
  ready: boolean,
  pending: SetupStationPending,
): SetupTrackId {
  if (view.has_activated_cohort) return "cohort";
  if (pending === "activate") return "cohort";
  if (pending === "ready") return "readiness";
  if (pending === "save" || dirty) return "local";
  if (blocked) return "readiness";
  if (ready && view.permitted_actions.includes("activate_cohort")) return "cohort";
  if (view.permitted_actions.includes("check_readiness")) return "readiness";
  return "draft";
}
