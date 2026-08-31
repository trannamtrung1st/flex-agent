import type { CSSProperties, KeyboardEvent, ReactNode, Ref } from "react";
import { mergeOverlayRefs } from "../overlays/AnchoredOverlay";
import { SelectPanelFoot } from "./SelectPanelFoot";

export function SearchableSelectPanel({
  open,
  panelId,
  panelRef,
  className,
  searchId,
  searchRef,
  searchValue,
  searchPlaceholder,
  listboxId,
  listRef,
  listClassName,
  listLabel,
  labelledBy,
  multiSelectable,
  activeDescendant,
  meta,
  visibleCount,
  emptyMessage,
  children,
  footLeading,
  onSearchChange,
  onSearchKeyDown,
  onDone,
  doneLabel = "Done",
  style,
}: {
  open: boolean;
  panelId?: string;
  panelRef?: Ref<HTMLElement | null>;
  className: string;
  style?: CSSProperties;
  searchId: string;
  searchRef: Ref<HTMLInputElement>;
  searchValue: string;
  searchPlaceholder: string;
  listboxId: string;
  listRef?: Ref<HTMLUListElement>;
  listClassName: string;
  listLabel?: string;
  labelledBy?: string;
  multiSelectable?: boolean;
  activeDescendant?: string;
  meta: ReactNode;
  visibleCount: number;
  emptyMessage: string;
  children: ReactNode;
  footLeading?: ReactNode;
  onSearchChange: (value: string) => void;
  onSearchKeyDown: (event: KeyboardEvent<HTMLInputElement>) => void;
  onDone: () => void;
  doneLabel?: string;
}) {
  return (
    <div
      ref={mergeOverlayRefs(panelRef)}
      id={panelId}
      className={className}
      style={style}
      hidden={!open}
    >
      <div className="multiselect-search">
        <label className="visually-hidden" htmlFor={searchId}>{searchPlaceholder}</label>
        <input
          ref={searchRef}
          id={searchId}
          type="search"
          role="combobox"
          placeholder={searchPlaceholder}
          autoComplete="off"
          spellCheck={false}
          value={searchValue}
          aria-autocomplete="list"
          aria-expanded={open}
          aria-controls={listboxId}
          aria-activedescendant={activeDescendant}
          onChange={(event) => onSearchChange(event.target.value)}
          onKeyDown={onSearchKeyDown}
        />
      </div>
      <div className="multiselect-meta" aria-live="polite">
        {meta}
      </div>
      <ul
        ref={listRef}
        id={listboxId}
        className={listClassName}
        role="listbox"
        aria-label={listLabel}
        aria-labelledby={labelledBy}
        aria-multiselectable={multiSelectable || undefined}
        hidden={visibleCount === 0}
      >
        {children}
      </ul>
      <p className="multiselect-empty" hidden={visibleCount !== 0}>{emptyMessage}</p>
      <SelectPanelFoot leading={footLeading} doneLabel={doneLabel} onDone={onDone} />
    </div>
  );
}
