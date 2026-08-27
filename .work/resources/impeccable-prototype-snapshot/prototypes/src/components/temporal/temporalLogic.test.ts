import { describe, expect, it } from "vitest";
import {
  addMonths,
  calendarCells,
  displayDateTime,
  displayTime,
  joinDateTime,
  minuteValues,
  normalizeIsoTime,
  parseIsoDate,
  parseIsoTime,
  shiftIsoDate,
  shiftTime,
  splitDateTime,
  startOfCalendar,
  viewMonthFrom,
  wheelScrollTop,
} from "./temporalLogic";

describe("parseIsoDate", () => {
  it("accepts real calendar days and rejects overflow", () => {
    expect(parseIsoDate("2026-08-26")).toEqual({ year: 2026, month: 7, day: 26 });
    expect(parseIsoDate("2026-02-31")).toBeNull();
    expect(parseIsoDate("26-08-2026")).toBeNull();
  });
});

describe("parseIsoTime", () => {
  it("accepts 24h marks and rejects overflow", () => {
    expect(parseIsoTime("09:00")).toEqual({ hour: 9, minute: 0, second: 0 });
    expect(parseIsoTime("24:00")).toBeNull();
    expect(parseIsoTime("9:00")).toBeNull();
  });

  it("accepts second marks when configured", () => {
    expect(parseIsoTime("09:00:30", true)).toEqual({ hour: 9, minute: 0, second: 30 });
    expect(parseIsoTime("09:00", true)).toBeNull();
    expect(parseIsoTime("09:00:30", false)).toBeNull();
    expect(parseIsoTime("09:00:60", true)).toBeNull();
  });
});

describe("splitDateTime", () => {
  it("reads ISO and spaced forms", () => {
    expect(splitDateTime("2026-08-26T14:30")).toEqual({ date: "2026-08-26", time: "14:30" });
    expect(splitDateTime("2026-08-26 14:30")).toEqual({ date: "2026-08-26", time: "14:30" });
    expect(splitDateTime("2026-08-26T14:30:45")).toEqual({ date: "2026-08-26", time: "14:30:45" });
    expect(joinDateTime("2026-08-26", "14:30")).toBe("2026-08-26T14:30");
    expect(joinDateTime("2026-08-26", "14:30:45", true)).toBe("2026-08-26T14:30:45");
    expect(displayDateTime("2026-08-26T14:30")).toBe("2026-08-26 14:30");
    expect(displayDateTime("2026-08-26T14:30:45", true)).toBe("2026-08-26 14:30:45");
    expect(displayDateTime("2026-08-26T14:30:45", false)).toBe("2026-08-26 14:30");
    expect(displayDateTime("2026-08-26T14:30", true)).toBe("2026-08-26 14:30:00");
  });
});

describe("normalizeIsoTime", () => {
  it("honors the withSeconds flag for display and wheel state", () => {
    expect(normalizeIsoTime("14:30:45", false)).toBe("14:30");
    expect(normalizeIsoTime("14:30:45", true)).toBe("14:30:45");
    expect(normalizeIsoTime("09:00", true)).toBe("09:00:00");
    expect(displayTime("09:00:30", false)).toBe("09:00");
    expect(displayTime("09:00", true)).toBe("09:00:00");
  });
});

describe("calendarCells", () => {
  it("starts on Monday and drops a trailing week that is entirely out of month", () => {
    const start = startOfCalendar(2026, 8);
    expect(start.getDay()).toBe(1);
    const cells = calendarCells(2026, 8);
    expect(cells).toHaveLength(35);
    expect(cells[0]?.iso).toBe("2026-08-31");
    expect(cells.find((cell) => cell.iso === "2026-09-01")?.inMonth).toBe(true);
    expect(cells[0]?.inMonth).toBe(false);
    expect(cells.at(-1)?.iso).toBe("2026-10-04");
  });

  it("keeps six weeks when the month still occupies the last row", () => {
    expect(calendarCells(2026, 7)).toHaveLength(42);
    expect(calendarCells(2017, 0)).toHaveLength(42);
  });
});

describe("shift helpers", () => {
  it("moves days and wraps hours", () => {
    expect(shiftIsoDate("2026-08-26", 1)).toBe("2026-08-27");
    expect(shiftIsoDate("2026-08-31", 1)).toBe("2026-09-01");
    expect(shiftTime("23:59", "hour", 1)).toBe("00:59");
    expect(shiftTime("14:00", "minute", -1)).toBe("14:59");
    expect(shiftTime("14:30:58", "second", 1, { withSeconds: true })).toBe("14:30:59");
    expect(shiftTime("14:30:00", "second", -1, { withSeconds: true, secondStep: 15 })).toBe("14:30:45");
    expect(minuteValues(15)).toEqual(["00", "15", "30", "45"]);
    expect(addMonths(2026, 11, 1)).toEqual({ year: 2027, month: 0 });
    expect(viewMonthFrom("2026-09-18", "2026-08-26")).toEqual({ year: 2026, month: 8 });
  });
});

describe("wheelScrollTop", () => {
  it("places different indices on the same visual horizon", () => {
    const height = 184;
    const row = 30;
    const pad = (height - row) / 2;
    const hour = wheelScrollTop(pad + 14 * row, row, height);
    const minute = wheelScrollTop(pad + 30 * row, row, height);
    expect(pad + 14 * row - hour).toBe(pad + 30 * row - minute);
  });
});
