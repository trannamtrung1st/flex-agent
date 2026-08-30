import { REQUIRED_SOURCE_CATEGORIES } from "../../api/production-assessment";

export const INTENT_SOURCE_CATEGORIES = ["agent", "harness"] as const;

const CATEGORY_LABELS: Record<(typeof REQUIRED_SOURCE_CATEGORIES)[number], string> = {
  organization_policy: "Organization policy",
  agent: "Agent",
  harness: "Harness",
  workflow: "Workflow",
  adaptive_follow_up: "Adaptive follow-up",
  rubric_evaluation: "Rubric",
  model_deployment: "Model",
  capability: "Capability",
  review_release: "Review and Release",
  task_submission: "Task and Submission",
};

const UUID_VERSION = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function sourceCategoryLabel(category: string): string {
  return CATEGORY_LABELS[category as keyof typeof CATEGORY_LABELS] ?? category.replaceAll("_", " ");
}

export function inheritedSourceCategories(): Exclude<
  (typeof REQUIRED_SOURCE_CATEGORIES)[number],
  (typeof INTENT_SOURCE_CATEGORIES)[number]
>[] {
  return REQUIRED_SOURCE_CATEGORIES.filter(
    (category): category is Exclude<
      (typeof REQUIRED_SOURCE_CATEGORIES)[number],
      (typeof INTENT_SOURCE_CATEGORIES)[number]
    > => !(INTENT_SOURCE_CATEGORIES as readonly string[]).includes(category),
  );
}

export function sourceBindingName(sourceKind: string): string {
  return sourceKind
    .replace(/^assessment\./i, "")
    .replaceAll("_", " ")
    .replace(/\.v\d+$/i, "")
    .trim();
}

export function sourceRevisionCaption(versionId: string): string {
  return UUID_VERSION.test(versionId) ? versionId.slice(0, 8) : versionId;
}

export function sourceEligibilityLabel(productionEligible: boolean): string {
  return productionEligible ? "available" : "development";
}

export const LISTED_REVISIONS_DEVELOPMENT_NOTE = "Listed revisions are development only.";

export type CreateSourceEligibilityMode = "silent" | "plate" | "berth";

export function createSourceEligibilityMode(
  selected: Array<{ production_eligible: boolean } | undefined>,
): CreateSourceEligibilityMode {
  const known = selected.filter((option): option is { production_eligible: boolean } => Boolean(option));
  if (known.length === 0) {
    return "silent";
  }

  const developmentCount = known.filter((option) => !option.production_eligible).length;
  if (developmentCount === 0) {
    return "silent";
  }

  return developmentCount === known.length ? "plate" : "berth";
}

export function sourceSelectOptionLabel(
  sourceKind: string,
  versionId: string,
  mode: "full" | "revision" = "full",
): string {
  const revision = sourceRevisionCaption(versionId);
  if (mode === "revision") {
    return revision;
  }

  return `${sourceBindingName(sourceKind)} · ${revision}`;
}
