import type { OperatorIdentity, OperatorRole } from "../../design-system";

export function productionOperatorRole(
  relationship: string | undefined,
  destinationIds: string[],
): OperatorRole {
  const rel = relationship?.toLowerCase() ?? "";
  if (rel.includes("review")) {
    return "Reviewer";
  }
  if (rel.includes("admin")) {
    return "Administrator";
  }
  if (rel.includes("participant")) {
    return "Participant";
  }
  if (destinationIds.includes("activities")) {
    return "Administrator";
  }
  return "Participant";
}

export function productionOperatorIdentity(
  relationship: string | undefined,
  destinationIds: string[],
  displayName?: string | null,
): OperatorIdentity {
  const role = productionOperatorRole(relationship, destinationIds);
  const seated = displayName?.trim() || role;
  return {
    shortId: seated,
    fullId: seated,
    role,
    home: "/",
  };
}
