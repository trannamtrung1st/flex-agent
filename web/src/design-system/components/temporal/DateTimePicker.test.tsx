import { fireEvent, render, screen } from "@testing-library/react";
import { DatePicker, DateTimePicker, TimePicker } from "./DateTimePicker";

describe("DateTimePicker overlay plate", () => {
  it("keeps the date plate on its authored instrument class instead of stretching to the mark", () => {
    render(
      <DatePicker
        labelId="deadline-label"
        value=""
        onChange={() => undefined}
        now="2026-08-26"
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: /select date/i }));
    const plate = screen.getByRole("dialog", { name: "Choose date" });
    expect(plate).toHaveClass("datetime-popover--date", "floating-overlay");
    expect(plate).not.toHaveClass("datetime-popover--split");
    expect(plate.style.width).toBe("");
    expect(plate.style.minWidth).toBe("");
    expect(plate.style.maxHeight).toBe("");
  });

  it("gives a time plate its own clock grid area on the portaled node", () => {
    render(
      <TimePicker
        labelId="session-label"
        value="09:00"
        onChange={() => undefined}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: /09:00/ }));
    const plate = screen.getByRole("dialog", { name: "Choose time" });
    expect(plate).toHaveClass("datetime-popover--time", "floating-overlay");
    expect(plate.querySelector(".datetime-clock")).toBeTruthy();
  });

  it("gives a seconds time plate the seconds clock on the portaled node", () => {
    render(
      <TimePicker
        labelId="sync-label"
        value="09:00:00"
        onChange={() => undefined}
        withSeconds
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: /09:00:00/ }));
    const plate = screen.getByRole("dialog", { name: "Choose time" });
    expect(plate).toHaveClass("datetime-popover--time");
    expect(plate.querySelector(".datetime-clock--seconds")).toBeTruthy();
    expect(document.querySelector(".select-shell--time.has-seconds")).toBeTruthy();
  });

  it("keeps the datetime plate split on the portaled node", () => {
    render(
      <DateTimePicker
        labelId="activation-label"
        value="2026-08-26T14:30"
        onChange={() => undefined}
        now="2026-08-26"
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: /2026-08-26 14:30/ }));
    const plate = screen.getByRole("dialog", { name: "Choose date and time" });
    expect(plate).toHaveClass("datetime-popover--datetime", "datetime-popover--split", "floating-overlay");
    expect(plate.querySelector(".datetime-calendar")).toBeTruthy();
    expect(plate.querySelector(".datetime-clock")).toBeTruthy();
  });

  it("stays open while a time wheel scrolls and closes on external scroll", () => {
    render(
      <TimePicker
        labelId="session-label"
        value="09:00"
        onChange={() => undefined}
      />,
    );
    const trigger = screen.getByRole("button", { name: /09:00/ });
    fireEvent.click(trigger);
    const plate = screen.getByRole("dialog", { name: "Choose time" });
    const wheel = plate.querySelector(".time-wheel");
    expect(wheel).toBeTruthy();
    fireEvent.scroll(wheel!);
    expect(screen.getByRole("dialog", { name: "Choose time" })).toBeInTheDocument();
    fireEvent.scroll(window);
    expect(screen.queryByRole("dialog", { name: "Choose time" })).not.toBeInTheDocument();
    expect(document.activeElement).not.toBe(trigger);
  });

  it("restores trigger focus on Escape", () => {
    render(
      <TimePicker
        labelId="session-label"
        value="09:00"
        onChange={() => undefined}
      />,
    );
    const trigger = screen.getByRole("button", { name: /09:00/ });
    fireEvent.click(trigger);
    fireEvent.keyDown(screen.getByRole("dialog", { name: "Choose time" }), { key: "Escape" });
    expect(screen.queryByRole("dialog", { name: "Choose time" })).not.toBeInTheDocument();
    expect(document.activeElement).toBe(trigger);
  });
});
