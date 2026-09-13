import type {
  EvidenceLocationV1,
  ReviewCaseReadV1,
  ReviewCriterionReadV1,
  ReviewEvidenceOpenV1,
  ReviewWorkItemV1,
} from "../../contracts/v1";

export const INTERNAL_EVALUATION_NOTICE = "Internal Evaluation · Not a released Result";
export const REVIEW_WORK_TITLE = "Review work";
export const REVIEW_WORK_DESCRIPTION = "Assigned Review cases for the signed-in Reviewer. Open a case to inspect its exact Evaluation and Evidence.";
export const LOADING_REVIEW_WORK = "Loading Review work";
export const EMPTY_REVIEW_WORK = "No Review work is assigned to you";
export const OPEN_EVIDENCE = "Open Evidence";
export const BACK_TO_CRITERION = "Back to criterion";
export const RUNNING_CRITERION_UNAVAILABLE = "Evaluation running. Criterion judgments are not available until completion.";
export const WHOLE_ITEM_PRECISION = "Whole item cited — a finer verified location is unavailable";

const PROCESSING_COPY: Record<ReviewWorkItemV1["evaluation_processing_state"], string> = {
  awaiting: "Awaiting eligible Evaluation",
  queued: "Queued",
  running: "Running",
  retryable_failure: "Failed — retryable",
  review_required: "Review required",
  completed: "Evaluation completed",
};

const ASSIGNMENT_COPY: Record<ReviewWorkItemV1["assignment_state"], string> = {
  assigned: "Assigned to you",
  revoked: "Assignment revoked",
};

const INTEGRITY_COPY: Record<ReviewWorkItemV1["integrity_state"], string> = {
  intact: "Intact",
  lower_precision: "Lower precision",
  integrity_changed: "Integrity changed",
  lawfully_unavailable: "Lawfully unavailable",
};

const CANDIDATE_COPY: Record<NonNullable<ReviewCaseReadV1["candidate_state"]>, string> = {
  none: "No eligible candidate",
  selected: "Selected",
  replacement_available: "Replacement available",
  stale: "Candidate stale",
};

const CRITERION_STATUS_COPY: Record<ReviewCriterionReadV1["status"], string> = {
  unavailable: "Unavailable",
  satisfied: "Satisfied",
  not_satisfied: "Not satisfied",
  insufficient_evidence: "Insufficient evidence",
  not_applicable: "Not applicable",
  conflict: "Conflict",
};

const EVIDENCE_AVAILABILITY_COPY: Record<ReviewEvidenceOpenV1["availability"], string> = {
  available: "Available",
  denied: "Source unavailable",
  unavailable: "Source unavailable",
  integrity_changed: "Integrity warning",
  lower_precision: "Lower precision",
};

const MODE_COPY = {
  deterministic: "Rule-based",
  agent_assisted: "Agent-assisted",
  agent_judgment: "Agent judgment",
} as const;

export function evaluationProcessingCopy(state: ReviewWorkItemV1["evaluation_processing_state"]) {
  return PROCESSING_COPY[state];
}

export function isInspectableReviewState(
  state: ReviewWorkItemV1["evaluation_processing_state"] | undefined,
) {
  return state === "completed" || state === "review_required";
}

export function assignmentCopy(state: ReviewWorkItemV1["assignment_state"]) {
  return ASSIGNMENT_COPY[state];
}

export function integrityCopy(state: ReviewWorkItemV1["integrity_state"]) {
  return INTEGRITY_COPY[state];
}

export function candidateCopy(state: ReviewCaseReadV1["candidate_state"] | undefined) {
  return state ? CANDIDATE_COPY[state] : null;
}

export function criterionStatusCopy(status: ReviewCriterionReadV1["status"]) {
  return CRITERION_STATUS_COPY[status];
}

export function evaluatorModeCopy(
  mode: "deterministic" | "agent_assisted" | "agent_judgment",
  label?: string | null,
) {
  return label || MODE_COPY[mode];
}

export function canonicalEvaluatorModeCopy(mode: ReviewCriterionReadV1["evaluator_mode"]) {
  return mode;
}

export function evaluatorModePresentation(
  mode: ReviewCriterionReadV1["evaluator_mode"],
  label?: string | null,
) {
  const friendly = evaluatorModeCopy(mode, label);
  return friendly === mode ? friendly : `${friendly} (${mode})`;
}

export function verificationStateCopy(
  state: ReviewEvidenceOpenV1["locator"]["integrity"]["verification_state"],
) {
  if (state === "verified") {
    return "Verified";
  }
  if (state === "degraded") {
    return "Degraded";
  }
  return "Failed";
}

export function evidenceLocationCopy(location: EvidenceLocationV1) {
  if (location.location_type === "whole_item") {
    return `Whole item ${location.item_id}`;
  }
  if (location.location_type === "line_range") {
    return `Lines ${location.start_line_inclusive}–${location.end_line_inclusive} of ${location.item_id}`;
  }
  if (location.location_type === "utf8_byte_range") {
    return `Bytes [${location.start_inclusive}, ${location.end_exclusive}) of ${location.item_id}`;
  }
  return `JSON pointer ${location.json_pointer}`;
}

export function evidenceAvailabilityCopy(availability: ReviewEvidenceOpenV1["availability"]) {
  return EVIDENCE_AVAILABILITY_COPY[availability];
}

export function processingWellCopy(record: Pick<ReviewCaseReadV1, "evaluation_processing_state" | "processing_notice">) {
  if (record.evaluation_processing_state === "running" || record.evaluation_processing_state === "queued") {
    return RUNNING_CRITERION_UNAVAILABLE;
  }
  if (record.processing_notice) {
    return record.processing_notice;
  }
  if (record.evaluation_processing_state === "awaiting") {
    return "This Session is awaiting an eligible Evaluation. Criterion judgments are not available.";
  }
  if (record.evaluation_processing_state === "retryable_failure") {
    return "Evaluation failed and can be retried. Criterion judgments are not available.";
  }
  if (record.evaluation_processing_state === "review_required") {
    return "Evaluation requires review before criterion judgments can be treated as complete.";
  }
  return RUNNING_CRITERION_UNAVAILABLE;
}

export function evidencePrecisionCopy(precision: ReviewEvidenceOpenV1["locator"]["precision"] | undefined) {
  if (precision === "whole_item") {
    return WHOLE_ITEM_PRECISION;
  }
  if (precision === "stable_segment") {
    return "Stable segment cited";
  }
  return "Exact range cited";
}

export function caseTitle(record: Pick<ReviewCaseReadV1, "task_label" | "campaign_label">) {
  return record.task_label || record.campaign_label || "Review case";
}

export function workRowTitle(item: Pick<ReviewWorkItemV1, "task_label" | "campaign_label">) {
  return item.task_label || item.campaign_label || "Assigned Review case";
}

export function nextActionCopy(action: ReviewWorkItemV1["next_action"]) {
  return action === "open_review" ? "Open review" : null;
}

export function processingIndicatorVariant(state: ReviewWorkItemV1["evaluation_processing_state"]) {
  if (state === "completed") return "sealed" as const;
  if (state === "running" || state === "queued") return "live" as const;
  if (state === "retryable_failure" || state === "review_required") return "rest" as const;
  return "dim" as const;
}
