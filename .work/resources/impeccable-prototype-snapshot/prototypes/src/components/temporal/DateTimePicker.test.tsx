import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { DatePicker, DateTimePicker, TimePicker } from "./DateTimePicker";

function Harness({
  mode,
  initial = "",
  withSeconds,
}: {
  mode: "date" | "time" | "datetime";
  initial?: string;
  withSeconds?: boolean;
}) {
  const [value, setValue] = useState(initial);
  const Picker = mode === "date" ? DatePicker : mode === "time" ? TimePicker : DateTimePicker;
  return (
    <div>
      <span id="whenLabel">When</span>
      <Picker id="when" labelId="whenLabel" value={value} onChange={setValue} now="2026-08-26" withSeconds={withSeconds} />
    </div>
  );
}

describe("DateTimePicker", () => {
  it("commits a calendar day and closes the date plate", async () => {
    const user = userEvent.setup();
    render(<Harness mode="date" initial="2026-08-26" />);
    await user.click(screen.getByRole("button", { name: /2026-08-26/ }));
    const dialog = screen.getByRole("dialog", { name: "Choose date" });
    await waitFor(() => expect(within(dialog).getByRole("button", { name: "2026-08-26" })).toHaveFocus());
    await user.click(within(dialog).getByRole("button", { name: "2026-08-18" }));
    expect(screen.getByRole("button", { name: /2026-08-18/ })).toHaveAttribute("aria-expanded", "false");
  });

  it("commits hour and minute from the chrono wheels", async () => {
    const user = userEvent.setup();
    render(<Harness mode="time" initial="09:00" />);
    await user.click(screen.getByRole("button", { name: /09:00/ }));
    const dialog = screen.getByRole("dialog", { name: "Choose time" });
    await user.click(within(within(dialog).getByRole("listbox", { name: "Hours" })).getByRole("option", { name: "14" }));
    await user.click(within(within(dialog).getByRole("listbox", { name: "Minutes" })).getByRole("option", { name: "30" }));
    expect(screen.getByRole("button", { name: /14:30/ })).toBeVisible();
    await user.click(within(dialog).getByRole("button", { name: "Done" }));
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("commits hour, minute, and second when seconds are enabled", async () => {
    const user = userEvent.setup();
    render(<Harness mode="time" initial="09:00:00" withSeconds />);
    await user.click(screen.getByRole("button", { name: /09:00:00/ }));
    const dialog = screen.getByRole("dialog", { name: "Choose time" });
    await user.click(within(within(dialog).getByRole("listbox", { name: "Hours" })).getByRole("option", { name: "14" }));
    await user.click(within(within(dialog).getByRole("listbox", { name: "Seconds" })).getByRole("option", { name: "45" }));
    expect(screen.getByRole("button", { name: /14:00:45/ })).toBeVisible();
  });

  it("preserves seconds when changing date on a datetime plate", async () => {
    const user = userEvent.setup();
    render(<Harness mode="datetime" initial="2026-08-26T14:30:45" withSeconds />);
    await user.click(screen.getByRole("button", { name: /2026-08-26 14:30:45/ }));
    const dialog = screen.getByRole("dialog", { name: "Choose date and time" });
    await user.click(within(dialog).getByRole("button", { name: "2026-08-18" }));
    expect(screen.getByRole("button", { name: /2026-08-18 14:30:45/ })).toBeVisible();
    expect(within(within(dialog).getByRole("listbox", { name: "Seconds" })).getByRole("option", { name: "45" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
  });

  it("strips stored seconds from display and wheels when seconds are disabled", async () => {
    const user = userEvent.setup();
    render(<Harness mode="datetime" initial="2026-08-26T14:30:45" />);
    await user.click(screen.getByRole("button", { name: /2026-08-26 14:30/ }));
    const dialog = screen.getByRole("dialog", { name: "Choose date and time" });
    expect(within(within(dialog).getByRole("listbox", { name: "Hours" })).getByRole("option", { name: "14" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    expect(within(within(dialog).getByRole("listbox", { name: "Minutes" })).getByRole("option", { name: "30" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    expect(screen.queryByRole("listbox", { name: "Seconds" })).toBeNull();
  });

  it("keeps date and time on one plate until Done", async () => {
    const user = userEvent.setup();
    render(<Harness mode="datetime" initial="2026-08-26T14:30" />);
    await user.click(screen.getByRole("button", { name: /2026-08-26 14:30/ }));
    const dialog = screen.getByRole("dialog", { name: "Choose date and time" });
    await user.click(within(dialog).getByRole("button", { name: "2026-08-18" }));
    expect(dialog).toBeVisible();
    const hour = within(within(dialog).getByRole("listbox", { name: "Hours" })).getByRole("option", { name: "08" });
    await user.click(hour);
    expect(screen.getByRole("button", { name: /2026-08-18 08:30/ })).toHaveAttribute("aria-expanded", "true");
    expect(hour).toHaveFocus();
  });

  it("etches a frozen value and withholds the plate", async () => {
    const user = userEvent.setup();
    render(
      <div>
        <span id="openedLabel">Opened on</span>
        <DatePicker id="opened" labelId="openedLabel" value="2026-07-01" onChange={() => undefined} frozen now="2026-08-26" />
      </div>,
    );
    const trigger = screen.getByRole("button", { name: /2026-07-01/ });
    expect(trigger).toBeDisabled();
    expect(trigger.closest(".select-shell")).toHaveClass("is-frozen");
    await user.click(trigger);
    expect(screen.queryByRole("dialog")).toBeNull();
  });
});
