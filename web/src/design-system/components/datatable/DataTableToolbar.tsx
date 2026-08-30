import type { ChangeEventHandler, ReactNode } from "react";

export const SEARCH_ID_PLACEHOLDER = "SEARCH ID";
export const SEARCH_NAME_OR_ID_PLACEHOLDER = "SEARCH NAME OR ID";
export const SEARCH_TITLE_OR_ID_PLACEHOLDER = "SEARCH TITLE OR ID";

export function DataTableToolbar({
  ariaLabel,
  actions,
  leading,
  readout,
  search,
  selection,
}: {
  ariaLabel: string;
  actions?: ReactNode;
  leading?: ReactNode;
  readout?: ReactNode;
  search?: ReactNode;
  selection?: ReactNode;
}) {
  return (
    <div className="datatable-toolbar" aria-label={ariaLabel}>
      {actions}
      <div className="toolbar datatable-toolbar-row datatable-toolbar-row--controls" role="toolbar">
        <div className="toolbar-group toolbar-group--read">
          {leading}
          {readout}
        </div>
        <div className="toolbar-spacer" aria-hidden="true" />
        {search}
      </div>
      {selection}
    </div>
  );
}

export function ToolbarReadout({
  label,
  value,
  valueId,
}: {
  label: ReactNode;
  value: ReactNode;
  valueId: string;
}) {
  return (
    <div className="toolbar-seg toolbar-seg--readout" aria-live="polite">
      <span className="seg-label">{label}</span>
      <span className="seg-value" id={valueId}>
        {value}
      </span>
    </div>
  );
}

export function ToolbarSearch({
  id,
  label,
  placeholder,
  value,
  onChange,
}: {
  id: string;
  label: string;
  placeholder: string;
  value: string;
  onChange: ChangeEventHandler<HTMLInputElement>;
}) {
  return (
    <div className="toolbar-seg toolbar-seg--search">
      <label className="visually-hidden" htmlFor={id}>
        {label}
      </label>
      <input
        className="seg-search"
        id={id}
        type="search"
        placeholder={placeholder}
        autoComplete="off"
        spellCheck={false}
        value={value}
        onChange={onChange}
      />
    </div>
  );
}
