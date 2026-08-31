import { render, screen } from "@testing-library/react";
import { PhaseSpine } from "./PhaseSpine";

describe("PhaseSpine", () => {
  it("owns phase spine node grammar", () => {
    render(
      <PhaseSpine
        aria-label="Assignment phases"
        nodes={[
          {
            id: "submission",
            label: "Submission",
            short: "Prepare",
            state: "current",
            viewing: true,
            ariaLabel: "Submission — Prepare",
            onSelect: () => undefined,
          },
          {
            id: "attempt",
            label: "Attempt",
            short: "Locked",
            state: "locked",
            disabled: true,
            ariaLabel: "Attempt — not yet available",
            onSelect: () => undefined,
          },
        ]}
      />,
    );

    const nav = screen.getByRole("navigation", { name: "Assignment phases" });
    expect(nav).toHaveClass("phase-spine");
    expect(screen.getByRole("button", { name: "Submission — Prepare" })).toHaveClass("phase-node--current", "is-viewing");
    expect(screen.getByRole("button", { name: "Attempt — not yet available" })).toHaveClass("phase-node--locked");
  });
});
