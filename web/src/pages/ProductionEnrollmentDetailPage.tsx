import { useCallback, useEffect, useId, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import {
  createEnrollmentIdempotencyKey,
  createProductionEnrollmentClient,
  enrollmentFailureCopy,
  enrollmentOutcomeCopy,
  type EnrollmentDetailV1,
  type EnrollmentTimingV2,
} from "../api/production-enrollment";
import { campaignDeadlineCopy, formatCampaignInstant } from "../lib/campaign-timezone";
import {
  ACCOMMODATION_CONSEQUENCE_COPY,
  ACCOMMODATION_DIMENSION_COPY,
  ACCOMMODATION_STATUS_COPY,
  ELIGIBILITY_COPY,
  ENROLLMENT_TERMINAL_CONSEQUENCE,
  accommodationValueExample,
  accommodationValueHint,
  accommodationCurrentBoundTerm,
  accommodationCurrentUtc,
  enrollmentLifecycleReason,
  enrollmentLifecycleReceipt,
  enrollmentRecordVariant,
  enrollmentStatusCopy,
  pickerValueToUtcInstant,
  utcInstantToPickerValue,
  wordsFromCode,
  type EnrollmentLifecycleOperation,
} from "../lib/enrollment-presentation";
import { AssignmentInstrumentGrid } from "../components/work/AssignmentInstrumentGrid";
import { AssignmentRecordReadout } from "../components/work/AssignmentRecordReadout";
import { AcknowledgmentGate } from "../components/work/AcknowledgmentGate";
import {
  Alert,
  BackKey,
  CeremonyArea,
  CeremonyDialog,
  CeremonyUnavailable,
  CeremonyWait,
  CompactId,
  DateTimePicker,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  EmptyPlate,
  FieldInput,
  FormField,
  Inline,
  InstantReadout,
  Key,
  KeyGroup,
  OperateArea,
  ReadoutGridField,
  ReadoutGridRow,
  ReadoutList,
  Stack,
  usePushToast,
  WorkWell,
  WorkWellHead,
  WorkWellSection,
} from "../design-system";

function campaignInstantCopy(utc: string | undefined, timeZoneId: string) {
  if (!utc) return "—";
  return campaignDeadlineCopy(formatCampaignInstant(utc, timeZoneId));
}

export function ProductionEnrollmentDetailPage() {
  const { activityId = "", cohortId = "", enrollmentId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const confirmId = useId();
  const lifecycleTitleId = useId();
  const valueId = useId();
  const fairnessId = useId();
  const [detail, setDetail] = useState<EnrollmentDetailV1 | null>(null);
  const [timing, setTiming] = useState<EnrollmentTimingV2 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [lifecycleConfirm, setLifecycleConfirm] = useState<"close" | "revoke" | null>(null);
  const [requestedValue, setRequestedValue] = useState("");
  const [fairnessException, setFairnessException] = useState(false);
  const pushToast = usePushToast();

  const reload = useCallback(async () => {
    const [next, nextTiming] = await Promise.all([
      client.getEnrollment(activityId, cohortId, enrollmentId),
      client.getEnrollmentTiming(activityId, cohortId, enrollmentId).catch(() => null),
    ]);
    setDetail(next);
    setTiming(nextTiming);
    setError(null);
  }, [activityId, client, cohortId, enrollmentId]);

  useEffect(() => {
    const signal = { cancelled: false };
    void Promise.all([
      client.getEnrollment(activityId, cohortId, enrollmentId),
      client.getEnrollmentTiming(activityId, cohortId, enrollmentId).catch(() => null),
    ])
      .then(([next, nextTiming]) => {
        if (signal.cancelled) return;
        setDetail(next);
        setTiming(nextTiming);
        setError(null);
      })
      .catch((caught: unknown) => {
        if (!signal.cancelled) {
          setError(enrollmentFailureCopy(caught, "This Enrollment is not available."));
        }
      });
    return () => {
      signal.cancelled = true;
    };
  }, [activityId, client, cohortId, enrollmentId]);

  if (error && !detail) {
    return (
      <CeremonyUnavailable
        title="Enrollment unavailable"
        note={error}
        danger
        recovery={{ label: "Return to Participants", to: `/activities/${activityId}/cohorts/${cohortId}/enrollments` }}
      />
    );
  }

  if (!detail) {
    return (
      <CeremonyArea label="Enrollment" title="Enrollment">
        <CeremonyWait label="Loading Enrollment…" />
      </CeremonyArea>
    );
  }

  const actions = detail.enrollment.permitted_actions;
  const revision = detail.enrollment.revision;
  const record = enrollmentRecordVariant(detail.enrollment.status);
  const statusCopy = enrollmentStatusCopy(detail.enrollment.status);
  const dimension = timing?.permitted_dimensions[0];
  const reasonCategory = timing?.permitted_reason_categories[0];
  const canRequest = Boolean(timing?.policy_available && dimension && reasonCategory);
  const zone = timing?.effective.time_zone_id ?? timing?.baseline.time_zone_id ?? "UTC";
  const eligibility = wordsFromCode(timing?.effective.eligibility_state, ELIGIBILITY_COPY);
  const consequence = wordsFromCode(
    timing?.effective.participant_consequence_code,
    ACCOMMODATION_CONSEQUENCE_COPY,
    "None",
  );
  const dimensionCopy = wordsFromCode(dimension, ACCOMMODATION_DIMENSION_COPY);
  const reasonCopy = wordsFromCode(reasonCategory);
  const durationDimension = dimension === "per_attempt_duration_seconds";
  const currentBound = timing ? accommodationCurrentUtc(dimension, timing.effective) : "";
  const effectiveEnd = campaignInstantCopy(timing?.effective.submission_exclusive_end_utc, zone);
  const baselineDeadline = campaignInstantCopy(timing?.baseline.deadline_utc, timing?.baseline.time_zone_id ?? zone);
  const currentBoundCopy = durationDimension
    ? (currentBound ? `${currentBound} seconds` : "—")
    : campaignInstantCopy(currentBound, zone);
  const selectedUtc = durationDimension
    ? null
    : pickerValueToUtcInstant(requestedValue, zone) ?? (currentBound || null);
  const selectedDisplay = selectedUtc ? campaignInstantCopy(selectedUtc, zone) : "";
  const valueExample = accommodationValueExample(dimension, durationDimension ? undefined : currentBound);
  const valueHint = accommodationValueHint(
    dimension,
    zone,
    selectedDisplay && selectedDisplay !== "—" ? selectedDisplay : undefined,
  );

  function mutate(operation: EnrollmentLifecycleOperation) {
    setPending(true);
    void client.mutate(
      activityId,
      cohortId,
      enrollmentId,
      operation,
      enrollmentLifecycleReason(operation),
      revision,
      createEnrollmentIdempotencyKey(),
    )
      .then((outcome) => {
        if (!outcome.succeeded) {
          setError(enrollmentOutcomeCopy(outcome.outcome_code, "The Enrollment could not be updated."));
          return;
        }
        setLifecycleConfirm(null);
        pushToast(enrollmentLifecycleReceipt(operation));
        return reload();
      })
      .catch((caught: unknown) => {
        setError(enrollmentFailureCopy(caught, "The Enrollment could not be updated."));
      })
      .finally(() => setPending(false));
  }

  function runAccommodation(work: () => Promise<{ succeeded: boolean; outcome_code: string }>, failed: string) {
    setPending(true);
    void work()
      .then((outcome) => {
        if (!outcome.succeeded) {
          setError(enrollmentOutcomeCopy(outcome.outcome_code, failed));
          return;
        }
        pushToast({ label: "Accommodation", copy: "The request was recorded." });
        return reload();
      })
      .catch((caught: unknown) => {
        setError(enrollmentFailureCopy(caught, failed));
      })
      .finally(() => setPending(false));
  }

  const hasLifecycle = actions.some((action) => ["suspend", "restore", "close", "revoke"].includes(action));

  return (
    <OperateArea
      bay="record"
      framed={false}
      label="Enrollment"
      title={detail.enrollment.display_label}
      description={`${statusCopy}. History remains inspectable.`}
      back={<BackKey to={`/activities/${activityId}/cohorts/${cohortId}/enrollments`} label="Participants" />}
    >
      <Stack gap="6">
        <AssignmentInstrumentGrid label="Enrollment identity" columns={4}>
          <ReadoutGridRow label="Identity">
            <ReadoutGridField term="Participant">{detail.enrollment.display_label}</ReadoutGridField>
            <ReadoutGridField term="Enrollment">
              <CompactId tabbable value={detail.enrollment.enrollment_id} />
            </ReadoutGridField>
            <ReadoutGridField term="Revision">{String(detail.enrollment.revision)}</ReadoutGridField>
            <ReadoutGridField term="Record">
              <AssignmentRecordReadout
                variant={record.variant}
                solid={record.solid}
                label={statusCopy}
              />
            </ReadoutGridField>
          </ReadoutGridRow>
          <ReadoutGridRow label="Timing">
            <ReadoutGridField term="Eligibility">{timing ? eligibility : "—"}</ReadoutGridField>
            <ReadoutGridField term="Baseline deadline">{timing ? baselineDeadline : "—"}</ReadoutGridField>
            <ReadoutGridField term="Effective exclusive end">{timing ? effectiveEnd : "—"}</ReadoutGridField>
            <ReadoutGridField term="Accommodation">{timing ? consequence : "—"}</ReadoutGridField>
          </ReadoutGridRow>
        </AssignmentInstrumentGrid>
        {error && !confirmOpen && !lifecycleConfirm ? <Alert variant="danger" title="Update did not complete">{error}</Alert> : null}
        <WorkWell
          live={false}
          seat="stack"
          label="Enrollment actions"
          head={<WorkWellHead title="Enrollment actions" ident="Lifecycle stays on the server." />}
        >
          <WorkWellSection>
            <Stack gap="4">
              {timing ? null : (
                <EmptyPlate
                  inset
                  label="Timing unavailable"
                  note="Timing is not available for this Enrollment."
                />
              )}
              {hasLifecycle || canRequest ? (
                <KeyGroup aria-label="Enrollment commands">
                  {actions.includes("suspend") ? (
                    <Key variant="quiet" disabled={pending} onClick={() => mutate("suspend")}>Suspend Enrollment</Key>
                  ) : null}
                  {actions.includes("restore") ? (
                    <Key variant="quiet" disabled={pending} onClick={() => mutate("restore")}>Restore Enrollment</Key>
                  ) : null}
                  {actions.includes("close") ? (
                    <Key variant="quiet" destructive disabled={pending} onClick={() => {
                      setError(null);
                      setLifecycleConfirm("close");
                    }}>Close Enrollment</Key>
                  ) : null}
                  {actions.includes("revoke") ? (
                    <Key variant="quiet" destructive disabled={pending} onClick={() => {
                      setError(null);
                      setLifecycleConfirm("revoke");
                    }}>Revoke Enrollment</Key>
                  ) : null}
                  {canRequest ? (
                    <Key variant="open" disabled={pending} onClick={() => {
                      setError(null);
                      setRequestedValue((current) => {
                        if (current.trim() || !timing) return current;
                        const bound = accommodationCurrentUtc(dimension, timing.effective);
                        if (durationDimension) return bound;
                        return utcInstantToPickerValue(bound, zone);
                      });
                      setConfirmOpen(true);
                    }}>
                      Request accommodation
                    </Key>
                  ) : null}
                </KeyGroup>
              ) : null}
            </Stack>
          </WorkWellSection>
        </WorkWell>
        <WorkWell live={false} seat="stack" label="Accommodations" head={<WorkWellHead title="Accommodations" ident="Policy-bounded. Baseline stays frozen until granted." />}>
          <WorkWellSection>
            {timing && timing.history.length > 0 ? (
              <ul>
                {timing.history.map((item) => (
                  <li key={`${item.accommodation_id}-${item.revision}`}>
                    <Stack gap="2">
                      <span>
                        {wordsFromCode(item.dimension, ACCOMMODATION_DIMENSION_COPY)}
                        {" · "}
                        {wordsFromCode(item.status, ACCOMMODATION_STATUS_COPY)}
                        {" · "}
                        {wordsFromCode(item.reason_category)}
                        {item.fairness_exception ? " · Fairness exception" : ""}
                      </span>
                      <InstantReadout value={item.created_at_utc} timeZone={zone} />
                      {item.status === "pending_approval" ? (
                        <Inline gap="2" wrap>
                          <Key
                            variant="quiet"
                            disabled={pending}
                            onClick={() => {
                              runAccommodation(
                                () => client.decideAccommodation(
                                  activityId,
                                  cohortId,
                                  enrollmentId,
                                  item.accommodation_id,
                                  true,
                                  item.revision,
                                  createEnrollmentIdempotencyKey(),
                                ),
                                "The accommodation could not be decided.",
                              );
                            }}
                          >
                            Approve exception
                          </Key>
                          <Key
                            variant="quiet"
                            disabled={pending}
                            onClick={() => {
                              runAccommodation(
                                () => client.decideAccommodation(
                                  activityId,
                                  cohortId,
                                  enrollmentId,
                                  item.accommodation_id,
                                  false,
                                  item.revision,
                                  createEnrollmentIdempotencyKey(),
                                ),
                                "The accommodation could not be decided.",
                              );
                            }}
                          >
                            Reject exception
                          </Key>
                        </Inline>
                      ) : null}
                      {item.status === "granted" ? (
                        <Key
                          variant="quiet"
                          disabled={pending}
                          onClick={() => {
                            runAccommodation(
                              () => client.revokeAccommodation(
                                activityId,
                                cohortId,
                                enrollmentId,
                                item.accommodation_id,
                                item.revision,
                                createEnrollmentIdempotencyKey(),
                              ),
                              "The accommodation could not be revoked.",
                            );
                          }}
                        >
                          Revoke accommodation
                        </Key>
                      ) : null}
                    </Stack>
                  </li>
                ))}
              </ul>
            ) : (
              <p>No accommodation history for this Enrollment.</p>
            )}
          </WorkWellSection>
        </WorkWell>
        <WorkWell live={false} seat="stack" label="History" head={<WorkWellHead title="History" ident="Prior states remain inspectable." />}>
          <WorkWellSection>
            {detail.history.length > 0 ? (
              <ol>
                {detail.history.map((item) => (
                  <li key={item.sequence} data-sequence={String(item.sequence)} value={item.sequence}>
                    <Stack gap="2">
                      <span>
                        {enrollmentStatusCopy(item.prior_status)}
                        {" → "}
                        {enrollmentStatusCopy(item.new_status)}
                        {" ("}
                        {wordsFromCode(item.reason_code)}
                        {")"}
                      </span>
                      <InstantReadout value={item.occurred_at} timeZone={zone} />
                    </Stack>
                  </li>
                ))}
              </ol>
            ) : (
              <p>No enrollment history is available.</p>
            )}
          </WorkWellSection>
        </WorkWell>
      </Stack>
      <CeremonyDialog
        open={lifecycleConfirm !== null}
        onClose={() => {
          if (!pending) setLifecycleConfirm(null);
        }}
        labelledBy={lifecycleTitleId}
      >
        <DialogPlate>
          <DialogPlateHead
            title={lifecycleConfirm === "revoke" ? "Revoke this Enrollment?" : "Close this Enrollment?"}
            titleId={lifecycleTitleId}
          />
          <DialogPlateBody>
            <Stack gap="4">
              {lifecycleConfirm && error ? <Alert variant="danger" title="Update did not complete">{error}</Alert> : null}
              <p>
                {lifecycleConfirm === "revoke" ? "Revoking" : "Closing"}{" "}
                {detail.enrollment.display_label}. {ENROLLMENT_TERMINAL_CONSEQUENCE}
              </p>
              <p>
                Required reason: {wordsFromCode(lifecycleConfirm ? enrollmentLifecycleReason(lifecycleConfirm) : "")}.
              </p>
            </Stack>
          </DialogPlateBody>
          <DialogPlateFooter
            arrangement="split"
            secondary={
              <Key variant="quiet" disabled={pending} onClick={() => setLifecycleConfirm(null)}>
                Cancel
              </Key>
            }
            primary={
              <Key
                variant="transmit"
                size="large"
                destructive
                waiting={pending}
                disabled={pending || !lifecycleConfirm}
                onClick={() => {
                  if (!lifecycleConfirm) return;
                  mutate(lifecycleConfirm);
                }}
              >
                {lifecycleConfirm === "revoke" ? "Revoke Enrollment" : "Close Enrollment"}
              </Key>
            }
          />
        </DialogPlate>
      </CeremonyDialog>
      <CeremonyDialog open={confirmOpen} onClose={() => setConfirmOpen(false)} labelledBy={confirmId}>
        <DialogPlate width="wide">
          <DialogPlateHead title="Request a bounded accommodation?" titleId={confirmId} />
          <DialogPlateBody>
            <Stack gap="4">
              {error ? <Alert variant="danger" title="Request did not complete">{error}</Alert> : null}
              <p>
                This records a policy-bounded request. It does not change the frozen cohort baseline until the server
                grants an effective replacement.
              </p>
              <ReadoutList
                label="Request bounds"
                rows={[
                  { term: "Dimension", value: dimensionCopy },
                  { term: "Reason", value: reasonCopy },
                  { term: accommodationCurrentBoundTerm(dimension), value: currentBoundCopy },
                ]}
              />
              {durationDimension ? (
                <FormField id={valueId} label="Requested value" layout="stack" hint={valueHint}>
                  {(control) => (
                    <FieldInput
                      {...control}
                      value={requestedValue}
                      placeholder={valueExample}
                      onChange={(event) => setRequestedValue(event.target.value)}
                    />
                  )}
                </FormField>
              ) : (
                <FormField
                  id={valueId}
                  label="Requested value"
                  layout="stack"
                  hint={valueHint}
                  labelAssociatesControl={false}
                >
                  {(control, { labelId }) => (
                    <DateTimePicker
                      id={control.id}
                      labelId={labelId}
                      describedBy={control["aria-describedby"]}
                      mode="datetime"
                      value={requestedValue}
                      onChange={setRequestedValue}
                      now={utcInstantToPickerValue(new Date().toISOString(), zone)}
                    />
                  )}
                </FormField>
              )}
              <AcknowledgmentGate
                id={fairnessId}
                presentation="inline"
                checked={fairnessException}
                onChange={setFairnessException}
              >
                Requires a distinct fairness-exception approver
              </AcknowledgmentGate>
            </Stack>
          </DialogPlateBody>
          <DialogPlateFooter
            arrangement="split"
            secondary={<Key variant="quiet" onClick={() => setConfirmOpen(false)}>Cancel</Key>}
            primary={
            <Key
              variant="transmit"
              disabled={pending || requestedValue.trim().length === 0 || !dimension || !reasonCategory}
              onClick={() => {
                if (!dimension || !reasonCategory) return;
                const requested = durationDimension
                  ? requestedValue.trim()
                  : pickerValueToUtcInstant(requestedValue, zone);
                if (!requested) {
                  setError(durationDimension
                    ? "Enter a duration in seconds."
                    : "Enter a complete date and time.");
                  return;
                }
                setPending(true);
                void client.grantAccommodation(activityId, cohortId, enrollmentId, {
                  dimension,
                  requested_value: requested,
                  reason_category: reasonCategory,
                  fairness_exception: fairnessException,
                  expected_revision: timing?.enrollment.revision ?? revision,
                  idempotency_key: createEnrollmentIdempotencyKey(),
                })
                  .then((outcome) => {
                    if (!outcome.succeeded) {
                      setError(enrollmentOutcomeCopy(outcome.outcome_code, "The accommodation could not be recorded."));
                      return;
                    }
                    setConfirmOpen(false);
                    setRequestedValue("");
                    setFairnessException(false);
                    pushToast({ label: "Accommodation", copy: "The request was recorded." });
                    return reload();
                  })
                  .catch((caught: unknown) => {
                    setError(enrollmentFailureCopy(caught, "The accommodation could not be recorded."));
                  })
                  .finally(() => setPending(false));
              }}
            >
              Request accommodation
            </Key>
            }
          />
        </DialogPlate>
      </CeremonyDialog>
    </OperateArea>
  );
}
