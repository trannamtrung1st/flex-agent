import { useCallback, useEffect, useId, useMemo, useRef, useState } from "react";
import { ChevronGlyph } from "../glyphs/ChevronGlyph";
import { AnchoredOverlay } from "../overlays/AnchoredOverlay";
import { overlayPlateClass } from "../overlays/overlayPlate";
import { selectShellStyle, type SelectPopoverConfig } from "./selectShell";
import { useOverlayDismiss } from "../overlays/useOverlayDismiss";

export function DisclosureMenu({
  label,
  value,
  selectedId,
  options,
  onSelect,
  disabled,
  variant = "toolbar",
  popover,
  ariaLabel,
  keyId,
  menuId,
  valueId,
}: {
  label: string;
  value: string;
  selectedId: string;
  options: { id: string; label: string }[];
  onSelect: (id: string) => void;
  disabled?: boolean;
  variant?: "toolbar" | "context";
  popover?: SelectPopoverConfig;
  ariaLabel: string;
  keyId?: string;
  menuId?: string;
  valueId?: string;
}) {
  const [open, setOpen] = useState(false);
  const keyRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLUListElement>(null);
  const rootRef = useRef<HTMLDivElement>(null);
  const uid = useId();
  const selectedIdx = Math.max(
    0,
    options.findIndex((opt) => opt.id === selectedId),
  );

  const focusOption = useCallback((idx: number) => {
    const items = Array.from(menuRef.current?.querySelectorAll<HTMLElement>("[role='option']") ?? []);
    items.forEach((li, i) => {
      li.tabIndex = i === idx ? 0 : -1;
      if (i === idx) li.focus();
    });
  }, []);

  useOverlayDismiss(open, [rootRef, menuRef], () => setOpen(false));

  useEffect(() => {
    if (!open) return;
    const frame = requestAnimationFrame(() => focusOption(selectedIdx));
    return () => cancelAnimationFrame(frame);
  }, [focusOption, open, selectedIdx]);

  return (
    <div
      ref={rootRef}
      className={`toolbar-seg select-shell select-shell--${variant}`}
      style={selectShellStyle(popover)}
    >
      <button
        ref={keyRef}
        className={`seg-key select-trigger select-trigger--${variant}`}
        type="button"
        id={keyId ?? uid}
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => {
          if (!disabled) setOpen((v) => !v);
        }}
        onKeyDown={(e) => {
          if (disabled) return;
          if (!open && (e.key === "Enter" || e.key === " " || e.key === "ArrowDown")) {
            e.preventDefault();
            setOpen(true);
          }
          if (e.key === "Escape") setOpen(false);
        }}
      >
        <span className="seg-label">{label}</span>
        <span className="seg-value" id={valueId}>{value}</span>
        <ChevronGlyph />
      </button>
      <AnchoredOverlay open={open} triggerRef={rootRef} tokenSourceRef={rootRef} floatingRef={menuRef}>
        {({ ref, style, overlayClassName }) => (
      <ul
        ref={ref}
        style={style}
        className={overlayPlateClass("seg-menu", "option-menu", overlayClassName)}
        role="listbox"
        id={menuId}
        aria-label={ariaLabel}
        hidden={!open}
        onKeyDown={(e) => {
          const items = Array.from(menuRef.current?.querySelectorAll<HTMLElement>("[role='option']") ?? []);
          const idx = items.indexOf(document.activeElement as HTMLElement);
          if (e.key === "ArrowDown") {
            e.preventDefault();
            focusOption(Math.min(idx + 1, items.length - 1));
          }
          if (e.key === "ArrowUp") {
            e.preventDefault();
            focusOption(Math.max(idx - 1, 0));
          }
          if (e.key === "Escape") {
            setOpen(false);
            keyRef.current?.focus();
          }
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            (document.activeElement as HTMLElement)?.click();
          }
        }}
      >
        {options.map((opt) => (
          <li
            key={opt.id}
            role="option"
            aria-selected={opt.id === selectedId}
            onClick={() => {
              onSelect(opt.id);
              setOpen(false);
              keyRef.current?.focus();
            }}
          >
            {opt.label}
          </li>
        ))}
      </ul>
        )}
      </AnchoredOverlay>
    </div>
  );
}

export type DropdownSelectOption = string | { value: string; label: string };

function normalizeDropdownOptions(options: DropdownSelectOption[]) {
  return options.map((option) =>
    typeof option === "string" ? { value: option, label: option } : option,
  );
}

type DropdownSelectBaseProps = {
  labelId: string;
  options: DropdownSelectOption[];
  disabled?: boolean;
  frozen?: boolean;
  id?: string;
  popover?: SelectPopoverConfig;
  describedBy?: string;
  valueId?: string;
  variant?: "field" | "toolbar";
};

type RequiredDropdownSelectProps = DropdownSelectBaseProps & {
  clearable?: false;
  value: string;
  onChange: (value: string) => void;
  placeholder?: never;
  clearLabel?: never;
};

type ClearableDropdownSelectProps = DropdownSelectBaseProps & {
  clearable: true;
  value: string | null;
  onChange: (value: string | null) => void;
  placeholder?: string;
  clearLabel?: string;
};

export type DropdownSelectProps = RequiredDropdownSelectProps | ClearableDropdownSelectProps;

function dropdownValueId(id?: string) {
  if (!id) return undefined;
  return id.endsWith("Select") ? id.replace(/Select$/, "Value") : `${id}Value`;
}

export function DropdownSelect({
  labelId,
  value,
  options,
  onChange,
  disabled,
  frozen,
  id,
  popover,
  describedBy,
  valueId: valueIdProp,
  variant = "field",
  clearable = false,
  placeholder = "Select an option",
  clearLabel = "Clear",
}: DropdownSelectProps) {
  const [open, setOpen] = useState(false);
  const keyRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLUListElement>(null);
  const overlayRef = useRef<HTMLDivElement>(null);
  const clearRef = useRef<HTMLButtonElement>(null);
  const rootRef = useRef<HTMLDivElement>(null);
  const normalizedOptions = useMemo(() => normalizeDropdownOptions(options), [options]);
  const selectedLabel = value === null
    ? placeholder
    : normalizedOptions.find((option) => option.value === value)?.label ?? value;
  const valueId = valueIdProp ?? dropdownValueId(id);
  const inert = Boolean(disabled || frozen);
  const isToolbar = variant === "toolbar";

  const commitValue = (nextValue: string | null) => {
    if (clearable) {
      (onChange as ClearableDropdownSelectProps["onChange"])(nextValue);
    } else if (nextValue !== null) {
      (onChange as RequiredDropdownSelectProps["onChange"])(nextValue);
    }
    setOpen(false);
    keyRef.current?.focus();
  };

  useOverlayDismiss(open, [rootRef, overlayRef], () => setOpen(false), { labelId, controlId: id });

  const focusOption = (idx: number) => {
    const items = Array.from(menuRef.current?.querySelectorAll<HTMLElement>("[role='option']") ?? []);
    items.forEach((li, i) => {
      li.tabIndex = i === idx ? 0 : -1;
      if (i === idx) li.focus();
    });
  };

  return (
    <div
      className={`${isToolbar ? "toolbar-seg" : "dropdown"} select-shell select-shell--${variant}${frozen ? " is-frozen" : ""}`}
      ref={rootRef}
      style={selectShellStyle(popover)}
    >
      <button
        ref={keyRef}
        className={`${isToolbar ? "seg-key" : "dropdown-key"} select-trigger select-trigger--${variant}`}
        type="button"
        id={id}
        disabled={inert}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-labelledby={valueId ? `${labelId} ${valueId}` : labelId}
        aria-describedby={describedBy}
        onClick={() => {
          if (!inert) setOpen((v) => !v);
        }}
        onKeyDown={(e) => {
          if (inert) return;
          if (!open && (e.key === "Enter" || e.key === " ")) {
            e.preventDefault();
            setOpen(true);
            const selectedOptionIdx = normalizedOptions.findIndex((option) => option.value === value);
            requestAnimationFrame(() => focusOption(Math.max(0, selectedOptionIdx)));
          } else if ((e.key === "ArrowDown" || e.key === "ArrowUp") && !open) {
            e.preventDefault();
            setOpen(true);
            requestAnimationFrame(() =>
              focusOption(e.key === "ArrowDown" ? 0 : normalizedOptions.length - 1),
            );
          }
          if (e.key === "Escape") setOpen(false);
        }}
      >
        <span
          className={`${isToolbar ? "seg-value" : "dropdown-value"}${value === null ? " is-placeholder" : ""}`}
          id={valueId}
        >
          {selectedLabel}
        </span>
        {isToolbar ? (
          <ChevronGlyph />
        ) : (
          <svg className="dropdown-chevron chevron-glyph" viewBox="0 0 10 6" aria-hidden="true">
            <path d="M1 1l4 4 4-4" />
          </svg>
        )}
      </button>
      <AnchoredOverlay
        open={open}
        triggerRef={rootRef}
        tokenSourceRef={rootRef}
        floatingRef={overlayRef}
        align={isToolbar ? "start" : "stretch"}
      >
        {({ ref, style, overlayClassName }) => (
      <div
        ref={ref}
        style={style}
        className={overlayPlateClass(isToolbar ? "seg-menu" : "dropdown-menu", overlayClassName)}
        hidden={!open}
      >
        <ul
          ref={menuRef}
          className="option-menu"
          role="listbox"
          aria-label="Options"
          onKeyDown={(e) => {
            const items = Array.from(menuRef.current?.querySelectorAll<HTMLElement>("[role='option']") ?? []);
            const idx = items.indexOf(document.activeElement as HTMLElement);
            if (e.key === "ArrowDown") {
              e.preventDefault();
              if (idx === items.length - 1 && clearable && value !== null) {
                clearRef.current?.focus();
                return;
              }
              focusOption(Math.min(idx + 1, items.length - 1));
            }
            if (e.key === "ArrowUp") {
              e.preventDefault();
              focusOption(Math.max(idx - 1, 0));
            }
            if (e.key === "Escape") {
              e.preventDefault();
              setOpen(false);
              keyRef.current?.focus();
            }
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              (document.activeElement as HTMLElement)?.click();
            }
          }}
        >
          {normalizedOptions.map((opt) => (
            <li
              key={opt.value}
              role="option"
              data-value={opt.value}
              aria-selected={opt.value === value}
              tabIndex={-1}
              onClick={() => commitValue(opt.value)}
            >
              {opt.label}
            </li>
          ))}
        </ul>
        {clearable ? (
          <div className="select-popover-foot">
            <button
              ref={clearRef}
              type="button"
              className="clear-action"
              disabled={value === null}
              onClick={() => commitValue(null)}
              onKeyDown={(e) => {
                if (e.key === "ArrowUp") {
                  e.preventDefault();
                  focusOption(normalizedOptions.length - 1);
                }
                if (e.key === "Escape") {
                  e.preventDefault();
                  setOpen(false);
                  keyRef.current?.focus();
                }
              }}
            >
              {clearLabel}
            </button>
          </div>
        ) : null}
      </div>
        )}
      </AnchoredOverlay>
    </div>
  );
}

