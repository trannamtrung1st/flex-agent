import {
  Alert,
  EllipsisKey,
  ErrorSummary,
  FieldInput,
  FormField,
  FormSection,
  Grid,
  Key,
  KeyGroup,
  ReadoutGridField,
  ReadoutGridRow,
  Stack,
} from "../../design-system";
import { CAMPAIGN_TITLE_PLACEHOLDER } from "../../content/fieldCopy";
import { AssignmentInstrumentGrid } from "../../components/work/AssignmentInstrumentGrid";
import { SetupCeremony, SetupCeremonyFoot, SetupCeremonyScroll } from "../../components/work/SetupCeremony";
import { SetupTrackReadout } from "./SetupTrackReadout";
import type { AssessmentSetupView } from "../../api/production-assessment";
import { sourceCategoryLabel } from "./campaignCreatePresentation";
import {
  isSetupTitleDirty,
  setupBlockers,
  setupCeremonySummary,
  setupCapabilityCopy,
  setupDurationLabel,
  setupInstantLabel,
  setupIsReady,
  setupMemoryCopy,
  setupOpaqueCaption,
  setupSourceCaption,
  setupTracks,
  SETUP_FROZEN_PLACEHOLDER,
  SETUP_RESOLVED_NOTE,
  SETUP_UNBOUND,
  SETUP_UNSEATED,
  type SetupStationPending,
} from "./setupStation";

function FrozenResolvedField({
  id,
  label,
  value,
}: {
  id: string;
  label: string;
  value: string;
}) {
  return (
    <FormField id={id} layout="stack" label={label}>
      {(control) => (
        <FieldInput
          {...control}
          frozen
          width="wide"
          value={value}
          placeholder={SETUP_FROZEN_PLACEHOLDER}
        />
      )}
    </FormField>
  );
}

export function SetupCeremonyTracks({
  view,
  title,
  pending,
}: {
  view: AssessmentSetupView;
  title: string;
  pending: SetupStationPending;
}) {
  const tracks = setupTracks(view, title, pending);
  return (
    <AssignmentInstrumentGrid label="Setup tracks" columns={4}>
      <ReadoutGridRow label="Local through cohort">
        {tracks.map((track) => (
          <ReadoutGridField key={track.id} term={track.term}>
            <SetupTrackReadout
              variant={track.variant}
              solid={track.solid}
              label={track.value}
              now={track.now}
            />
          </ReadoutGridField>
        ))}
      </ReadoutGridRow>
    </AssignmentInstrumentGrid>
  );
}

export function SetupCeremonyStation({
  view,
  title,
  pending,
  error,
  titleId,
  onTitleChange,
  onSave,
  onCheck,
  onRequestActivate,
}: {
  view: AssessmentSetupView;
  title: string;
  pending: SetupStationPending;
  error: string | null;
  titleId: string;
  onTitleChange: (value: string) => void;
  onSave: () => void;
  onCheck: () => void;
  onRequestActivate: () => void;
}) {
  const busy = pending !== null;
  const canSave = view.permitted_actions.includes("save_draft") && !view.has_activated_cohort;
  const canReady = view.permitted_actions.includes("check_readiness") && !view.has_activated_cohort;
  const canActivate = view.permitted_actions.includes("activate_cohort")
    && !view.has_activated_cohort
    && setupIsReady(view)
    && !isSetupTitleDirty(view, title);
  const blockers = setupBlockers(view);
  const summary = setupCeremonySummary(titleId, error, blockers);
  const zone = view.timing?.time_zone_id;

  const draftKeys = canSave || canReady ? (
    <KeyGroup aria-label="Draft actions">
      {canSave ? (
        <EllipsisKey variant="quiet" disabled={busy} onClick={onSave}>
          Save draft
        </EllipsisKey>
      ) : null}
      {canReady ? (
        <EllipsisKey variant="quiet" disabled={busy} onClick={onCheck}>
          Check readiness
        </EllipsisKey>
      ) : null}
    </KeyGroup>
  ) : null;

  const primary = canActivate ? (
    <EllipsisKey variant="activate" size="large" disabled={busy} onClick={onRequestActivate}>
      Activate cohort
    </EllipsisKey>
  ) : view.has_activated_cohort && view.cohort_id ? (
    <Key variant="open" size="large" to={`/activities/${view.activity_id}/cohorts/${view.cohort_id}/enrollments`}>
      Assign Participants
    </Key>
  ) : null;

  return (
    <SetupCeremony frozen={view.has_activated_cohort}>
      <SetupCeremonyTracks view={view} title={title} pending={pending} />
      <SetupCeremonyScroll>
        {summary ? (
          <ErrorSummary headingId={summary.headingId} title={summary.title} errors={summary.errors} />
        ) : null}
        <Stack gap="4">
          {view.has_activated_cohort ? (
            <Alert variant="success" title="Cohort activated">
              <p>Baseline {view.baseline_digest ?? "recorded"}. Verification {view.verification_status ?? "pending"}.</p>
              <p>{SETUP_RESOLVED_NOTE}</p>
            </Alert>
          ) : (
            <Alert variant="info" title={SETUP_RESOLVED_NOTE} />
          )}
          <FormField
            id={titleId}
            layout="stack"
            label="Campaign title"
            hint={
              view.has_activated_cohort
                ? undefined
                : isSetupTitleDirty(view, title)
                  ? "Unsaved changes"
                  : `Saved as revision ${view.revision_number}`
            }
          >
            {(control) => (
              <FieldInput
                {...control}
                placeholder={CAMPAIGN_TITLE_PLACEHOLDER}
                value={title}
                width="wide"
                frozen={view.has_activated_cohort}
                disabled={view.has_activated_cohort ? false : !canSave || busy}
                onChange={(event) => onTitleChange(event.target.value)}
              />
            )}
          </FormField>
        </Stack>
        <FormSection legend="Task and Submission requirements">
          <Grid gap="4" minItemWidth="control">
            <FrozenResolvedField id={`${titleId}-task`} label="Task" value={view.task_title?.trim() || SETUP_UNBOUND} />
            <FrozenResolvedField
              id={`${titleId}-task-submission`}
              label={sourceCategoryLabel("task_submission")}
              value={setupSourceCaption(view, "task_submission")}
            />
          </Grid>
        </FormSection>
        <FormSection legend="Agent and Harness">
          <Grid gap="4" minItemWidth="control">
            <FrozenResolvedField id={`${titleId}-agent`} label={sourceCategoryLabel("agent")} value={setupSourceCaption(view, "agent")} />
            <FrozenResolvedField id={`${titleId}-harness`} label={sourceCategoryLabel("harness")} value={setupSourceCaption(view, "harness")} />
          </Grid>
        </FormSection>
        <FormSection legend="Assessment behavior">
          <Grid gap="4" minItemWidth="control">
            <FrozenResolvedField
              id={`${titleId}-policy`}
              label={sourceCategoryLabel("organization_policy")}
              value={setupSourceCaption(view, "organization_policy")}
            />
            <FrozenResolvedField
              id={`${titleId}-workflow`}
              label={sourceCategoryLabel("workflow")}
              value={setupSourceCaption(view, "workflow")}
            />
            <FrozenResolvedField
              id={`${titleId}-follow-up`}
              label={sourceCategoryLabel("adaptive_follow_up")}
              value={setupSourceCaption(view, "adaptive_follow_up")}
            />
            <FrozenResolvedField
              id={`${titleId}-rubric`}
              label={sourceCategoryLabel("rubric_evaluation")}
              value={setupSourceCaption(view, "rubric_evaluation")}
            />
            <FrozenResolvedField
              id={`${titleId}-model`}
              label={sourceCategoryLabel("model_deployment")}
              value={setupSourceCaption(view, "model_deployment")}
            />
            <FrozenResolvedField
              id={`${titleId}-capability-source`}
              label={sourceCategoryLabel("capability")}
              value={setupSourceCaption(view, "capability")}
            />
          </Grid>
        </FormSection>
        <FormSection legend="Timing and Attempts">
          <Grid gap="4" minItemWidth="compact">
            <FrozenResolvedField id={`${titleId}-timezone`} label="Timezone" value={view.timing?.time_zone_id || SETUP_UNSEATED} />
            <FrozenResolvedField
              id={`${titleId}-attempts`}
              label="Attempt limit"
              value={view.timing ? String(view.timing.attempt_limit) : SETUP_UNSEATED}
            />
            <FrozenResolvedField
              id={`${titleId}-starts`}
              label="Starts"
              value={setupInstantLabel(view.timing?.starts_at_utc, zone)}
            />
            <FrozenResolvedField
              id={`${titleId}-ends`}
              label="Ends"
              value={setupInstantLabel(view.timing?.ends_at_utc, zone)}
            />
            <FrozenResolvedField
              id={`${titleId}-deadline`}
              label="Deadline"
              value={setupInstantLabel(view.timing?.deadline_utc, zone)}
            />
            <FrozenResolvedField
              id={`${titleId}-duration`}
              label="Per-attempt duration"
              value={setupDurationLabel(view.timing?.per_attempt_duration_seconds)}
            />
            {view.timing?.warning_approaching_remaining_seconds != null ? (
              <FrozenResolvedField
                id={`${titleId}-warning-approaching`}
                label="Approaching warning"
                value={setupDurationLabel(view.timing.warning_approaching_remaining_seconds)}
              />
            ) : null}
            {view.timing?.warning_imminent_remaining_seconds != null ? (
              <FrozenResolvedField
                id={`${titleId}-warning-imminent`}
                label="Imminent warning"
                value={setupDurationLabel(view.timing.warning_imminent_remaining_seconds)}
              />
            ) : null}
          </Grid>
        </FormSection>
        <FormSection legend="Memory and capabilities">
          <Grid gap="4" minItemWidth="control">
            <FrozenResolvedField id={`${titleId}-memory`} label="Memory" value={setupMemoryCopy(view.memory_mode)} />
            <FrozenResolvedField id={`${titleId}-disabled-capabilities`} label="Disabled capabilities" value={setupCapabilityCopy(view)} />
          </Grid>
        </FormSection>
        <FormSection legend="Review and Release requirements">
          <FrozenResolvedField
            id={`${titleId}-review`}
            label={sourceCategoryLabel("review_release")}
            value={setupSourceCaption(view, "review_release")}
          />
        </FormSection>
        <FormSection legend="Cohort">
          <Grid gap="4" minItemWidth="control">
            <FrozenResolvedField
              id={`${titleId}-cohort-state`}
              label="Cohort state"
              value={view.has_activated_cohort ? "Activated" : "Unactivated"}
            />
            <FrozenResolvedField
              id={`${titleId}-baseline`}
              label="Baseline"
              value={setupOpaqueCaption(view.baseline_digest)}
            />
            <FrozenResolvedField
              id={`${titleId}-verification`}
              label="Verification"
              value={view.verification_status?.trim() || SETUP_UNSEATED}
            />
          </Grid>
        </FormSection>
      </SetupCeremonyScroll>
      {draftKeys || primary ? (
        <SetupCeremonyFoot
          arrangement={primary && draftKeys ? "split" : primary ? "end" : "start"}
          secondary={primary && draftKeys ? draftKeys : undefined}
          primary={primary && draftKeys ? primary : undefined}
        >
          {primary && draftKeys ? null : primary ?? draftKeys}
        </SetupCeremonyFoot>
      ) : null}
    </SetupCeremony>
  );
}
