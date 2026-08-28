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
import { CeremonyArea, CeremonyEmpty } from "../components/shell/SessionChrome";
import {
  Alert,
  BackKey,
  CeremonyDialog,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
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

export function ProductionEnrollmentDetailPage() {
  const { activityId = "", cohortId = "", enrollmentId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const confirmId = useId();
  const valueId = useId();
  const [detail, setDetail] = useState<EnrollmentDetailV1 | null>(null);
  const [timing, setTiming] = useState<EnrollmentTimingV2 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [requestedValue, setRequestedValue] = useState("");
  const [fairnessException, setFairnessException] = useState(false);

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
      <CeremonyArea label="Enrollment unavailable" title="Enrollment unavailable" danger>
        <CeremonyEmpty note={error}>
          <Key variant="open" to={`/activities/${activityId}/cohorts/${cohortId}/enrollments`}>Return to Participants</Key>
        </CeremonyEmpty>
      </CeremonyArea>
    );
  }

  if (!detail) {
    return (
      <CeremonyArea label="Enrollment" title="Enrollment">
        <WaitPanel label="Loading Enrollment…" />
      </CeremonyArea>
    );
  }

  const actions = detail.enrollment.permitted_actions;
  const revision = detail.enrollment.revision;
  const dimension = timing?.permitted_dimensions[0];
  const reasonCategory = timing?.permitted_reason_categories[0];
  const canRequest = Boolean(timing?.policy_available && dimension && reasonCategory);

  function mutate(operation: "suspend" | "restore" | "close" | "revoke") {
    setPending(true);
    void client.mutate(
      activityId,
      cohortId,
      enrollmentId,
      operation,
      "administrator_action",
      revision,
      createEnrollmentIdempotencyKey(),
    )
      .then((outcome) => {
        if (!outcome.succeeded) {
          setError(enrollmentOutcomeCopy(outcome.outcome_code, "The Enrollment could not be updated."));
          return;
        }
        return reload();
      })
      .catch((caught: unknown) => {
        setError(enrollmentFailureCopy(caught, "The Enrollment could not be updated."));
      })
      .finally(() => setPending(false));
  }

  return (
    <OperateArea
      className="workspace-area work-plane"
      frameClassName="record-frame"
      label="Enrollment"
      title={detail.enrollment.display_label}
      description={`Status ${detail.enrollment.status}. History remains inspectable.`}
      back={<BackKey to={`/activities/${activityId}/cohorts/${cohortId}/enrollments`} label="Participants" />}
      context={(
        <ReadoutGrid label="Enrollment identity" columns={4} className="assignment-instruments">
          <ReadoutGridRow label="Identity">
            <ReadoutGridField term="Participant">{detail.enrollment.display_label}</ReadoutGridField>
            <ReadoutGridField term="Enrollment">{detail.enrollment.enrollment_id}</ReadoutGridField>
            <ReadoutGridField term="Revision">{String(detail.enrollment.revision)}</ReadoutGridField>
            <ReadoutGridField term="Record">
              <StateReadout
                variant="rest"
                label={detail.enrollment.status}
                className="assignment-record"
                labelClassName="assignment-record-label"
              />
            </ReadoutGridField>
          </ReadoutGridRow>
        </ReadoutGrid>
      )}
    >
      <div className="assignment-station">
        {error ? <Alert variant="danger" title="Update did not complete">{error}</Alert> : null}
        <WorkWell
          live={false}
          label="Enrollment actions"
          head={<WorkWellHead title="Enrollment actions" ident="Lifecycle stays on the server." />}
          foot={
            <Inline gap="2" wrap>
              {actions.includes("suspend") ? (
                <Key variant="quiet" disabled={pending} onClick={() => mutate("suspend")}>Suspend</Key>
              ) : null}
              {actions.includes("restore") ? (
                <Key variant="quiet" disabled={pending} onClick={() => mutate("restore")}>Restore</Key>
              ) : null}
              {actions.includes("close") ? (
                <Key variant="quiet" disabled={pending} onClick={() => mutate("close")}>Close</Key>
              ) : null}
              {actions.includes("revoke") ? (
                <Key variant="quiet" disabled={pending} onClick={() => mutate("revoke")}>Revoke</Key>
              ) : null}
              {canRequest ? (
                <Key variant="quiet" disabled={pending} onClick={() => setConfirmOpen(true)}>Request accommodation</Key>
              ) : null}
            </Inline>
          }
        >
          <WorkWellSection>
            {timing?.effective ? (
              <p>
                Effective eligibility {timing.effective.eligibility_state} in {timing.effective.time_zone_id}.
                Exclusive submission end{" "}
                {campaignDeadlineCopy(
                  formatCampaignInstant(
                    timing.effective.submission_exclusive_end_utc,
                    timing.effective.time_zone_id,
                  ),
                )}.
              </p>
            ) : (
              <p>Timing is not available for this Enrollment.</p>
            )}
          </WorkWellSection>
        </WorkWell>
        <WorkWell live={false} label="Accommodations" head={<WorkWellHead title="Accommodations" ident="Policy-bounded requests. The cohort baseline stays frozen until the server grants a replacement." />}>
          <WorkWellSection>
            {timing && timing.history.length > 0 ? (
              <ol>
                {timing.history.map((item) => (
                  <li key={`${item.accommodation_id}-${item.revision}`}>
                    {item.dimension} {item.status} ({item.reason_category}
                    {item.fairness_exception ? "; fairness exception" : ""})
                    {item.status === "pending_approval" ? (
                      <Inline gap="2">
                        <Key
                          variant="quiet"
                          disabled={pending}
                          onClick={() => {
                            setPending(true);
                            void client.decideAccommodation(
                              activityId,
                              cohortId,
                              enrollmentId,
                              item.accommodation_id,
                              true,
                              item.revision,
                              createEnrollmentIdempotencyKey(),
                            )
                              .then((outcome) => {
                                if (!outcome.succeeded) {
                                  setError(enrollmentOutcomeCopy(outcome.outcome_code, "The accommodation could not be decided."));
                                  return;
                                }
                                return reload();
                              })
                              .catch((caught: unknown) => {
                                setError(enrollmentFailureCopy(caught, "The accommodation could not be decided."));
                              })
                              .finally(() => setPending(false));
                          }}
                        >
                          Approve exception
                        </Key>
                        <Key
                          variant="quiet"
                          disabled={pending}
                          onClick={() => {
                            setPending(true);
                            void client.decideAccommodation(
                              activityId,
                              cohortId,
                              enrollmentId,
                              item.accommodation_id,
                              false,
                              item.revision,
                              createEnrollmentIdempotencyKey(),
                            )
                              .then((outcome) => {
                                if (!outcome.succeeded) {
                                  setError(enrollmentOutcomeCopy(outcome.outcome_code, "The accommodation could not be decided."));
                                  return;
                                }
                                return reload();
                              })
                              .catch((caught: unknown) => {
                                setError(enrollmentFailureCopy(caught, "The accommodation could not be decided."));
                              })
                              .finally(() => setPending(false));
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
                          setPending(true);
                          void client.revokeAccommodation(
                            activityId,
                            cohortId,
                            enrollmentId,
                            item.accommodation_id,
                            item.revision,
                            createEnrollmentIdempotencyKey(),
                          )
                            .then((outcome) => {
                              if (!outcome.succeeded) {
                                setError(enrollmentOutcomeCopy(outcome.outcome_code, "The accommodation could not be revoked."));
                                return;
                              }
                              return reload();
                            })
                            .catch((caught: unknown) => {
                              setError(enrollmentFailureCopy(caught, "The accommodation could not be revoked."));
                            })
                            .finally(() => setPending(false));
                        }}
                      >
                        Revoke accommodation
                      </Key>
                    ) : null}
                  </li>
                ))}
              </ol>
            ) : (
              <p>No accommodation history for this Enrollment.</p>
            )}
          </WorkWellSection>
        </WorkWell>
        <WorkWell live={false} label="History" head={<WorkWellHead title="History" ident="Prior enrollment states remain inspectable." />}>
          <WorkWellSection>
            {detail.history.length > 0 ? (
              <ol>
                {detail.history.map((item) => (
                  <li key={item.sequence}>
                    {item.prior_status} → {item.new_status} ({item.reason_code}) at {item.occurred_at}
                  </li>
                ))}
              </ol>
            ) : (
              <p>No enrollment history is available.</p>
            )}
          </WorkWellSection>
        </WorkWell>
      </div>
      <CeremonyDialog open={confirmOpen} onClose={() => setConfirmOpen(false)} labelledBy={confirmId}>
        <DialogPlate>
          <DialogPlateHead title="Request a bounded accommodation?" titleId={confirmId} />
          <DialogPlateBody>
            <p>
              This records a policy-bounded request. It does not change the frozen cohort baseline until the server
              grants an effective replacement.
            </p>
            <FormField id={valueId} label="Requested value" layout="stack">
              {(control) => (
                <FieldInput
                  {...control}
                  value={requestedValue}
                  onChange={(event) => setRequestedValue(event.target.value)}
                />
              )}
            </FormField>
            <p>
              <label>
                <input
                  type="checkbox"
                  checked={fairnessException}
                  onChange={(event) => setFairnessException(event.target.checked)}
                />
                {" "}
                Requires a distinct fairness-exception approver
              </label>
            </p>
          </DialogPlateBody>
          <DialogPlateFooter>
            <Key variant="quiet" onClick={() => setConfirmOpen(false)}>Cancel</Key>
            <Key
              variant="transmit"
              disabled={pending || requestedValue.trim().length === 0 || !dimension || !reasonCategory}
              onClick={() => {
                if (!dimension || !reasonCategory) return;
                setPending(true);
                void client.grantAccommodation(activityId, cohortId, enrollmentId, {
                  dimension,
                  requested_value: requestedValue.trim(),
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
          </DialogPlateFooter>
        </DialogPlate>
      </CeremonyDialog>
    </OperateArea>
  );
}
