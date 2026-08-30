import { useId, useMemo, useRef, type KeyboardEvent } from "react";
import { ChevronGlyph } from "../glyphs/ChevronGlyph";
import { AnchoredOverlay } from "../overlays/AnchoredOverlay";
import { SearchableSelectPanel } from "./SearchableSelectPanel";
import { filterOptionIndices, optionNounCount, pinIndex, stepVisibleIndex } from "./selectLogic";
import { selectShellStyle, type SelectPopoverConfig } from "./selectShell";
import { useDismissOnOutsidePointer } from "./useDismissOnOutsidePointer";
import { useSearchableSelect } from "./useSearchableSelect";

export function SearchableDisclosureMenu({
  label,
  value,
  selectedId,
  options,
  onSelect,
  disabled,
  frozen,
  variant = "context",
  popover,
  ariaLabel,
  keyId,
  menuId,
  valueId,
  caseSensitive = false,
  searchPlaceholder = "Filter options",
  emptyMessage = "No options match this filter. Revise the search term.",
  optionNoun = "option",
  searchId: searchIdProp,
  optionId,
}: {
  label: string;
  value: string;
  selectedId: string;
  options: { id: string; label: string }[];
  onSelect: (id: string) => void;
  disabled?: boolean;
  frozen?: boolean;
  variant?: "toolbar" | "context";
  popover?: SelectPopoverConfig;
  ariaLabel: string;
  keyId?: string;
  menuId?: string;
  valueId?: string;
  caseSensitive?: boolean;
  searchPlaceholder?: string;
  emptyMessage?: string;
  optionNoun?: string;
  searchId?: string;
  optionId?: (option: { id: string; label: string }, index: number) => string;
}) {
  const keyRef = useRef<HTMLButtonElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLUListElement>(null);
  const rootRef = useRef<HTMLDivElement>(null);
  const uid = useId();
  const searchId = searchIdProp ?? `${uid}-search`;
  const listboxId = menuId ?? `${uid}-listbox`;
  const triggerId = keyId ?? uid;
  const triggerValueId = valueId ?? `${uid}-value`;
  const inert = Boolean(disabled || frozen);
  const { open, search, setSearch, focusIdx, setFocusIdx, close, openPanel } = useSearchableSelect({
    disabled: inert,
    triggerRef: keyRef,
    searchRef,
  });

  const visibleIndices = useMemo(() => {
    const filtered = filterOptionIndices(options, search, caseSensitive, (opt) => [opt.label, opt.id]);
    return pinIndex(filtered, options.findIndex((opt) => opt.id === selectedId));
  }, [caseSensitive, options, search, selectedId]);

  useDismissOnOutsidePointer(open, [rootRef, panelRef], () => close());

  const selectOption = (id: string) => {
    onSelect(id);
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
      selectOption(options[index].id);
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
      className={`searchable-disclosure toolbar-seg select-shell select-shell--${variant}${frozen ? " is-frozen" : ""}`}
      ref={rootRef}
      style={selectShellStyle(popover)}
    >
      <button
        ref={keyRef}
        className={`seg-key select-trigger select-trigger--${variant}`}
        type="button"
        id={triggerId}
        disabled={inert}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={open ? listboxId : undefined}
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
        <span className="seg-label">{label}</span>
        <span className="seg-value" id={triggerValueId}>{value}</span>
        <ChevronGlyph />
      </button>
      <AnchoredOverlay open={open} triggerRef={keyRef} tokenSourceRef={rootRef} floatingRef={panelRef}>
        {({ ref, style, overlayClassName }) => (
      <SearchableSelectPanel
        open={open}
        panelRef={ref}
        style={style}
        panelId={menuId ? undefined : `${uid}-panel`}
        className={`searchable-disclosure-panel multiselect-panel seg-menu select-popover popover-surface menu-surface option-menu ${overlayClassName}`}
        searchId={searchId}
        searchRef={searchRef}
        searchValue={search}
        searchPlaceholder={searchPlaceholder}
        listboxId={listboxId}
        listRef={listRef}
        listClassName="searchable-disclosure-options"
        listLabel={ariaLabel}
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
        footClassName="searchable-disclosure-foot"
        doneLabel="Close"
        onDone={() => close(true)}
      >
        {options.map((opt, index) => {
          const hidden = !visibleIndices.includes(index);
          return (
            <li
              key={opt.id}
              id={optionId?.(opt, index) ?? `${uid}-opt-${index}`}
              data-option-index={index}
              role="option"
              aria-selected={opt.id === selectedId}
              hidden={hidden}
              tabIndex={focusIdx === index && !hidden ? 0 : -1}
              className={focusIdx === index && !hidden ? "is-focused" : undefined}
              onClick={() => selectOption(opt.id)}
              onFocus={() => setFocusIdx(index)}
              onKeyDown={(e) => onOptionKeyDown(e, index)}
            >
              {opt.label}
            </li>
          );
        })}
      </SearchableSelectPanel>
        )}
      </AnchoredOverlay>
    </div>
  );
}
