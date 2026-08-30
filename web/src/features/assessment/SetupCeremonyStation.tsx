import {
  Alert,
  EllipsisKey,
  ErrorSummary,
  FieldInput,
  FormField,
  CAMPAIGN_TITLE_PLACEHOLDER,
  Key,
  KeyGroup,
  PlateFoot,
  ReadoutGrid,
  ReadoutGridField,
  ReadoutGridRow,
  Stack,
  StateReadout,
} from "../../design-system";
import { cx } from "../../lib/cx";
import type { AssessmentSetupView } from "../../api/production-assessment";
import {
  isSetupTitleDirty,
  setupBlockers,
  setupIsReady,
  setupMemoryCopy,
  setupTracks,
  type SetupStationPending,
} from "./setupStation";

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
    <ReadoutGrid label="Setup tracks" columns={4} className="assignment-instruments">
      <ReadoutGridRow label="Local through cohort">
        {tracks.map((track) => (
          <ReadoutGridField key={track.id} term={track.term}>
            <StateReadout
              variant={track.variant}
              solid={track.solid}
              label={track.value}
              className={track.now ? "setup-track-now" : undefined}
            />
          </ReadoutGridField>
        ))}
      </ReadoutGridRow>
    </ReadoutGrid>
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
    <EllipsisKey variant="activate" disabled={busy} onClick={onRequestActivate}>
      Activate cohort
    </EllipsisKey>
  ) : view.has_activated_cohort && view.cohort_id ? (
    <Key variant="open" to={`/activities/${view.activity_id}/cohorts/${view.cohort_id}/enrollments`}>
      Assign Participants
    </Key>
  ) : null;

  return (
    <Stack gap="none" className={cx("setup-ceremony", view.has_activated_cohort && "is-frozen")}>
      <SetupCeremonyTracks view={view} title={title} pending={pending} />
      <Stack gap="4" className="create-ceremony__scroll">
        {error ? (
          <ErrorSummary headingId={`${titleId}-summary`} title="Correct these items" errors={[error]} />
        ) : null}
        {view.has_activated_cohort ? (
          <Alert variant="success" title="Cohort activated">
            <p>Baseline {view.baseline_digest ?? "recorded"}. Verification {view.verification_status ?? "pending"}.</p>
          </Alert>
        ) : null}
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
        <p className="setup-ceremony__memory">{setupMemoryCopy(view.memory_mode)}</p>
        {blockers.length > 0 ? (
          <Alert variant="warning" title="Readiness blockers">
            <ul>
              {blockers.map((issue) => (
                <li key={`${issue.category}-${issue.reason_code}`}>{issue.recovery_hint}</li>
              ))}
            </ul>
          </Alert>
        ) : null}
      </Stack>
      {draftKeys || primary ? (
        <PlateFoot
          className="setup-ceremony__foot"
          arrangement={primary && draftKeys ? "split" : primary ? "end" : "start"}
          secondary={primary && draftKeys ? draftKeys : undefined}
          primary={primary && draftKeys ? primary : undefined}
        >
          {primary && draftKeys ? null : primary ?? draftKeys}
        </PlateFoot>
      ) : null}
    </Stack>
  );
}
