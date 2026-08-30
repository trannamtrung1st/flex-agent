import { useId, useLayoutEffect, useMemo, useRef, useState, type ComponentProps, type KeyboardEvent } from "react";
import { ChevronGlyph } from "../glyphs/ChevronGlyph";
import { IconButton, Key } from "../keys";
import { selectShellStyle, type SelectPopoverConfig } from "../select/selectShell";
import { useDismissOnOutsidePointer } from "../select/useDismissOnOutsidePointer";
import { DateGlyph, TimeGlyph } from "./TemporalGlyphs";
import {
  WEEKDAYS,
  addMonths,
  calendarCells,
  displayTemporal,
  hourValues,
  joinDateTime,
  minuteValues,
  monthTitle,
  normalizeIsoTime,
  parseIsoDate,
  placeholderFor,
  secondValues,
  shiftIsoDate,
  resolveNowAnchor,
  shiftTime,
  splitDateTime,
  toIsoDate,
  valueForNow,
  viewMonthFrom,
  wheelScrollTop,
  type TemporalMode,
} from "./temporalLogic";

export type { TemporalMode };

function centerWheel(list: HTMLUListElement | null, stamp: string) {
  const item = list?.querySelector<HTMLElement>(`[data-time="${stamp}"]`);
  if (!list || !item) return;
  const itemTop = item.getBoundingClientRect().top - list.getBoundingClientRect().top + list.scrollTop;
  list.scrollTop = wheelScrollTop(itemTop, item.offsetHeight, list.clientHeight);
}

export function DateTimePicker({
  mode = "datetime",
  value,
  onChange,
  id,
  labelId,
  describedBy,
  disabled,
  frozen,
  invalid,
  minuteStep = 1,
  secondStep = 1,
  withSeconds = false,
  now,
  popover,
  valueId: valueIdProp,
}: {
  mode?: TemporalMode;
  value: string;
  onChange: (value: string) => void;
  id?: string;
  labelId: string;
  describedBy?: string;
  disabled?: boolean;
  frozen?: boolean;
  invalid?: boolean;
  minuteStep?: number;
  secondStep?: number;
  withSeconds?: boolean;
  now?: string;
  popover?: SelectPopoverConfig;
  valueId?: string;
}) {
  const uid = useId();
  const today = now ?? toIsoDate(new Date());
  const rootRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const hourRef = useRef<HTMLUListElement>(null);
  const minuteRef = useRef<HTMLUListElement>(null);
  const secondRef = useRef<HTMLUListElement>(null);
  const openedRef = useRef(false);
  const [open, setOpen] = useState(false);
  const inert = Boolean(disabled || frozen);
  const panelId = `${uid}-panel`;
  const gridId = `${uid}-grid`;
  const hourListId = `${uid}-hours`;
  const minuteListId = `${uid}-minutes`;
  const secondListId = `${uid}-seconds`;
  const valueId = valueIdProp ?? (id ? `${id}Value` : `${uid}-value`);
  const display = displayTemporal(mode, value, withSeconds);
  const { date, time } = splitDateTime(value);
  const effectiveTime = normalizeIsoTime(time, withSeconds);
  const hours = useMemo(() => hourValues(), []);
  const minutes = useMemo(() => minuteValues(minuteStep), [minuteStep]);
  const seconds = useMemo(() => secondValues(secondStep), [secondStep]);
  const timeParts = {
    hour: effectiveTime.slice(0, 2),
    minute: effectiveTime.slice(3, 5),
    second: withSeconds ? effectiveTime.slice(6, 8) : "00",
  };
  const timeStepOptions = { minuteStep, secondStep, withSeconds };
  const viewSeed = viewMonthFrom(value, today);
  const [view, setView] = useState(viewSeed);
  const cells = useMemo(() => calendarCells(view.year, view.month), [view.month, view.year]);
  const selectedIso = parseIsoDate(date) ? date : "";

  useDismissOnOutsidePointer(open, rootRef, () => {
    setOpen(false);
  }, { labelId, controlId: id });

  useLayoutEffect(() => {
    if (!open) {
      openedRef.current = false;
      return;
    }
    const justOpened = !openedRef.current;
    openedRef.current = true;
    if (justOpened) {
      if (mode !== "time") {
        const iso = parseIsoDate(date) ? date : today;
        rootRef.current?.querySelector<HTMLButtonElement>(`[data-day="${iso}"]`)?.focus();
      } else {
        hourRef.current?.querySelector<HTMLElement>('[aria-selected="true"]')?.focus();
      }
    }
    if (mode === "date") return;
    centerWheel(hourRef.current, timeParts.hour);
    centerWheel(minuteRef.current, timeParts.minute);
    if (withSeconds) centerWheel(secondRef.current, timeParts.second);
  }, [date, effectiveTime, mode, open, timeParts.hour, timeParts.minute, timeParts.second, today, withSeconds]);

  const close = (restore = false) => {
    setOpen(false);
    if (restore) triggerRef.current?.focus();
  };

  const openPanel = () => {
    if (inert) return;
    setView(viewMonthFrom(value, today));
    setOpen(true);
  };

  const commitDate = (nextDate: string) => {
    if (mode === "date") {
      onChange(nextDate);
      close(true);
      return;
    }
    onChange(joinDateTime(nextDate, effectiveTime, withSeconds));
  };

  const commitTime = (nextTime: string) => {
    if (mode === "time") {
      onChange(nextTime);
      return;
    }
    onChange(joinDateTime(date || today, nextTime, withSeconds));
  };

  const commitNow = () => {
    const anchor = resolveNowAnchor(now);
    const next = valueForNow(mode, anchor, timeStepOptions);
    onChange(next);
    if (mode === "date") {
      close(true);
      return;
    }
    setView(viewMonthFrom(next, today));
  };

  const onTriggerKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    if (inert) return;
    if (!open && (event.key === "Enter" || event.key === " " || event.key === "ArrowDown")) {
      event.preventDefault();
      openPanel();
    } else if (event.key === "Escape" && open) {
      event.preventDefault();
      event.stopPropagation();
      close(true);
    }
  };

  const onDayKeyDown = (event: KeyboardEvent<HTMLButtonElement>, iso: string) => {
    const move: Record<string, number> = {
      ArrowLeft: -1,
      ArrowRight: 1,
      ArrowUp: -7,
      ArrowDown: 7,
    };
    if (event.key in move) {
      event.preventDefault();
      const next = shiftIsoDate(iso, move[event.key]);
      const parsed = parseIsoDate(next);
      if (parsed) setView({ year: parsed.year, month: parsed.month });
      requestAnimationFrame(() => {
        rootRef.current?.querySelector<HTMLButtonElement>(`[data-day="${next}"]`)?.focus();
      });
    } else if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      commitDate(iso);
    } else if (event.key === "PageUp") {
      event.preventDefault();
      setView((current) => addMonths(current.year, current.month, -1));
    } else if (event.key === "PageDown") {
      event.preventDefault();
      setView((current) => addMonths(current.year, current.month, 1));
    } else if (event.key === "Escape") {
      event.preventDefault();
      event.stopPropagation();
      close(true);
    }
  };

  const onTimeKeyDown = (event: KeyboardEvent<HTMLLIElement>, field: "hour" | "minute" | "second") => {
    if (event.key === "ArrowUp") {
      event.preventDefault();
      commitTime(shiftTime(effectiveTime, field, -1, timeStepOptions));
    } else if (event.key === "ArrowDown") {
      event.preventDefault();
      commitTime(shiftTime(effectiveTime, field, 1, timeStepOptions));
    } else if (event.key === "Escape") {
      event.preventDefault();
      event.stopPropagation();
      close(true);
    }
  };

  const Glyph = mode === "time" ? TimeGlyph : DateGlyph;
  const dialogLabel = mode === "date" ? "Choose date" : mode === "time" ? "Choose time" : "Choose date and time";

  return (
    <div
      className={[
        "datetime-picker",
        "select-shell",
        "select-shell--field",
        `select-shell--temporal select-shell--${mode}`,
        frozen ? "is-frozen" : undefined,
        invalid ? "is-invalid" : undefined,
      ]
        .filter(Boolean)
        .join(" ")}
      ref={rootRef}
      style={selectShellStyle(popover)}
    >
      <button
        ref={triggerRef}
        className="dropdown-key select-trigger select-trigger--field datetime-trigger"
        type="button"
        id={id}
        disabled={inert}
        aria-haspopup="dialog"
        aria-expanded={open}
        aria-controls={open ? panelId : undefined}
        aria-invalid={invalid || undefined}
        aria-labelledby={`${labelId} ${valueId}`}
        aria-describedby={describedBy}
        onClick={() => (open ? close(true) : openPanel())}
        onKeyDown={onTriggerKeyDown}
      >
        <Glyph />
        <span className={`dropdown-value select-value${display ? "" : " is-placeholder"}`} id={valueId}>
          {display || placeholderFor(mode)}
        </span>
        <ChevronGlyph className="dropdown-chevron chevron-glyph" />
      </button>
      <div
        id={panelId}
        className={`datetime-popover select-popover popover-surface menu-surface${mode === "datetime" ? " datetime-popover--split" : ""}`}
        role="dialog"
        aria-label={dialogLabel}
        hidden={!open}
      >
        {mode !== "time" ? (
          <div className="datetime-calendar">
            <div className="calendar-head">
              <IconButton
                className="calendar-nav"
                label="Previous month"
                onClick={() => setView((current) => addMonths(current.year, current.month, -1))}
              >
                <ChevronGlyph className="chevron-glyph calendar-nav-chevron calendar-nav-chevron--prev" />
              </IconButton>
              <p className="calendar-title" aria-live="polite">
                {monthTitle(view.year, view.month)}
              </p>
              <IconButton
                className="calendar-nav"
                label="Next month"
                onClick={() => setView((current) => addMonths(current.year, current.month, 1))}
              >
                <ChevronGlyph className="chevron-glyph calendar-nav-chevron calendar-nav-chevron--next" />
              </IconButton>
            </div>
            <div className="calendar-weekdays" aria-hidden="true">
              {WEEKDAYS.map((day) => (
                <span key={day}>{day}</span>
              ))}
            </div>
            <div className="calendar-grid" id={gridId} role="grid" aria-label={monthTitle(view.year, view.month)}>
              {Array.from({ length: cells.length / 7 }, (_, week) => (
                <div className="calendar-week" role="row" key={week}>
                  {cells.slice(week * 7, week * 7 + 7).map((cell) => {
                    const selected = cell.iso === selectedIso;
                    const isToday = cell.iso === today;
                    return (
                      <div role="gridcell" key={cell.iso} aria-selected={selected}>
                        <button
                          type="button"
                          className={[
                            "calendar-day",
                            cell.inMonth ? undefined : "is-muted",
                            selected ? "is-selected" : undefined,
                            isToday ? "is-today" : undefined,
                          ]
                            .filter(Boolean)
                            .join(" ")}
                          data-day={cell.iso}
                          tabIndex={selected || (!selectedIso && cell.iso === today) ? 0 : -1}
                          aria-current={isToday ? "date" : undefined}
                          aria-label={cell.iso}
                          onClick={() => commitDate(cell.iso)}
                          onKeyDown={(event) => onDayKeyDown(event, cell.iso)}
                        >
                          {cell.day}
                        </button>
                      </div>
                    );
                  })}
                </div>
              ))}
            </div>
          </div>
        ) : null}
        {mode !== "date" ? (
          <div className={`datetime-clock${withSeconds ? " datetime-clock--seconds" : ""}`}>
            <div className="time-wheel-labels" aria-hidden="true">
              <span>HH</span>
              <span>MM</span>
              {withSeconds ? <span>SS</span> : null}
            </div>
            <div className="time-wheels">
              <ul
                ref={hourRef}
                id={hourListId}
                className="time-wheel option-menu"
                role="listbox"
                aria-label="Hours"
              >
                {hours.map((hour) => (
                  <li
                    key={hour}
                    role="option"
                    data-time={hour}
                    aria-selected={timeParts.hour === hour}
                    tabIndex={timeParts.hour === hour ? 0 : -1}
                    onClick={() =>
                      commitTime(
                        withSeconds
                          ? `${hour}:${timeParts.minute}:${timeParts.second}`
                          : `${hour}:${timeParts.minute}`,
                      )
                    }
                    onKeyDown={(event) => onTimeKeyDown(event, "hour")}
                  >
                    {hour}
                  </li>
                ))}
              </ul>
              <ul
                ref={minuteRef}
                id={minuteListId}
                className="time-wheel option-menu"
                role="listbox"
                aria-label="Minutes"
              >
                {minutes.map((minute) => (
                  <li
                    key={minute}
                    role="option"
                    data-time={minute}
                    aria-selected={timeParts.minute === minute}
                    tabIndex={timeParts.minute === minute ? 0 : -1}
                    onClick={() =>
                      commitTime(
                        withSeconds
                          ? `${timeParts.hour}:${minute}:${timeParts.second}`
                          : `${timeParts.hour}:${minute}`,
                      )
                    }
                    onKeyDown={(event) => onTimeKeyDown(event, "minute")}
                  >
                    {minute}
                  </li>
                ))}
              </ul>
              {withSeconds ? (
                <ul
                  ref={secondRef}
                  id={secondListId}
                  className="time-wheel option-menu"
                  role="listbox"
                  aria-label="Seconds"
                >
                  {seconds.map((second) => (
                    <li
                      key={second}
                      role="option"
                      data-time={second}
                      aria-selected={timeParts.second === second}
                      tabIndex={timeParts.second === second ? 0 : -1}
                      onClick={() => commitTime(`${timeParts.hour}:${timeParts.minute}:${second}`)}
                      onKeyDown={(event) => onTimeKeyDown(event, "second")}
                    >
                      {second}
                    </li>
                  ))}
                </ul>
              ) : null}
            </div>
          </div>
        ) : null}
        <div className="multiselect-foot datetime-foot">
          <div className="datetime-foot-actions">
            {mode !== "time" ? (
              <button className="clear-action" type="button" onClick={commitNow}>
                Now
              </button>
            ) : null}
            <button
              className="clear-action"
              type="button"
              disabled={!value}
              onClick={() => onChange("")}
            >
              Clear
            </button>
          </div>
          <Key variant="quiet" size="compact" onClick={() => close(true)}>
            Done
          </Key>
        </div>
      </div>
    </div>
  );
}

export function DatePicker(props: Omit<ComponentProps<typeof DateTimePicker>, "mode">) {
  return <DateTimePicker {...props} mode="date" />;
}

export function TimePicker(props: Omit<ComponentProps<typeof DateTimePicker>, "mode">) {
  return <DateTimePicker {...props} mode="time" />;
}
