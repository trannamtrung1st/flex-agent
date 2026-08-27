import { describe, expect, it } from "vitest";
import {
  EMPTY_SELECTION,
  deriveHeaderSelectionState,
  headerCheckboxState,
  headerSelectionLabel,
  isSelected,
  matchingQueryKey,
  normalizeSelection,
  removeIds,
  resolveSelectedIds,
  selectAllMatching,
  selectionCopy,
  transitionHeaderSelection,
  togglePage,
  toggleRow,
} from "./tableSelection";

const matching = ["a", "b", "c", "d", "e"];
const page = ["a", "b"];

describe("tableSelection", () => {
  it("selects only the visible page from empty", () => {
    const next = togglePage(EMPTY_SELECTION, page, true);
    expect(next).toEqual({ mode: "explicit", ids: ["a", "b"] });
    expect(deriveHeaderSelectionState(next, page, matching)).toBe("page");
    expect(selectionCopy(next, page, matching, "campaigns").label).toBe("02 selected on this page");
  });

  it("escalates to all matching and supports exclusions", () => {
    const all = selectAllMatching(matching, "q");
    expect(resolveSelectedIds(all, matching)).toEqual(matching);
    expect(deriveHeaderSelectionState(all, page, matching)).toBe("matching");
    const excluded = toggleRow(all, "c", false);
    expect(isSelected(excluded, "c")).toBe(false);
    expect(deriveHeaderSelectionState(excluded, page, matching)).toBe("page");
    expect(selectionCopy(excluded, page, matching, "campaigns").label).toBe("04 matching selected · 01 excluded");
  });

  it("transitions header from page to matching to clear", () => {
    const pageSel = togglePage(EMPTY_SELECTION, page, true);
    expect(deriveHeaderSelectionState(pageSel, page, matching)).toBe("page");
    const all = transitionHeaderSelection(pageSel, page, matching, "q");
    expect(deriveHeaderSelectionState(all, page, matching)).toBe("matching");
    const cleared = transitionHeaderSelection(all, page, matching, "q");
    expect(cleared).toEqual(EMPTY_SELECTION);
  });

  it("derives partial when some visible rows are selected", () => {
    const partial = toggleRow(EMPTY_SELECTION, "a", true);
    expect(deriveHeaderSelectionState(partial, page, matching)).toBe("partial");
    expect(headerCheckboxState("partial")).toEqual({ checked: false, indeterminate: true });
  });

  it("clears stale matching selection when the query changes", () => {
    const all = selectAllMatching(matching, "q1");
    expect(normalizeSelection(all, matching, "q2")).toEqual(EMPTY_SELECTION);
  });

  it("prunes deleted ids from explicit and matching selections", () => {
    const explicit = removeIds({ mode: "explicit", ids: ["a", "b"] }, ["a"], matching, "q");
    expect(explicit).toEqual({ mode: "explicit", ids: ["b"] });
    const matchingSel = removeIds(selectAllMatching(matching, "q"), ["a", "b"], matching.filter((id) => id !== "a" && id !== "b"), "q");
    expect(resolveSelectedIds(matchingSel, ["c", "d", "e"])).toEqual(["c", "d", "e"]);
  });

  it("builds a stable query key from filter parts", () => {
    expect(matchingQueryKey({ search: "CMP", activation: "draft" })).toBe("activation:draft|search:CMP");
  });

  it("provides header labels with scope and next action", () => {
    const none = headerSelectionLabel("none", page, matching, EMPTY_SELECTION, "campaigns");
    expect(none.ariaLabel).toContain("Select all visible campaigns");
    const matchingLabel = headerSelectionLabel("matching", page, matching, selectAllMatching(matching, "q"), "campaigns");
    expect(matchingLabel.ariaLabel).toContain("Clear selection");
  });

  it("derives page when every visible row is selected with off-page explicit ids", () => {
    const crossPage = { mode: "explicit" as const, ids: ["a", "b", "c"] };
    expect(deriveHeaderSelectionState(crossPage, page, matching)).toBe("page");
    const copy = selectionCopy(crossPage, page, matching, "campaigns");
    expect(copy.label).toContain("03 selected");
  });

  it("handles an empty page without throwing", () => {
    expect(deriveHeaderSelectionState(EMPTY_SELECTION, [], matching)).toBe("none");
    const next = transitionHeaderSelection(EMPTY_SELECTION, [], matching, "q");
    expect(next).toEqual(EMPTY_SELECTION);
  });

  it("derives matching when explicit selection covers the full filtered set", () => {
    const explicitAll = { mode: "explicit" as const, ids: [...matching] };
    expect(deriveHeaderSelectionState(explicitAll, page, matching)).toBe("matching");
  });

  it("restores matching after exclusions via header transition from page", () => {
    const excluded = toggleRow(selectAllMatching(matching, "q"), "c", false);
    expect(deriveHeaderSelectionState(excluded, page, matching)).toBe("page");
    const restored = transitionHeaderSelection(excluded, page, matching, "q");
    expect(deriveHeaderSelectionState(restored, page, matching)).toBe("matching");
    expect(isSelected(restored, "c")).toBe(true);
  });

  it("keeps matching mode when re-selecting visible rows from partial exclusions", () => {
    const excluded = toggleRow(selectAllMatching(matching, "q"), "a", false);
    expect(deriveHeaderSelectionState(excluded, page, matching)).toBe("partial");
    const restoredVisible = transitionHeaderSelection(excluded, page, matching, "q");
    expect(restoredVisible.mode).toBe("matching");
    expect(isSelected(restoredVisible, "a")).toBe(true);
  });

  it("transitions none to page on first header click", () => {
    const partial = toggleRow(EMPTY_SELECTION, "a", true);
    expect(deriveHeaderSelectionState(partial, page, matching)).toBe("partial");
    const pageSel = transitionHeaderSelection(partial, page, matching, "q");
    expect(deriveHeaderSelectionState(pageSel, page, matching)).toBe("page");
  });

  it("treats a one-page filtered set as matching once fully selected", () => {
    const singlePage = ["a", "b"];
    const pageSel = togglePage(EMPTY_SELECTION, singlePage, true);
    expect(deriveHeaderSelectionState(pageSel, singlePage, singlePage)).toBe("matching");
    const cleared = transitionHeaderSelection(pageSel, singlePage, singlePage, "q");
    expect(cleared).toEqual(EMPTY_SELECTION);
  });
});
