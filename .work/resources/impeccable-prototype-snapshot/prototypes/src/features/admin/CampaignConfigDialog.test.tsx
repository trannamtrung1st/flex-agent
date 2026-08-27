import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { Campaign } from "../../data/types";
import { CampaignConfigDialog } from "./CampaignConfigDialog";

const campaign: Campaign = {
  id: "CMP-TEST",
  name: "Test Campaign",
  frozen: false,
  config: {
    harness: "GOVERNED-EXAM-01",
    agent: "EXAMINER-CORE",
    sessionLimit: "60:00",
    timeWarning: "10:00",
    maxAttempts: "1",
    cooldown: "24H",
  },
  rows: [],
  updatedAt: new Date("2026-08-26T00:00:00Z"),
};

describe("CampaignConfigDialog", () => {
  it("connects ceremony validation messages to every invalid field", async () => {
    const user = userEvent.setup();
    render(
      <CampaignConfigDialog
        open
        onClose={() => undefined}
        campaign={campaign}
        onActivate={vi.fn()}
      />,
    );

    await user.clear(screen.getByRole("textbox", { name: "Time warning at" }));
    await user.type(screen.getByRole("textbox", { name: "Time warning at" }), "99:00");
    await user.clear(screen.getByRole("textbox", { name: "Max attempts" }));
    await user.type(screen.getByRole("textbox", { name: "Max attempts" }), "0");
    await user.click(screen.getByRole("button", { name: "Activate" }));

    const warning = screen.getByRole("textbox", { name: "Time warning at" });
    expect(warning).toHaveAttribute("aria-invalid", "true");
    expect(warning).toHaveAttribute("aria-describedby", "timeWarningHint timeWarningError");
    expect(screen.getByText(/Time warning must land before/)).toHaveAttribute("id", "timeWarningError");

    const attempts = screen.getByRole("textbox", { name: "Max attempts" });
    expect(attempts).toHaveAttribute("aria-invalid", "true");
    expect(attempts).toHaveAttribute("aria-describedby", "maxAttemptsError");
    expect(screen.getByText(/Max attempts must be a whole number/)).toHaveAttribute("id", "maxAttemptsError");
  });

  it("shows session-limit recovery under the field as the value is typed", async () => {
    const user = userEvent.setup();
    render(
      <CampaignConfigDialog
        open
        onClose={() => undefined}
        campaign={campaign}
        onActivate={vi.fn()}
      />,
    );

    const limit = screen.getByRole("textbox", { name: "Session limit" });
    await user.clear(limit);
    await user.type(limit, "99");

    expect(limit).toHaveAttribute("aria-invalid", "true");
    expect(limit).toHaveAttribute("aria-describedby", "sessionLimitHint sessionLimitError");
    expect(screen.getByText(/enter a value like 60:00/i)).toHaveAttribute("id", "sessionLimitError");
    expect(document.getElementById("sessionLimitHint")).toHaveTextContent("MM:SS · e.g. 60:00");
  });
});
