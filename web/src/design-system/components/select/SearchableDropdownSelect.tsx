import { useId, useMemo, useRef, type KeyboardEvent } from "react";
import { ChevronGlyph } from "../glyphs/ChevronGlyph";
import { AnchoredOverlay } from "../overlays/AnchoredOverlay";
import { overlayPlateClass } from "../overlays/overlayPlate";
import { SearchableSelectPanel } from "./SearchableSelectPanel";
import { filterOptionIndices, optionNounCount, pinIndex, stepVisibleIndex } from "./selectLogic";
import { selectShellStyle, type SelectPopoverConfig } from "./selectShell";
import { useOverlayDismiss } from "../overlays/useOverlayDismiss";
import { useSearchableSelect } from "./useSearchableSelect";

export function SearchableDropdownSelect({
  labelId,
  value,
  options,
  onChange,
  disabled,
  frozen,
  id,
  popover,
  caseSensitive = false,
  searchPlaceholder = "Filter options",
  listLabel,
  emptyMessage = "No options match this filter. Revise the search term.",
  optionNoun = "option",
  searchId: searchIdProp,
  listboxId: listboxIdProp,
  optionId,
  valueId: valueIdProp,
}: {
  labelId: string;
  value: string;
  options: string[];
  onChange: (value: string) => void;
  disabled?: boolean;
  frozen?: boolean;
  id?: string;
  popover?: SelectPopoverConfig;
  caseSensitive?: boolean;
  searchPlaceholder?: string;
  listLabel?: string;
  emptyMessage?: string;
  optionNoun?: string;
  searchId?: string;
  listboxId?: string;
  optionId?: (option: string, index: number) => string;
  valueId?: string;
}) {
  const keyRef = useRef<HTMLButtonElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLUListElement>(null);
  const rootRef = useRef<HTMLDivElement>(null);
  const uid = useId();
  const searchId = searchIdProp ?? `${uid}-search`;
  const listboxId = listboxIdProp ?? `${uid}-listbox`;
  const valueId = valueIdProp ?? (id ? id.replace(/Select$/, "Value") : `${uid}-value`);
  const inert = Boolean(disabled || frozen);
  const { open, search, setSearch, focusIdx, setFocusIdx, close, openPanel } = useSearchableSelect({
    disabled: inert,
    triggerRef: keyRef,
    searchRef,
  });

  const visibleIndices = useMemo(() => {
    const filtered = filterOptionIndices(options, search, caseSensitive, (option) => [option]);
    return pinIndex(filtered, options.indexOf(value));
  }, [caseSensitive, options, search, value]);

  useOverlayDismiss(open, [rootRef, panelRef], () => close(), { labelId, controlId: id });

  const selectOption = (opt: string) => {
    onChange(opt);
    close(true);
  };

  const moveFocus = (step: number) => {
    const next = stepVisibleIndex(visibleIndices, focusIdx, step);
    if (next === undefined) return;
    setFocusIdx(next);
    listRef.current?.querySelector<HTMLElement>(`[data-option-index="${next}"]`)?.focus();
  };

  const onSearchKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "ArrowDown") {
      e.preventDefault();
      moveFocus(1);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      moveFocus(-1);
    } else if (e.key === "Escape") {
      e.preventDefault();
      close(true);
    }
  };

  const onOptionKeyDown = (e: KeyboardEvent<HTMLLIElement>, index: number) => {
    if (e.key === "ArrowDown") {
      e.preventDefault();
      moveFocus(1);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      moveFocus(-1);
    } else if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      selectOption(options[index]);
    } else if (e.key === "Escape") {
      e.preventDefault();
      close(true);
    } else if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) {
      searchRef.current?.focus();
      setSearch((prev) => prev + e.key);
    }
  };

  return (
    <div
      className={`searchable-select dropdown select-shell select-shell--field${frozen ? " is-frozen" : ""}`}
      ref={rootRef}
      style={selectShellStyle(popover)}
    >
      <button
        ref={keyRef}
        className="searchable-select-key dropdown-key select-trigger select-trigger--field"
        type="button"
        id={id}
        disabled={inert}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={open ? listboxId : undefined}
        aria-labelledby={`${labelId} ${valueId}`}
        onClick={() => (open ? close(true) : openPanel())}
        onKeyDown={(e) => {
          if (inert) return;
          if (!open && (e.key === "Enter" || e.key === " ")) {
            e.preventDefault();
            openPanel();
          } else if ((e.key === "ArrowDown" || e.key === "ArrowUp") && !open) {
            e.preventDefault();
            openPanel();
          }
          if (e.key === "Escape" && open) {
            e.preventDefault();
            close(true);
          }
        }}
      >
        <span className="searchable-select-value dropdown-value select-value" id={valueId}>{value}</span>
        <ChevronGlyph className="dropdown-chevron chevron-glyph" />
      </button>
      <AnchoredOverlay open={open} triggerRef={keyRef} tokenSourceRef={rootRef} floatingRef={panelRef} align="stretch">
        {({ ref, style, overlayClassName }) => (
      <SearchableSelectPanel
        open={open}
        panelRef={ref}
        style={style}
        className={overlayPlateClass("searchable-select-panel", "multiselect-panel", "dropdown-menu", overlayClassName)}
        searchId={searchId}
        searchRef={searchRef}
        searchValue={search}
        searchPlaceholder={searchPlaceholder}
        listboxId={listboxId}
        listRef={listRef}
        listClassName="searchable-select-options"
        listLabel={listLabel ?? "Options"}
        activeDescendant={
          focusIdx >= 0 && visibleIndices.includes(focusIdx) ? (optionId?.(options[focusIdx], focusIdx) ?? `${uid}-opt-${focusIdx}`) : undefined
        }
        meta={<span>{optionNounCount(visibleIndices.length, optionNoun)}</span>}
        visibleCount={visibleIndices.length}
        emptyMessage={emptyMessage}
        onSearchChange={(next) => {
          setSearch(next);
          setFocusIdx(-1);
        }}
        onSearchKeyDown={onSearchKeyDown}
        doneLabel="Close"
        onDone={() => close(true)}
      >
        {options.map((opt, index) => {
          const hidden = !visibleIndices.includes(index);
          return (
            <li
              key={opt}
              id={optionId?.(opt, index) ?? `${uid}-opt-${index}`}
              data-option-index={index}
              role="option"
              aria-selected={opt === value}
              hidden={hidden}
              tabIndex={focusIdx === index && !hidden ? 0 : -1}
              className={focusIdx === index && !hidden ? "is-focused" : undefined}
              onClick={() => selectOption(opt)}
              onFocus={() => setFocusIdx(index)}
              onKeyDown={(e) => onOptionKeyDown(e, index)}
            >
              {opt}
            </li>
          );
        })}
      </SearchableSelectPanel>
        )}
      </AnchoredOverlay>
    </div>
  );
}
