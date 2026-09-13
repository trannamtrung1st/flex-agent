import { useEffect, useMemo } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import {
  Alert,
  GuidedTaskFoot,
  InstantReadout,
  Key,
  KeyGroup,
  ReadoutList,
  SplitBay,
  Stack,
  WaitPlate,
  WorkWell,
  WorkWellHead,
  WorkWellSection,
} from "../design-system";
import { useProductionApi } from "../api/production-api";
import { createProductionReviewClient, isReviewAccessLoss } from "../api/production-review";
import { AssignmentHead } from "../components/work/AssignmentHead";
import { AssignmentRecordReadout } from "../components/work/AssignmentRecordReadout";
import { AssignmentStationLayout } from "../components/work/AssignmentStationLayout";
import { AssignmentStatusReadout } from "../components/work/AssignmentStatusReadout";
import { ReviewerSealedReadout } from "../components/work/ReviewerSealedReadout";
import {
  useReviewCaseQuery,
  useReviewCriterionQuery,
  useReviewEvidenceQuery,
} from "../features/review/queries";
import {
  BACK_TO_CRITERION,
  INTERNAL_EVALUATION_NOTICE,
  OPEN_EVIDENCE,
  assignmentCopy,
  candidateCopy,
  caseTitle,
  criterionStatusCopy,
  evaluatorModeCopy,
  evaluationProcessingCopy,
  evidenceAvailabilityCopy,
  evidencePrecisionCopy,
  integrityCopy,
  processingIndicatorVariant,
  processingWellCopy,
  isInspectableReviewState,
} from "../features/review/presentation";
import { maxWidthQuery } from "../lib/breakpoints";
import { useMediaQuery } from "../lib/useMediaQuery";

type ReviewLocationState = {
  restoreEvidenceId?: string;
};

function ReviewHeading({
  title,
  meta,
  phase,
  record,
}: {
  title: string;
  meta?: string;
  phase: string;
  record: string;
}) {
  return (
    <AssignmentHead
      title={title}
      meta={meta}
      status={(
        <AssignmentStatusReadout
          aria-label="Review case status"
          phase={phase}
          record={record}
        />
      )}
    />
  );
}

export function ProductionReviewCasePage() {
  const { reviewId, criterionId, evidenceId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const restoreEvidenceId = (location.state as ReviewLocationState | null)?.restoreEvidenceId;
  const { fetchJson, shell } = useProductionApi();
  const client = useMemo(() => createProductionReviewClient(fetchJson), [fetchJson]);
  const scope = shell
    ? { actorId: shell.actor_id, organizationId: shell.organization_id }
    : null;
  const compact = useMediaQuery(maxWidthQuery("compact"));
  const evidenceFocusId = restoreEvidenceId ? `evidence-ref-${restoreEvidenceId}` : null;
  const caseQuery = useReviewCaseQuery(client, scope, reviewId);
  const record = caseQuery.data;
  const inspectable = isInspectableReviewState(record?.evaluation_processing_state);
  const summaries = inspectable ? record.criterion_summaries : [];
  const activeCriterionId = criterionId ?? summaries[0]?.criterion_id;
  const inspectingEvidence = Boolean(inspectable && evidenceId);
  const criterionQuery = useReviewCriterionQuery(
    client,
    scope,
    reviewId,
    activeCriterionId,
    Boolean(inspectable && activeCriterionId && !inspectingEvidence),
  );
  const evidenceQuery = useReviewEvidenceQuery(
    client,
    scope,
    reviewId,
    evidenceId,
    inspectingEvidence,
  );

  useEffect(() => {
    if (!evidenceFocusId || inspectingEvidence || criterionQuery.isPending) {
      return;
    }
    document.getElementById(evidenceFocusId)?.focus();
  }, [evidenceFocusId, inspectingEvidence, criterionQuery.isPending, criterionQuery.data]);

  const accessLost = isReviewAccessLoss(caseQuery.error)
    || isReviewAccessLoss(criterionQuery.error)
    || isReviewAccessLoss(evidenceQuery.error);

  if (accessLost) {
    return (
      <AssignmentStationLayout
        railHomeTo="/review"
        railHomeLabel="Review work"
        brandSuffix="Review Station"
        railLabel="Review instruments"
        mainLabel="Review case"
        instruments={null}
        heading={<ReviewHeading title="Assignment revoked" phase="Unavailable" record="Assignment revoked" />}
        actions={(
          <GuidedTaskFoot arrangement="end">
            <Key variant="quiet" to="/review">Return to Review work</Key>
          </GuidedTaskFoot>
        )}
      >
        <WorkWell live={false} label="Assignment revoked">
          <WorkWellSection>
            <p>This Review case is no longer assigned to you. Protected Evaluation and Evidence were removed.</p>
          </WorkWellSection>
        </WorkWell>
      </AssignmentStationLayout>
    );
  }

  if (caseQuery.error && !record) {
    const note = caseQuery.error instanceof Error ? caseQuery.error.message : "Request failed";
    return (
      <AssignmentStationLayout
        railHomeTo="/review"
        railHomeLabel="Review work"
        brandSuffix="Review Station"
        railLabel="Review instruments"
        mainLabel="Review case"
        instruments={null}
        heading={<ReviewHeading title="Review case unavailable" phase="Unavailable" record="Unavailable" />}
        actions={(
          <GuidedTaskFoot arrangement="end">
            <Key variant="quiet" to="/review">Return to Review work</Key>
          </GuidedTaskFoot>
        )}
      >
        <WorkWell live={false} label="Review case unavailable">
          <WorkWellSection>
            <p>{note}</p>
            <Key size="compact" onClick={() => void caseQuery.refetch()}>Retry</Key>
          </WorkWellSection>
        </WorkWell>
      </AssignmentStationLayout>
    );
  }

  if (!record) {
    return (
      <AssignmentStationLayout
        railHomeTo="/review"
        railHomeLabel="Review work"
        brandSuffix="Review Station"
        railLabel="Review instruments"
        mainLabel="Review case"
        instruments={null}
        heading={<ReviewHeading title="Review case" phase="Loading" record="—" />}
      >
        <WorkWell live={false} label="Review case">
          <WorkWellSection>
            <WaitPlate inset label="Loading Review case…" />
          </WorkWellSection>
        </WorkWell>
      </AssignmentStationLayout>
    );
  }

  const title = caseTitle(record);
  const phase = evaluationProcessingCopy(record.evaluation_processing_state);
  const assignment = assignmentCopy(record.assignment_state);
  const instruments = (
    <ReadoutList
      label="Review case identity"
      rows={[
        { term: "Campaign", value: record.campaign_label ?? "—" },
        { term: "Task", value: record.task_label ?? "—" },
        { term: "Participant", value: record.participant_label ?? "—" },
        {
          term: "Evaluation",
          value: (
            <ReviewerSealedReadout
              variant={processingIndicatorVariant(record.evaluation_processing_state)}
              solid={record.evaluation_processing_state === "completed"}
              label={INTERNAL_EVALUATION_NOTICE}
            />
          ),
          emphasis: "inline",
        },
        { term: "Integrity", value: integrityCopy(record.integrity_state) },
        { term: "Candidate", value: candidateCopy(record.candidate_state) ?? "—" },
        { term: "Procedure", value: record.procedure_label ?? "—" },
        {
          term: "Completed",
          value: <InstantReadout value={record.completed_at} timeZone={record.time_zone_id} />,
        },
      ]}
    />
  );

  const heading = (
    <ReviewHeading
      title={title}
      meta={record.campaign_label && record.task_label ? record.campaign_label : undefined}
      phase={phase}
      record={assignment}
    />
  );

  const layoutProps = {
    railHomeTo: "/review",
    railHomeLabel: "Review work",
    brandSuffix: "Review Station",
    railLabel: "Review instruments",
    mainLabel: "Review case",
    instruments,
    heading,
  } as const;

  const criterionReturnTo = `/review/${record.review_case_id}/criteria/${encodeURIComponent(activeCriterionId ?? "")}`;
  const backToCriterion = (
    extra?: { size?: "compact" },
  ) => (
    <Key
      variant="back"
      size={extra?.size}
      to={criterionReturnTo}
      linkState={{ restoreEvidenceId: evidenceId } satisfies ReviewLocationState}
    >
      {BACK_TO_CRITERION}
    </Key>
  );

  if (!inspectable) {
    return (
      <AssignmentStationLayout {...layoutProps}>
        <WorkWell live={false} label={phase} head={<WorkWellHead title={phase} />}>
          <WorkWellSection>
            <p>{processingWellCopy(record)}</p>
            <p>{INTERNAL_EVALUATION_NOTICE}</p>
            <AssignmentRecordReadout
              variant={processingIndicatorVariant(record.evaluation_processing_state)}
              label={assignment}
            />
          </WorkWellSection>
        </WorkWell>
      </AssignmentStationLayout>
    );
  }

  if (inspectingEvidence) {
    const evidence = evidenceQuery.data;
    return (
      <AssignmentStationLayout
        {...layoutProps}
        actions={(
          <GuidedTaskFoot arrangement="end">
            {backToCriterion()}
          </GuidedTaskFoot>
        )}
      >
        <WorkWell
          live={false}
          label="Evidence source"
          head={(
            <WorkWellHead>
              <h2 className="work-well__title">Evidence source</h2>
              {backToCriterion({ size: "compact" })}
            </WorkWellHead>
          )}
        >
          <WorkWellSection>
            {evidenceQuery.isPending ? (
              <WaitPlate inset label="Opening Evidence…" />
            ) : evidenceQuery.error ? (
              <Alert variant="danger" title="Evidence could not be opened">
                {evidenceQuery.error instanceof Error ? evidenceQuery.error.message : "Request failed"}
              </Alert>
            ) : evidence ? (
              <Stack gap="4">
                <p>{INTERNAL_EVALUATION_NOTICE}</p>
                <p>{evidenceAvailabilityCopy(evidence.availability)}</p>
                <p>{evidencePrecisionCopy(evidence.locator.precision)}</p>
                <p>Source {evidence.locator.source_type} · version {evidence.locator.source_ref.source_version}</p>
                {evidence.unavailability_notice ? <p>{evidence.unavailability_notice}</p> : null}
                {evidence.display_text ? (
                  <pre className="review-evidence-source">{evidence.display_text}</pre>
                ) : null}
              </Stack>
            ) : null}
          </WorkWellSection>
        </WorkWell>
      </AssignmentStationLayout>
    );
  }

  const criterion = criterionQuery.data;
  const criterionNav = summaries.length === 0 ? (
    <p>No criteria are available on this Evaluation.</p>
  ) : compact ? (
    <KeyGroup>
      {summaries.map((item, index) => {
        const previous = summaries[index - 1];
        const next = summaries[index + 1];
        if (item.criterion_id !== activeCriterionId) {
          return null;
        }
        return (
          <span key={item.criterion_id}>
            <label>
              Criterion
              <select
                aria-label="Criterion"
                value={item.criterion_id}
                onChange={(event) => {
                  navigate(`/review/${record.review_case_id}/criteria/${encodeURIComponent(event.target.value)}`);
                }}
              >
                {summaries.map((option) => (
                  <option key={option.criterion_id} value={option.criterion_id}>
                    {option.display_label}
                  </option>
                ))}
              </select>
            </label>
            {previous ? (
              <Key
                size="compact"
                to={`/review/${record.review_case_id}/criteria/${encodeURIComponent(previous.criterion_id)}`}
              >
                Previous criterion
              </Key>
            ) : null}
            {next ? (
              <Key
                size="compact"
                to={`/review/${record.review_case_id}/criteria/${encodeURIComponent(next.criterion_id)}`}
              >
                Next criterion
              </Key>
            ) : null}
          </span>
        );
      })}
    </KeyGroup>
  ) : (
    <nav aria-label="Criteria">
      <ul className="review-criterion-nav">
        {summaries.map((item) => {
          const current = item.criterion_id === activeCriterionId;
          return (
            <li key={item.criterion_id} className="review-criterion-nav__item">
              <Link
                to={`/review/${record.review_case_id}/criteria/${encodeURIComponent(item.criterion_id)}`}
                aria-current={current ? "page" : undefined}
              >
                {item.display_label}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );

  return (
    <AssignmentStationLayout {...layoutProps}>
      <SplitBay start={criterionNav}>
        <WorkWell
          live={false}
          label={criterion?.display_label ?? "Criterion"}
          head={<WorkWellHead title={criterion?.display_label ?? "Criterion"} />}
        >
          <WorkWellSection>
            {criterionQuery.isPending ? (
              <WaitPlate inset label="Loading criterion…" />
            ) : criterionQuery.error ? (
              <Alert variant="danger" title="Criterion could not be loaded">
                {criterionQuery.error instanceof Error ? criterionQuery.error.message : "Request failed"}
              </Alert>
            ) : criterion ? (
              <Stack gap="4">
                <p>{INTERNAL_EVALUATION_NOTICE}</p>
                <p>{evaluatorModeCopy(criterion.evaluator_mode, criterion.evaluator_mode_label)}</p>
                <p>{criterionStatusCopy(criterion.status)}</p>
                {criterion.score != null ? <p>Score {String(criterion.score)}</p> : null}
                {criterion.confidence ? <p>Confidence {criterion.confidence}</p> : null}
                {criterion.uncertainty.length > 0 ? <p>Uncertainty {criterion.uncertainty.join(", ")}</p> : null}
                {criterion.rationale ? <p>{criterion.rationale}</p> : null}
                {criterion.provisional_feedback ? (
                  <p>Provisional feedback {criterion.provisional_feedback}</p>
                ) : null}
                {criterion.evidence_references.length === 0 ? (
                  <p>No Evidence references on this criterion.</p>
                ) : (
                  <ul>
                    {criterion.evidence_references.map((reference) => {
                      const restore = restoreEvidenceId === reference.evidence_id;
                      return (
                        <li key={reference.evidence_id}>
                          <Key
                            id={restore ? `evidence-ref-${reference.evidence_id}` : undefined}
                            variant="inspect"
                            size="compact"
                            to={`/review/${record.review_case_id}/criteria/${encodeURIComponent(criterion.criterion_id)}/evidence/${encodeURIComponent(reference.evidence_id)}`}
                          >
                            {OPEN_EVIDENCE}
                          </Key>
                          {" "}
                          {reference.source_type}
                          {" · "}
                          {evidencePrecisionCopy(reference.precision)}
                        </li>
                      );
                    })}
                  </ul>
                )}
              </Stack>
            ) : (
              <p>Select a criterion to inspect its judgment and Evidence.</p>
            )}
          </WorkWellSection>
        </WorkWell>
      </SplitBay>
    </AssignmentStationLayout>
  );
}
