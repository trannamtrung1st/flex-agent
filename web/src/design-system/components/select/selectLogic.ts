export function normalizeSelectText(text: string, caseSensitive: boolean) {
  return caseSensitive ? text : text.toLocaleLowerCase();
}

export function filterOptionIndices<T>(
  options: readonly T[],
  query: string,
  caseSensitive: boolean,
  getTexts: (item: T) => readonly string[],
): number[] {
  const q = normalizeSelectText(query.trim(), caseSensitive);
  return options
    .map((option, index) => ({ option, index }))
    .filter(({ option }) => !q || getTexts(option).some((text) => normalizeSelectText(text, caseSensitive).includes(q)))
    .map(({ index }) => index);
}

/** Keep the committed row visible when the filter would hide it. */
export function pinIndex(visibleIndices: number[], pinned: number): number[] {
  if (pinned < 0 || visibleIndices.includes(pinned)) return visibleIndices;
  return [pinned, ...visibleIndices];
}

/** Preserve the existing searchable-select pluralization, including `endsWith("s")`. */
export function optionNounCount(count: number, optionNoun: string): string {
  return count === 1
    ? `1 ${optionNoun}`
    : `${count} ${optionNoun.endsWith("s") ? `${optionNoun}es` : `${optionNoun}s`}`;
}

export function stepVisibleIndex(visibleIndices: readonly number[], focusIdx: number, step: number): number | undefined {
  if (!visibleIndices.length) return undefined;
  let visibleIdx = visibleIndices.indexOf(focusIdx);
  visibleIdx =
    visibleIdx < 0
      ? step > 0
        ? 0
        : visibleIndices.length - 1
      : Math.max(0, Math.min(visibleIdx + step, visibleIndices.length - 1));
  return visibleIndices[visibleIdx];
}
