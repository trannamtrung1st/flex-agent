import { useId, useMemo, useRef, type KeyboardEvent } from "react";
import { ChevronGlyph } from "../glyphs/ChevronGlyph";
import { AnchoredOverlay } from "../overlays/AnchoredOverlay";
import { SearchableSelectPanel } from "./SearchableSelectPanel";
import { filterOptionIndices, stepVisibleIndex } from "./selectLogic";
import { useDismissOnOutsidePointer } from "./useDismissOnOutsidePointer";
import { useSearchableSelect } from "./useSearchableSelect";

export type SearchableMultiSelectOption = {
  value: string;
  label: string;
  id?: string;
};

export type SearchableMultiSelectProps = {
  id?: string;
  labelId: string;
  valueId?: string;
  searchId?: string;
  panelId?: string;
  listboxId?: string;
  options: readonly SearchableMultiSelectOption[];
  values: readonly string[];
  onChange: (values: string[]) => void;
  placeholder?: string;
  emptyLabel?: string;
  optionNoun?: string;
  caseSensitive?: boolean;
  summary?: (selected: SearchableMultiSelectOption[]) => string;
};

export function SearchableMultiSelect({
  id,
  labelId,
  valueId,
  searchId,
  panelId,
  listboxId,
  options,
  values,
  onChange,
  placeholder = "Filter options",
  emptyLabel = "No options match this filter. Revise the search term.",
  optionNoun = "option",
  caseSensitive = false,
  summary,
}: SearchableMultiSelectProps) {
  const uid = useId();
  const resolvedValueId = valueId ?? `${uid}-value`;
  const resolvedSearchId = searchId ?? `${uid}-search`;
  const resolvedPanelId = panelId ?? `${uid}-panel`;
  const resolvedListboxId = listboxId ?? `${uid}-listbox`;
  const rootRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const optionRefs = useRef<Array<HTMLLIElement | null>>([]);
  const { open, search, setSearch, focusIdx, setFocusIdx, close, openPanel } = useSearchableSelect({
    triggerRef,
    searchRef,
    focusOnOpen: "layout",
  });

  const visibleIndices = useMemo(
    () => filterOptionIndices(options, search, caseSensitive, (option) => [option.label, option.value]),
    [caseSensitive, options, search],
  );
  const selected = options.filter((option) => values.includes(option.value));
  const selectedSummary = summary
    ? summary(selected)
    : selected.length === 0
      ? `No ${optionNoun}s selected`
      : selected.length <= 2
        ? selected.map((option) => option.label).join(" · ")
        : `${selected.length} ${optionNoun}s selected`;

  useDismissOnOutsidePointer(open, [rootRef, panelRef], () => {
    const focusWasInside = rootRef.current?.contains(document.activeElement) || panelRef.current?.contains(document.activeElement);
    close(false);
    if (focusWasInside) requestAnimationFrame(() => triggerRef.current?.focus());
  });

  const move = (step: number) => {
    const next = stepVisibleIndex(visibleIndices, focusIdx, step);
    if (next === undefined) return;
    setFocusIdx(next);
    optionRefs.current[next]?.focus();
  };

  const toggle = (value: string) => {
    onChange(values.includes(value) ? values.filter((item) => item !== value) : [...values, value]);
  };

  const onOptionKeyDown = (event: KeyboardEvent<HTMLLIElement>, index: number) => {
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      move(event.key === "ArrowDown" ? 1 : -1);
    } else if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      toggle(options[index].value);
    } else if (event.key === "Escape") {
      event.preventDefault();
      close(true);
    } else if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
      searchRef.current?.focus();
      setSearch((current) => current + event.key);
    }
  };

  const plural = visibleIndices.length === 1 ? optionNoun : `${optionNoun}s`;

  return (
    <div className="multiselect dropdown select-shell select-shell--field" ref={rootRef}>
      <button
        ref={triggerRef}
        className="multiselect-key dropdown-key select-trigger select-trigger--field"
        type="button"
        id={id}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={resolvedPanelId}
        aria-labelledby={`${labelId} ${resolvedValueId}`}
        onClick={() => (open ? close(true) : openPanel())}
        onKeyDown={(event) => {
          if (!open && ["Enter", " ", "ArrowDown", "ArrowUp"].includes(event.key)) {
            event.preventDefault();
            openPanel();
          } else if (open && event.key === "Escape") {
            event.preventDefault();
            close(true);
          }
        }}
      >
        <span className="multiselect-value dropdown-value select-value" id={resolvedValueId}>
          {selectedSummary}
        </span>
        <ChevronGlyph className="dropdown-chevron chevron-glyph" />
      </button>
      <AnchoredOverlay open={open} triggerRef={triggerRef} tokenSourceRef={rootRef} floatingRef={panelRef} align="stretch">
        {({ ref, style, overlayClassName }) => (
      <SearchableSelectPanel
        open={open}
        panelId={resolvedPanelId}
        panelRef={ref}
        style={style}
        className={`multiselect-panel dropdown-menu select-popover popover-surface option-menu ${overlayClassName}`}
        searchId={resolvedSearchId}
        searchRef={searchRef}
        searchValue={search}
        searchPlaceholder={placeholder}
        listboxId={resolvedListboxId}
        listClassName="multiselect-options"
        labelledBy={labelId}
        multiSelectable
        activeDescendant={focusIdx >= 0 ? options[focusIdx].id : undefined}
        meta={(
          <>
            <span>{visibleIndices.length} {plural}</span>
            <span>{selected.length} selected</span>
          </>
        )}
        visibleCount={visibleIndices.length}
        emptyMessage={emptyLabel}
        footLeading={(
          <button
            className="clear-action"
            type="button"
            disabled={selected.length === 0}
            onClick={() => {
              onChange([]);
              searchRef.current?.focus();
            }}
          >
            Clear
          </button>
        )}
        onSearchChange={(next) => {
          setSearch(next);
          setFocusIdx(-1);
        }}
        onSearchKeyDown={(event) => {
          if (event.key === "ArrowDown" || event.key === "ArrowUp") {
            event.preventDefault();
            move(event.key === "ArrowDown" ? 1 : -1);
          } else if (event.key === "Escape") {
            event.preventDefault();
            close(true);
          }
        }}
        onDone={() => close(true)}
      >
        {options.map((option, index) => {
          const hidden = !visibleIndices.includes(index);
          const checked = values.includes(option.value);
          return (
            <li
              ref={(node) => { optionRefs.current[index] = node; }}
              className={`multiselect-option${focusIdx === index && !hidden ? " is-focused" : ""}`}
              id={option.id ?? `${uid}-option-${index}`}
              key={option.value}
              role="option"
              aria-selected={checked}
              tabIndex={focusIdx === index && !hidden ? 0 : -1}
              hidden={hidden}
              onFocus={() => setFocusIdx(index)}
              onClick={() => toggle(option.value)}
              onKeyDown={(event) => onOptionKeyDown(event, index)}
            >
              <span className="select-mark" aria-hidden="true" />
              <span>{option.label}</span>
            </li>
          );
        })}
      </SearchableSelectPanel>
        )}
      </AnchoredOverlay>
    </div>
  );
}
