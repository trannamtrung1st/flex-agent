export type TableSelection =
  | { mode: "explicit"; ids: string[] }
  | { mode: "matching"; queryKey: string; total?: number; excludedIds: string[] };

export type HeaderSelectionState = "none" | "partial" | "page" | "matching";

export type TableSelectionCapability =
  | { mode: "page" }
  | { mode: "matching"; queryKey: string; total?: number; matchingIds?: readonly string[] };

export const EMPTY_SELECTION: TableSelection = { mode: "explicit", ids: [] };

export function matchingQueryKey(parts: Record<string, string>) {
  return Object.entries(parts)
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([key, value]) => `${key}:${value}`)
    .join("|");
}

export function completeMatchingIds(capability: TableSelectionCapability): readonly string[] | undefined {
  return capability.mode === "matching" ? capability.matchingIds : undefined;
}

export function isSelectionEmpty(selection: TableSelection, matchingIds?: readonly string[]) {
  if (selection.mode === "matching") {
    if (matchingIds === undefined) return selection.total === 0;
    return resolveSelectedIds(selection, matchingIds).length === 0;
  }
  if (matchingIds === undefined) return selection.ids.length === 0;
  return resolveSelectedIds(selection, matchingIds).length === 0;
}

export function isSelected(selection: TableSelection, id: string) {
  if (selection.mode === "explicit") return selection.ids.includes(id);
  return !selection.excludedIds.includes(id);
}

export function resolveSelectedIds(selection: TableSelection, matchingIds: readonly string[]) {
  if (selection.mode === "explicit") {
    const live = new Set(matchingIds);
    return selection.ids.filter((id) => live.has(id));
  }
  const excluded = new Set(selection.excludedIds);
  return matchingIds.filter((id) => !excluded.has(id));
}

export function resolveActionIds(selection: TableSelection, matchingIds?: readonly string[]) {
  if (selection.mode === "matching" && matchingIds === undefined) {
    throw new Error("Matching selection cannot be resolved from the current page.");
  }
  return resolveSelectedIds(selection, matchingIds ?? []);
}

export function normalizeSelection(
  selection: TableSelection,
  matchingIds: readonly string[] | undefined,
  queryKey: string,
): TableSelection {
  if (selection.mode === "matching" && selection.queryKey !== queryKey) {
    return EMPTY_SELECTION;
  }
  if (matchingIds === undefined) {
    return selection.mode === "explicit" && selection.ids.length === 0 ? EMPTY_SELECTION : selection;
  }
  const live = new Set(matchingIds);
  if (selection.mode === "explicit") {
    const ids = selection.ids.filter((id) => live.has(id));
    return ids.length ? { mode: "explicit", ids } : EMPTY_SELECTION;
  }
  const excludedIds = selection.excludedIds.filter((id) => live.has(id));
  if (excludedIds.length >= matchingIds.length) return EMPTY_SELECTION;
  return {
    mode: "matching",
    queryKey,
    total: selection.total ?? matchingIds.length,
    excludedIds,
  };
}

export function deriveHeaderSelectionState(
  selection: TableSelection,
  pageIds: readonly string[],
  capability: TableSelectionCapability,
): HeaderSelectionState {
  if (pageIds.length === 0) return "none";

  const selectedOnPage = pageIds.filter((id) => isSelected(selection, id));
  if (selectedOnPage.length === 0) return "none";
  if (selectedOnPage.length < pageIds.length) return "partial";

  if (capability.mode === "page") return "page";

  const matchingIds = capability.matchingIds;
  if (matchingIds) {
    const allMatchingSelected = matchingIds.length > 0 && matchingIds.every((id) => isSelected(selection, id));
    const hasExclusions = selection.mode === "matching" && selection.excludedIds.length > 0;
    if (allMatchingSelected && !hasExclusions) return "matching";
    return "page";
  }

  if (selection.mode === "matching" && selection.queryKey === capability.queryKey) {
    return "matching";
  }
  return "page";
}

export function headerCheckboxState(scope: HeaderSelectionState) {
  return {
    checked: scope === "page" || scope === "matching",
    indeterminate: scope === "partial",
  };
}

export function transitionHeaderSelection(
  selection: TableSelection,
  pageIds: readonly string[],
  capability: TableSelectionCapability,
): TableSelection {
  const scope = deriveHeaderSelectionState(selection, pageIds, capability);

  switch (scope) {
    case "none":
    case "partial":
      if (selection.mode === "matching") {
        const excluded = new Set(selection.excludedIds);
        pageIds.forEach((id) => excluded.delete(id));
        const matchingIds = completeMatchingIds(capability);
        if (matchingIds && excluded.size >= matchingIds.length) return EMPTY_SELECTION;
        return { ...selection, excludedIds: [...excluded] };
      }
      return togglePage(selection, pageIds, true);
    case "page":
      if (capability.mode === "page") return EMPTY_SELECTION;
      return selectAllMatching(capability.queryKey, {
        total: capability.total,
        matchingIds: capability.matchingIds,
      });
    case "matching":
      return EMPTY_SELECTION;
  }
}

export function headerSelectionLabel(
  scope: HeaderSelectionState,
  pageIds: readonly string[],
  capability: TableSelectionCapability,
  selection: TableSelection,
  noun: string,
) {
  const nounLabel = noun;
  const visibleCount = pageIds.length;
  const matchingIds = completeMatchingIds(capability);
  const matchingCount = capability.mode === "matching"
    ? (capability.total ?? matchingIds?.length)
    : matchingIds?.length;
  const totalSelected = matchingIds ? resolveSelectedIds(selection, matchingIds).length : undefined;
  const offPageExplicit =
    selection.mode === "explicit" &&
    Boolean(matchingIds) &&
    selection.ids.some((id) => !pageIds.includes(id) && matchingIds!.includes(id));
  const showCrossPage =
    scope === "page" && matchingIds !== undefined && (totalSelected !== visibleCount || offPageExplicit);

  switch (scope) {
    case "none":
      return {
        ariaLabel: `No ${nounLabel} on this page selected. Select all visible ${nounLabel}.`,
        tooltip: `Select all visible ${nounLabel}.`,
      };
    case "partial":
      return {
        ariaLabel: `Some ${nounLabel} on this page selected. Select all visible ${nounLabel}.`,
        tooltip: `Select all visible ${nounLabel}.`,
      };
    case "page": {
      if (capability.mode === "page") {
        return {
          ariaLabel: `All ${padCount(visibleCount)} visible ${nounLabel} selected. Clear selection.`,
          tooltip: "Clear selection.",
        };
      }
      const matchingPhrase = matchingCount === undefined
        ? `matching ${nounLabel}`
        : `${padCount(matchingCount)} matching ${nounLabel}`;
      const crossPage =
        showCrossPage && totalSelected !== undefined
          ? `; ${padCount(totalSelected)} selected across matching results`
          : "";
      return {
        ariaLabel: `All ${padCount(visibleCount)} visible ${nounLabel} selected${crossPage}. Select all ${matchingPhrase}.`,
        tooltip: `Select all ${matchingPhrase}.`,
      };
    }
    case "matching": {
      const matchingPhrase = matchingCount === undefined
        ? `matching ${nounLabel}`
        : `${padCount(matchingCount)} matching ${nounLabel}`;
      return {
        ariaLabel: `All ${matchingPhrase} selected. Clear selection.`,
        tooltip: "Clear selection.",
      };
    }
  }
}

export function toggleRow(selection: TableSelection, id: string, checked: boolean): TableSelection {
  if (selection.mode === "explicit") {
    const next = new Set(selection.ids);
    if (checked) next.add(id);
    else next.delete(id);
    return next.size ? { mode: "explicit", ids: [...next] } : EMPTY_SELECTION;
  }
  const excluded = new Set(selection.excludedIds);
  if (checked) excluded.delete(id);
  else excluded.add(id);
  return { ...selection, excludedIds: [...excluded] };
}

export function togglePage(selection: TableSelection, pageIds: readonly string[], checked: boolean): TableSelection {
  if (selection.mode === "explicit") {
    const next = new Set(selection.ids);
    pageIds.forEach((id) => {
      if (checked) next.add(id);
      else next.delete(id);
    });
    return next.size ? { mode: "explicit", ids: [...next] } : EMPTY_SELECTION;
  }
  const excluded = new Set(selection.excludedIds);
  pageIds.forEach((id) => {
    if (checked) excluded.delete(id);
    else excluded.add(id);
  });
  return { ...selection, excludedIds: [...excluded] };
}

export function selectAllMatching(
  queryKey: string,
  options?: { total?: number; matchingIds?: readonly string[] },
): TableSelection {
  if (options?.matchingIds && options.matchingIds.length === 0) return EMPTY_SELECTION;
  return {
    mode: "matching",
    queryKey,
    total: options?.total ?? options?.matchingIds?.length,
    excludedIds: [],
  };
}

export function removeIds(
  selection: TableSelection,
  deletedIds: string[],
  matchingIds: readonly string[] | undefined,
  queryKey: string,
) {
  const deleted = new Set(deletedIds);
  if (selection.mode === "explicit") {
    return normalizeSelection(
      { mode: "explicit", ids: selection.ids.filter((id) => !deleted.has(id)) },
      matchingIds,
      queryKey,
    );
  }
  return normalizeSelection(
    {
      ...selection,
      excludedIds: selection.excludedIds.filter((id) => !deleted.has(id)),
    },
    matchingIds === undefined ? undefined : matchingIds.filter((id) => !deleted.has(id)),
    queryKey,
  );
}

export function selectionCopy(
  selection: TableSelection,
  pageIds: readonly string[],
  matchingIds: readonly string[] | undefined,
  noun: string,
) {
  void noun;
  if (selection.mode === "matching" && matchingIds === undefined) {
    const excluded = selection.excludedIds.length;
    const label = selection.total === undefined
      ? (excluded > 0
        ? `Matching selected · ${padCount(excluded)} excluded`
        : "Matching selected")
      : (excluded > 0
        ? `${padCount(selection.total - excluded)} matching selected · ${padCount(excluded)} excluded`
        : `${padCount(selection.total)} matching selected`);
    return { count: selection.total === undefined ? null : selection.total - excluded, label };
  }

  const ids = resolveSelectedIds(selection, matchingIds ?? (selection.mode === "explicit" ? selection.ids : []));
  const count = ids.length;
  const excluded = selection.mode === "matching" ? selection.excludedIds.length : 0;
  const pageFullySelected = pageIds.length > 0 && pageIds.every((id) => isSelected(selection, id));
  const onlyThisPage =
    selection.mode === "explicit" && pageFullySelected && selection.ids.every((id) => pageIds.includes(id));

  let label = `${padCount(count)} selected`;
  if (selection.mode === "matching") {
    label = excluded > 0
      ? `${padCount(count)} matching selected · ${padCount(excluded)} excluded`
      : `${padCount(count)} matching selected`;
  } else if (onlyThisPage && (matchingIds?.length ?? 0) > pageIds.length) {
    label = `${padCount(count)} selected on this page`;
  }

  return { count, label };
}

export function padCount(n: number) {
  return String(n).padStart(n > 99 ? 3 : 2, "0");
}
