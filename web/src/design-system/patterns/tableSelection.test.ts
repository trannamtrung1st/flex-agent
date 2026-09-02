import {
  deriveHeaderSelectionState,
  EMPTY_SELECTION,
  headerSelectionLabel,
  resolveActionIds,
  selectAllMatching,
  selectionCopy,
  transitionHeaderSelection,
} from "./tableSelection";

describe("table selection capability", () => {
  const pageIds = ["a", "b"];

  it("cycles a page-only header without treating the page as the matching set", () => {
    const page = transitionHeaderSelection(EMPTY_SELECTION, pageIds, { mode: "page" });
    expect(page).toEqual({ mode: "explicit", ids: ["a", "b"] });
    expect(deriveHeaderSelectionState(page, pageIds, { mode: "page" })).toBe("page");
    expect(headerSelectionLabel("page", pageIds, { mode: "page" }, page, "participants").tooltip).toBe("Clear selection.");
    expect(transitionHeaderSelection(page, pageIds, { mode: "page" })).toEqual(EMPTY_SELECTION);
  });

  it("selects matching scope without requiring current-page identifiers", () => {
    const matching = selectAllMatching("q:alpha", { total: 40 });
    expect(matching).toEqual({ mode: "matching", queryKey: "q:alpha", total: 40, excludedIds: [] });
    expect(() => resolveActionIds(matching)).toThrow(/cannot be resolved from the current page/i);
    expect(selectionCopy(matching, pageIds, undefined, "campaigns").label).toBe("40 matching selected");
    expect(selectionCopy(selectAllMatching("q:alpha"), pageIds, undefined, "campaigns").label).toBe("Matching selected");
  });

  it("clears matching selection when the query changes", () => {
    const selected = selectAllMatching("q:alpha", { matchingIds: ["a", "b", "c"] });
    expect(transitionHeaderSelection(
      { mode: "explicit", ids: pageIds },
      pageIds,
      { mode: "matching", queryKey: "q:alpha", matchingIds: ["a", "b", "c"], total: 3 },
    )).toEqual(selected);
  });
});
