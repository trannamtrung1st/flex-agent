import { ProductionApiError } from "./production-api";
import type { AssessmentSetupView } from "../pages/AssessmentSetupPage";

export interface ProductionSourceRef {
  source_id: string;
  version_id: string;
  content_digest: string;
}

export interface ProductionSourceOption extends ProductionSourceRef {
  category: string;
  source_kind: string;
  production_eligible: boolean;
}

export interface ProductionActivitySummary {
  activity_id: string;
  title: string;
  revision_number: number;
  has_activated_cohort: boolean;
}

export interface ProductionActivityList {
  activities: ProductionActivitySummary[];
  permitted_actions: string[];
}

export interface ProductionActivityDetail {
  activity_id: string;
  title: string;
  revision_id: string;
  revision_number: number;
  memory_mode: string;
  has_activated_cohort: boolean;
  task_title?: string | null;
  timing?: {
    time_zone_id: string;
    attempt_limit: number;
    starts_at_utc: string;
    ends_at_utc: string;
    deadline_utc: string;
    per_attempt_duration_seconds?: number | null;
  } | null;
  disabled_capabilities?: string[] | null;
  cohort_id?: string | null;
  cohort_state?: string | null;
  baseline_digest?: string | null;
  verification_status?: string | null;
  sources?: Record<string, ProductionSourceRef | ProductionSourceRef[]>;
  permitted_actions: string[];
}

export interface ProductionReadinessResult {
  succeeded: boolean;
  outcome_code: string;
  overall_severity?: string;
  issues?: Array<{
    category: string;
    severity: string;
    reason_code: string;
    recovery_hint: string;
  }>;
}

export interface ProductionActivationOutcome {
  succeeded: boolean;
  outcome_code: string;
  cohort_state?: string;
  baseline_id?: string | null;
  baseline_digest?: string | null;
}

const pendingActivationKeys = new Map<string, string>();

function createIdempotencyKey() {
  return `act-${crypto.randomUUID()}`;
}

export function isAssessmentAccessLoss(error: unknown): error is ProductionApiError {
  return error instanceof ProductionApiError
    && (error.status === 401 || error.status === 403 || error.outcomeCode === "assessment.denied");
}

function throwAssessmentAccessLoss(error: ProductionApiError): never {
  if (error.status === 401 || error.status === 403) {
    throw error;
  }

  throw new ProductionApiError(error.status, "Your access changed", error.outcomeCode);
}

export const REQUIRED_SOURCE_CATEGORIES = [
  "organization_policy",
  "agent",
  "harness",
  "workflow",
  "adaptive_follow_up",
  "rubric_evaluation",
  "model_deployment",
  "capability",
  "review_release",
  "task_submission",
] as const;

export function sourceOptionIdentity(source: Pick<ProductionSourceRef, "source_id" | "version_id">) {
  return `${source.source_id}:${source.version_id}`;
}

export function sourceOptionLabel(source: ProductionSourceOption) {
  const name = source.source_kind.replaceAll("_", " ");
  const status = source.production_eligible ? "available" : "development only";
  return `${name} · ${source.version_id} · ${status}`;
}

export function resolveSelectedSources(
  sources: ProductionSourceOption[],
  selected: Record<string, string>,
  categories: readonly string[],
): Record<string, ProductionSourceRef> {
  const chosen: Record<string, ProductionSourceRef> = {};
  for (const category of categories) {
    const match = sources.find(
      (source) => source.category === category && sourceOptionIdentity(source) === selected[category],
    );
    if (match) {
      chosen[category] = match;
    }
  }

  return chosen;
}

function sourceFields(prefix: string, source: ProductionSourceRef | undefined) {
  return {
    [`${prefix}_source_id`]: source?.source_id,
    [`${prefix}_version_id`]: source?.version_id,
    [`${prefix}_digest`]: source?.content_digest,
  };
}

export function mapActivityToSetupView(
  activity: ProductionActivityDetail,
  readiness?: ProductionReadinessResult,
): AssessmentSetupView {
  const sources = activity.sources
    ? Object.entries(activity.sources).flatMap(([category, value]) => {
        const items = Array.isArray(value) ? value : [value];
        return items.map((source) => ({
          category,
          source_id: source.source_id,
          version_id: source.version_id,
          content_digest: source.content_digest,
        }));
      })
    : [];

  return {
    activity_id: activity.activity_id,
    title: activity.title,
    revision_id: activity.revision_id,
    revision_number: activity.revision_number,
    memory_mode: activity.memory_mode,
    has_activated_cohort: activity.has_activated_cohort,
    task_title: activity.task_title ?? undefined,
    timing: activity.timing ?? undefined,
    disabled_capabilities: activity.disabled_capabilities ?? undefined,
    permitted_actions: activity.permitted_actions,
    cohort_id: activity.cohort_id ?? undefined,
    baseline_digest: activity.baseline_digest ?? undefined,
    verification_status: activity.verification_status ?? undefined,
    overall_severity: readiness?.overall_severity,
    issues: readiness?.issues?.map((issue) => ({
      category: issue.category,
      severity: issue.severity,
      reason_code: issue.reason_code,
      recovery_hint: issue.recovery_hint,
    })),
    sources,
  };
}

export function createProductionAssessmentClient(fetchJson: <T>(path: string, init?: RequestInit) => Promise<T>) {
  const loadActivity = (activityId: string) =>
    fetchJson<ProductionActivityDetail>(`/v1/assessment/activities/${activityId}`);

  return {
    listActivities: (signal?: AbortSignal) =>
      fetchJson<ProductionActivityList>("/v1/assessment/activities", signal ? { signal } : undefined),
    listSourceOptions: (signal?: AbortSignal) =>
      fetchJson<{ sources: ProductionSourceOption[] }>(
        "/v1/assessment/source-options",
        signal ? { signal } : undefined,
      ),
    createActivity: async (title: string, sources: Partial<Record<string, ProductionSourceRef>>) => {
      const created = await fetchJson<{
        succeeded: boolean;
        activity_id?: string;
      }>("/v1/assessment/activities", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          title,
          ...sourceFields("organization_policy", sources.organization_policy),
          ...sourceFields("agent", sources.agent),
          ...sourceFields("harness", sources.harness),
          ...sourceFields("workflow", sources.workflow),
          ...sourceFields("adaptive_follow_up", sources.adaptive_follow_up),
          ...sourceFields("rubric", sources.rubric_evaluation),
          ...sourceFields("model", sources.model_deployment),
          ...sourceFields("capability", sources.capability),
          ...sourceFields("review", sources.review_release),
          ...sourceFields("task", sources.task_submission),
        }),
      });
      if (!created.succeeded || !created.activity_id) {
        throw new ProductionApiError(400, "The Campaign could not be created.");
      }

      return created.activity_id;
    },
    loadSetup: async (activityId: string) => mapActivityToSetupView(await loadActivity(activityId)),
    saveDraft: async (activityId: string, title: string, expectedRevision: number) => {
      try {
        await fetchJson("/v1/assessment/activities/" + activityId, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            title,
            expected_revision_number: expectedRevision,
          }),
        });
      } catch (error) {
        if (isAssessmentAccessLoss(error)) {
          throwAssessmentAccessLoss(error);
        }

        if (error instanceof ProductionApiError
          && (error.outcomeCode === "assessment.stale_revision" || error.message === "This draft changed")) {
          throw new ProductionApiError(409, "This draft changed", error.outcomeCode);
        }

        throw error;
      }

      return mapActivityToSetupView(await loadActivity(activityId));
    },
    checkReadiness: async (activityId: string) => {
      try {
        const readiness = await fetchJson<ProductionReadinessResult>(
          `/v1/assessment/activities/${activityId}/readiness`,
          { method: "POST" },
        );
        return mapActivityToSetupView(await loadActivity(activityId), readiness);
      } catch (error) {
        if (isAssessmentAccessLoss(error)) {
          throwAssessmentAccessLoss(error);
        }

        throw error;
      }
    },
    activateCohort: async (activityId: string, view: AssessmentSetupView) => {
      if (!view.cohort_id || !view.revision_id) {
        throw new ProductionApiError(400, "Activation is not available.");
      }

      const pendingKey = `${activityId}:${view.cohort_id}:${view.revision_id}:${String(view.revision_number)}`;
      const idempotencyKey = pendingActivationKeys.get(pendingKey) ?? createIdempotencyKey();
      pendingActivationKeys.set(pendingKey, idempotencyKey);
      try {
        const outcome = await fetchJson<ProductionActivationOutcome>(
          `/v1/assessment/activities/${activityId}/cohorts/${view.cohort_id}/activate`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              expected_revision_id: view.revision_id,
              expected_revision_number: view.revision_number,
              idempotency_key: idempotencyKey,
            }),
          },
        );
        if (!outcome.succeeded) {
          throw new ProductionApiError(409, "Activation did not complete. Reconcile before retrying.");
        }
      } catch (error) {
        if (isAssessmentAccessLoss(error)) {
          throwAssessmentAccessLoss(error);
        }

        try {
          const reconciled = await fetchJson<ProductionActivationOutcome>(
            `/v1/assessment/activities/${activityId}/cohorts/${view.cohort_id}/activation?idempotency_key=${encodeURIComponent(idempotencyKey)}`,
          );
          if (!reconciled.succeeded) {
            throw error;
          }
        } catch (reconcileError) {
          if (isAssessmentAccessLoss(reconcileError)) {
            throwAssessmentAccessLoss(reconcileError);
          }

          throw error instanceof Error
            ? error
            : new ProductionApiError(409, "Activation did not complete. Reconcile before retrying.");
        }
      }

      pendingActivationKeys.delete(pendingKey);
      return mapActivityToSetupView(await loadActivity(activityId));
    },
  };
}
