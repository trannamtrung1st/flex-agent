import { useEffect, useMemo } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import {
  Alert,
  GuidedTaskFoot,
  InstantReadout,
  Key,
  ReadoutList,
  type ReadoutListRow,
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
  usePruneStaleReviewEvaluationCache,
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
  evaluationProcessingCopy,
  evaluatorModePresentation,
  evidenceAvailabilityCopy,
  evidenceLocationCopy,
  evidencePrecisionCopy,
  integrityCopy,
  processingIndicatorVariant,
  processingWellCopy,
  verificationStateCopy,
  isInspectableReviewState,
} from "../features/review/presentation";
import type {
  ReviewCriterionReadV1,
  ReviewCriterionSummaryV1,
  ReviewEvidenceOpenV1,
} from "../contracts/v1";
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

function criterionJudgmentRows(criterion: ReviewCriterionReadV1): ReadoutListRow[] {
  const rows: ReadoutListRow[] = [
    { term: "Boundary", value: INTERNAL_EVALUATION_NOTICE, emphasis: "inline" },
    { term: "Version", value: criterion.criterion_version },
    { term: "Status", value: criterionStatusCopy(criterion.status) },
  ];
  if (criterion.score != null && String(criterion.score).length > 0) {
    rows.push({ term: "Score", value: String(criterion.score) });
  }
  rows.push({
    term: "Mode",
    value: evaluatorModePresentation(criterion.evaluator_mode, criterion.evaluator_mode_label),
  });
  if (criterion.confidence) {
    rows.push({ term: "Confidence", value: criterion.confidence });
  }
  if (criterion.uncertainty.length > 0) {
    rows.push({ term: "Uncertainty", value: criterion.uncertainty.join(", ") });
  }
  if (criterion.rationale) {
    rows.push({ term: "Rationale", value: criterion.rationale });
  }
  return rows;
}

function evidenceProvenanceRows(evidence: ReviewEvidenceOpenV1): ReadoutListRow[] {
  const rows: ReadoutListRow[] = [
    { term: "Boundary", value: INTERNAL_EVALUATION_NOTICE, emphasis: "inline" },
    { term: "Evaluation", value: evidence.evaluation_id },
    { term: "Source", value: `${evidence.locator.source_type} · ${evidence.locator.source_ref.source_id}` },
    { term: "Version", value: evidence.locator.source_ref.source_version },
    { term: "Location", value: evidenceLocationCopy(evidence.locator.location) },
    { term: "Precision", value: evidencePrecisionCopy(evidence.locator.precision) },
    { term: "Verification", value: verificationStateCopy(evidence.locator.integrity.verification_state) },
    { term: "Availability", value: evidenceAvailabilityCopy(evidence.availability) },
    { term: "Adapter", value: evidence.locator.integrity.adapter_version },
  ];
  if (evidence.unavailability_notice) {
    rows.push({ term: "Notice", value: evidence.unavailability_notice });
  }
  if (evidence.display_text) {
    rows.push({
      term: "Cited text",
      value: <pre className="review-evidence-source">{evidence.display_text}</pre>,
    });
  }
  return rows;
}

function CriterionNav({
  summaries,
  activeCriterionId,
  reviewCaseId,
}: {
  summaries: readonly ReviewCriterionSummaryV1[];
  activeCriterionId: string | undefined;
  reviewCaseId: string;
}) {
  if (summaries.length === 0) {
    return <p>No criteria are available on this Evaluation.</p>;
  }
  return (
    <nav className="review-criterion-nav" aria-label="Criteria">
      <ul className="nav-list">
        {summaries.map((item) => {
          const current = item.criterion_id === activeCriterionId;
          return (
            <li key={item.criterion_id}>
              <Link
                className="nav-link"
                to={`/review/${reviewCaseId}/criteria/${encodeURIComponent(item.criterion_id)}`}
                aria-current={current ? "page" : undefined}
              >
                <span className="nav-link-copy">
                  <span className="nav-link-placard">{item.display_label}</span>
                  <span className="nav-link-note">{criterionStatusCopy(item.status)}</span>
                </span>
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}

function criterionInspectRows(
  criterion: ReviewCriterionReadV1,
  reviewCaseId: string,
  restoreEvidenceId?: string,
): ReadoutListRow[] {
  const rows = criterionJudgmentRows(criterion);
  if (criterion.evidence_references.length === 0) {
    rows.push({ term: "Evidence", value: "No Evidence references on this criterion." });
  } else {
    for (const reference of criterion.evidence_references) {
      const restore = restoreEvidenceId === reference.evidence_id;
      rows.push({
        term: "Evidence",
        emphasis: "inline",
        value: (
          <Stack gap="2">
            <Key
              id={restore ? `evidence-ref-${reference.evidence_id}` : undefined}
              variant="inspect"
              size="compact"
              to={`/review/${reviewCaseId}/criteria/${encodeURIComponent(criterion.criterion_id)}/evidence/${encodeURIComponent(reference.evidence_id)}`}
            >
              {OPEN_EVIDENCE}
            </Key>
            <span className="action-note">
              {reference.source_type}
              {" · "}
              {evidencePrecisionCopy(reference.precision)}
            </span>
          </Stack>
        ),
      });
    }
  }
  if (criterion.provisional_feedback) {
    rows.push({ term: "Provisional feedback", value: criterion.provisional_feedback });
  }
  return rows;
}

export function ProductionReviewCasePage() {
  const { reviewId, criterionId, evidenceId } = useParams();
  const location = useLocation();
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
  const evaluationId = record?.evaluation_id;
  usePruneStaleReviewEvaluationCache(scope, reviewId, evaluationId);
  const inspectable = isInspectableReviewState(record?.evaluation_processing_state);
  const summaries = inspectable ? record.criterion_summaries : [];
  const activeCriterionId = criterionId ?? summaries[0]?.criterion_id;
  const inspectingEvidence = Boolean(inspectable && evidenceId);
  const criterionQuery = useReviewCriterionQuery(
    client,
    scope,
    reviewId,
    evaluationId,
    activeCriterionId,
    Boolean(inspectable && activeCriterionId && !inspectingEvidence),
  );
  const evidenceQuery = useReviewEvidenceQuery(
    client,
    scope,
    reviewId,
    evaluationId,
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
            <Stack gap="4">
              <p>{processingWellCopy(record)}</p>
              <ReadoutList
                label="Evaluation processing"
                rows={[
                  { term: "Boundary", value: INTERNAL_EVALUATION_NOTICE, emphasis: "inline" },
                  {
                    term: "Record",
                    value: (
                      <AssignmentRecordReadout
                        variant={processingIndicatorVariant(record.evaluation_processing_state)}
                        label={assignment}
                      />
                    ),
                    emphasis: "inline",
                  },
                ]}
              />
            </Stack>
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
              <ReadoutList
                label="Evidence provenance"
                rows={evidenceProvenanceRows(evidence)}
              />
            ) : null}
          </WorkWellSection>
        </WorkWell>
      </AssignmentStationLayout>
    );
  }

  const criterion = criterionQuery.data;
  const criterionNav = (
    <CriterionNav
      summaries={summaries}
      activeCriterionId={activeCriterionId}
      reviewCaseId={record.review_case_id}
    />
  );

  return (
    <AssignmentStationLayout {...layoutProps}>
      <SplitBay
        className="review-criterion-split"
        drawer={compact}
        start={compact ? undefined : criterionNav}
        toolbar={compact ? criterionNav : undefined}
      >
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
              <ReadoutList
                label="Criterion judgment"
                rows={criterionInspectRows(criterion, record.review_case_id, restoreEvidenceId)}
              />
            ) : (
              <p>Select a criterion to inspect its judgment and Evidence.</p>
            )}
          </WorkWellSection>
        </WorkWell>
      </SplitBay>
    </AssignmentStationLayout>
  );
}
