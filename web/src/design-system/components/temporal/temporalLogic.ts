export type TemporalMode = "date" | "time" | "datetime";

export type CalendarCell = {
  iso: string;
  day: number;
  inMonth: boolean;
};

export const WEEKDAYS = ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"] as const;

export const MONTHS = [
  "Jan",
  "Feb",
  "Mar",
  "Apr",
  "May",
  "Jun",
  "Jul",
  "Aug",
  "Sep",
  "Oct",
  "Nov",
  "Dec",
] as const;

const DATE_RE = /^(\d{4})-(\d{2})-(\d{2})$/;
const TIME_PART_RE = /^(\d{2}):(\d{2})(?::(\d{2}))?$/;
const DATETIME_RE = /^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})(?::(\d{2}))?$/;

export type ParsedTime = { hour: number; minute: number; second: number };

export type TimeStepOptions = {
  minuteStep?: number;
  secondStep?: number;
  withSeconds?: boolean;
};

export function pad2(value: number) {
  return String(value).padStart(2, "0");
}

export function toIsoDate(date: Date) {
  return `${date.getFullYear()}-${pad2(date.getMonth() + 1)}-${pad2(date.getDate())}`;
}

export function parseIsoDate(value: string) {
  const match = DATE_RE.exec(value.trim());
  if (!match) return null;
  const year = Number(match[1]);
  const month = Number(match[2]) - 1;
  const day = Number(match[3]);
  const date = new Date(year, month, day);
  if (date.getFullYear() !== year || date.getMonth() !== month || date.getDate() !== day) return null;
  return { year, month, day };
}

export function parseTimePart(value: string): ParsedTime | null {
  const match = TIME_PART_RE.exec(value.trim());
  if (!match) return null;
  const hour = Number(match[1]);
  const minute = Number(match[2]);
  const second = match[3] !== undefined ? Number(match[3]) : 0;
  if (hour > 23 || minute > 59 || second > 59) return null;
  return { hour, minute, second };
}

export function formatIsoTime({ hour, minute, second }: ParsedTime, withSeconds = false) {
  const base = `${pad2(hour)}:${pad2(minute)}`;
  return withSeconds ? `${base}:${pad2(second)}` : base;
}

export function parseIsoTime(value: string, withSeconds = false) {
  const parsed = parseTimePart(value);
  if (!parsed) return null;
  const segments = value.trim().split(":");
  if (withSeconds) {
    if (segments.length !== 3) return null;
    return parsed;
  }
  if (segments.length !== 2) return null;
  return parsed;
}

export function normalizeIsoTime(value: string, withSeconds = false) {
  const parsed = parseTimePart(value);
  if (!parsed) return withSeconds ? "00:00:00" : "00:00";
  return formatIsoTime(parsed, withSeconds);
}

export function splitDateTime(value: string) {
  const match = DATETIME_RE.exec(value.trim());
  if (match) {
    const date = `${match[1]}-${match[2]}-${match[3]}`;
    const time = match[6] !== undefined ? `${match[4]}:${match[5]}:${match[6]}` : `${match[4]}:${match[5]}`;
    if (!parseIsoDate(date) || !parseTimePart(time)) return { date: "", time: "" };
    return { date, time };
  }
  if (parseIsoDate(value)) return { date: value.trim(), time: "" };
  const time = parseTimePart(value);
  if (time) {
    const segments = value.trim().split(":");
    return { date: "", time: segments.length === 3 ? value.trim() : `${pad2(time.hour)}:${pad2(time.minute)}` };
  }
  return { date: "", time: "" };
}

export function joinDateTime(date: string, time: string, withSeconds = false) {
  if (!date) return "";
  return `${date}T${time || (withSeconds ? "00:00:00" : "00:00")}`;
}

export function displayDate(value: string) {
  return parseIsoDate(value) ? value : "";
}

export function displayTime(value: string, withSeconds = false) {
  const parsed = parseTimePart(value);
  if (!parsed) return "";
  return formatIsoTime(parsed, withSeconds);
}

export function displayDateTime(value: string, withSeconds = false) {
  const { date, time } = splitDateTime(value);
  if (!date) return "";
  const mark = time ? normalizeIsoTime(time, withSeconds) : withSeconds ? "00:00:00" : "00:00";
  return `${date} ${mark}`;
}

export function displayTemporal(mode: TemporalMode, value: string, withSeconds = false) {
  if (mode === "date") return displayDate(value);
  if (mode === "time") return displayTime(value, withSeconds);
  return displayDateTime(value, withSeconds);
}

export function placeholderFor(mode: TemporalMode) {
  if (mode === "date") return "Select date";
  if (mode === "time") return "Select time";
  return "Select date and time";
}

export function monthTitle(year: number, month: number) {
  return `${MONTHS[month]} ${year}`;
}

export function addMonths(year: number, month: number, delta: number) {
  const date = new Date(year, month + delta, 1);
  return { year: date.getFullYear(), month: date.getMonth() };
}

export function shiftIsoDate(iso: string, days: number) {
  const parsed = parseIsoDate(iso);
  if (!parsed) return iso;
  return toIsoDate(new Date(parsed.year, parsed.month, parsed.day + days));
}

export function hourValues() {
  return Array.from({ length: 24 }, (_, hour) => pad2(hour));
}

export function minuteValues(step = 1) {
  const size = Math.max(1, Math.min(30, step));
  const count = Math.floor(60 / size);
  return Array.from({ length: count }, (_, index) => pad2(index * size));
}

export function secondValues(step = 1) {
  return minuteValues(step);
}

export function shiftTime(value: string, field: "hour" | "minute" | "second", delta: number, options: TimeStepOptions = {}) {
  const { minuteStep = 1, secondStep = 1, withSeconds = false } = options;
  const parsed = parseTimePart(value) ?? { hour: 0, minute: 0, second: 0 };
  if (field === "hour") {
    return formatIsoTime({ ...parsed, hour: (parsed.hour + delta + 24) % 24 }, withSeconds);
  }
  if (field === "second") {
    const seconds = secondValues(secondStep);
    const current = seconds.includes(pad2(parsed.second)) ? pad2(parsed.second) : seconds[0];
    const index = seconds.indexOf(current);
    const next = seconds[(index + delta + seconds.length) % seconds.length];
    return formatIsoTime({ ...parsed, second: Number(next) }, withSeconds);
  }
  const minutes = minuteValues(minuteStep);
  const current = minutes.includes(pad2(parsed.minute)) ? pad2(parsed.minute) : minutes[0];
  const index = minutes.indexOf(current);
  const next = minutes[(index + delta + minutes.length) % minutes.length];
  return formatIsoTime({ ...parsed, minute: Number(next) }, withSeconds);
}

export function startOfCalendar(year: number, month: number) {
  const first = new Date(year, month, 1);
  const mondayIndex = (first.getDay() + 6) % 7;
  return new Date(year, month, 1 - mondayIndex);
}

export function calendarCells(year: number, month: number): CalendarCell[] {
  const start = startOfCalendar(year, month);
  const cells = Array.from({ length: 42 }, (_, index) => {
    const date = new Date(start.getFullYear(), start.getMonth(), start.getDate() + index);
    return {
      iso: toIsoDate(date),
      day: date.getDate(),
      inMonth: date.getMonth() === month,
    };
  });
  let weeks = 6;
  while (weeks > 4 && cells.slice((weeks - 1) * 7, weeks * 7).every((cell) => !cell.inMonth)) {
    weeks -= 1;
  }
  return cells.slice(0, weeks * 7);
}

export function viewMonthFrom(value: string, today: string) {
  const parsed = parseIsoDate(splitDateTime(value).date || today);
  const fallback = parseIsoDate(today) ?? parseIsoDate(toIsoDate(new Date()))!;
  const source = parsed ?? fallback;
  return { year: source.year, month: source.month };
}

/** Integer scroll so HH and MM selection bands share one horizon. */
export function wheelScrollTop(itemOffsetTop: number, itemHeight: number, listClientHeight: number) {
  return Math.round(itemOffsetTop - (listClientHeight - itemHeight) / 2);
}

/** Resolve the anchor instant for "Now" — full datetime when provided, else date-only or live clock. */
export function resolveNowAnchor(now?: string): Date {
  if (!now) return new Date();
  const { date, time } = splitDateTime(now);
  if (date && time) {
    const parsedDate = parseIsoDate(date);
    const parsedTime = parseTimePart(time);
    if (parsedDate && parsedTime) {
      return new Date(
        parsedDate.year,
        parsedDate.month,
        parsedDate.day,
        parsedTime.hour,
        parsedTime.minute,
        parsedTime.second,
      );
    }
  }
  const parsedDate = parseIsoDate(now);
  if (parsedDate) {
    const live = new Date();
    return new Date(parsedDate.year, parsedDate.month, parsedDate.day, live.getHours(), live.getMinutes(), live.getSeconds());
  }
  return new Date();
}

export function snapTimeToSteps(parsed: ParsedTime, options: TimeStepOptions = {}): string {
  const { minuteStep = 1, secondStep = 1, withSeconds = false } = options;
  let minute = parsed.minute;
  let second = parsed.second;
  if (minuteStep > 1) {
    minute = Math.round(minute / minuteStep) * minuteStep;
    if (minute >= 60) minute = 60 - minuteStep;
  }
  if (withSeconds && secondStep > 1) {
    second = Math.round(second / secondStep) * secondStep;
    if (second >= 60) second = 60 - secondStep;
  }
  return formatIsoTime({ ...parsed, minute, second }, withSeconds);
}

export function valueForNow(mode: TemporalMode, anchor: Date, options: TimeStepOptions = {}): string {
  const { withSeconds = false } = options;
  const date = toIsoDate(anchor);
  const time = snapTimeToSteps(
    { hour: anchor.getHours(), minute: anchor.getMinutes(), second: anchor.getSeconds() },
    options,
  );
  if (mode === "date") return date;
  if (mode === "time") return time;
  return joinDateTime(date, time, withSeconds);
}
