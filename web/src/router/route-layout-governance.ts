import type { RouteLeaf } from "./route-leaves";

export function routeLayoutMappingViolations(
  mapped: readonly string[],
  leaves: readonly RouteLeaf[],
): string[] {
  const violations: string[] = [];
  const counts = new Map<string, number>();
  for (const path of mapped) {
    counts.set(path, (counts.get(path) ?? 0) + 1);
  }
  for (const [path, count] of counts) {
    if (count > 1) {
      violations.push(`multiply mapped '${path}'`);
    }
  }

  const mappedSet = new Set(mapped);
  for (const leaf of leaves) {
    if (leaf.redirect) {
      if (mappedSet.has(leaf.path)) {
        violations.push(`redirect '${leaf.path}' has an independent layout`);
      }
      continue;
    }
    if (!mappedSet.has(leaf.path)) {
      violations.push(`unmapped route '${leaf.path}'`);
    }
  }

  return violations;
}
