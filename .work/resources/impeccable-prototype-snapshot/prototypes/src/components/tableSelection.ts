import type { TableSelection } from "../data/types";

export type { TableSelection };

export type HeaderSelectionState = "none" | "partial" | "page" | "matching";

export const EMPTY_SELECTION: TableSelection = { mode: "explicit", ids: [] };

export function matchingQueryKey(parts: Record<string, string>) {
  return Object.entries(parts)
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([key, value]) => `${key}:${value}`)
    .join("|");
}

export function isSelectionEmpty(selection: TableSelection, matchingIds: string[]) {
  return resolveSelectedIds(selection, matchingIds).length === 0;
}

export function isSelected(selection: TableSelection, id: string) {
  if (selection.mode === "explicit") return selection.ids.includes(id);
  return !selection.excludedIds.includes(id);
}

export function resolveSelectedIds(selection: TableSelection, matchingIds: string[]) {
  if (selection.mode === "explicit") {
    const live = new Set(matchingIds);
    return selection.ids.filter((id) => live.has(id));
  }
  const excluded = new Set(selection.excludedIds);
  return matchingIds.filter((id) => !excluded.has(id));
}

export function normalizeSelection(
  selection: TableSelection,
  matchingIds: string[],
  queryKey: string,
): TableSelection {
  if (selection.mode === "matching" && selection.queryKey !== queryKey) {
    return EMPTY_SELECTION;
  }
  const live = new Set(matchingIds);
  if (selection.mode === "explicit") {
    const ids = selection.ids.filter((id) => live.has(id));
    return ids.length ? { mode: "explicit", ids } : EMPTY_SELECTION;
  }
  const excludedIds = selection.excludedIds.filter((id) => live.has(id));
  if (excludedIds.length >= matchingIds.length) return EMPTY_SELECTION;
  return { mode: "matching", queryKey, total: matchingIds.length, excludedIds };
}

export function deriveHeaderSelectionState(
  selection: TableSelection,
  pageIds: string[],
  matchingIds: string[],
): HeaderSelectionState {
  if (pageIds.length === 0) return "none";

  const selectedOnPage = pageIds.filter((id) => isSelected(selection, id));
  if (selectedOnPage.length === 0) return "none";

  const allMatchingSelected =
    matchingIds.length > 0 && matchingIds.every((id) => isSelected(selection, id));
  const hasExclusions = selection.mode === "matching" && selection.excludedIds.length > 0;

  if (allMatchingSelected && !hasExclusions) return "matching";

  if (selectedOnPage.length < pageIds.length) return "partial";

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
  pageIds: string[],
  matchingIds: string[],
  queryKey: string,
): TableSelection {
  const scope = deriveHeaderSelectionState(selection, pageIds, matchingIds);

  switch (scope) {
    case "none":
    case "partial":
      if (selection.mode === "matching") {
        const excluded = new Set(selection.excludedIds);
        pageIds.forEach((id) => excluded.delete(id));
        if (excluded.size >= matchingIds.length) return EMPTY_SELECTION;
        return { ...selection, excludedIds: [...excluded] };
      }
      return togglePage(selection, pageIds, true);
    case "page":
      return selectAllMatching(matchingIds, queryKey);
    case "matching":
      return EMPTY_SELECTION;
  }
}

export function headerSelectionLabel(
  scope: HeaderSelectionState,
  pageIds: string[],
  matchingIds: string[],
  selection: TableSelection,
  noun: string,
) {
  const nounLabel = noun;
  const visibleCount = pageIds.length;
  const matchingCount = matchingIds.length;
  const totalSelected = resolveSelectedIds(selection, matchingIds).length;
  const offPageExplicit =
    selection.mode === "explicit" &&
    selection.ids.some((id) => !pageIds.includes(id) && matchingIds.includes(id));
  const showCrossPage =
    scope === "page" && (totalSelected !== visibleCount || offPageExplicit);

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
      const crossPage =
        showCrossPage
          ? `; ${padCount(totalSelected)} selected across matching results`
          : "";
      return {
        ariaLabel: `All ${padCount(visibleCount)} visible ${nounLabel} selected${crossPage}. Select all ${padCount(matchingCount)} matching ${nounLabel}.`,
        tooltip: `Select all ${padCount(matchingCount)} matching ${nounLabel}.`,
      };
    }
    case "matching":
      return {
        ariaLabel: `All ${padCount(matchingCount)} matching ${nounLabel} selected. Clear selection.`,
        tooltip: "Clear selection.",
      };
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

export function togglePage(selection: TableSelection, pageIds: string[], checked: boolean): TableSelection {
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

export function selectAllMatching(matchingIds: string[], queryKey: string): TableSelection {
  if (!matchingIds.length) return EMPTY_SELECTION;
  return { mode: "matching", queryKey, total: matchingIds.length, excludedIds: [] };
}

export function removeIds(selection: TableSelection, deletedIds: string[], matchingIds: string[], queryKey: string) {
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
    matchingIds.filter((id) => !deleted.has(id)),
    queryKey,
  );
}

export function selectionCopy(
  selection: TableSelection,
  pageIds: string[],
  matchingIds: string[],
  noun: string,
) {
  void noun;
  const ids = resolveSelectedIds(selection, matchingIds);
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
  } else if (onlyThisPage && matchingIds.length > pageIds.length) {
    label = `${padCount(count)} selected on this page`;
  }

  return { count, label };
}

export function padCount(n: number) {
  return String(n).padStart(n > 99 ? 3 : 2, "0");
}
