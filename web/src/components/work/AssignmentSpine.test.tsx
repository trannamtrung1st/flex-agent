import { fireEvent, render, screen } from "@testing-library/react";
import { AssignmentSpine } from "./AssignmentSpine";

describe("AssignmentSpine", () => {
  it("lets the Participant inspect Attempt readiness", () => {
    const onSelect = vi.fn();
    render(<AssignmentSpine view="submission" onSelect={onSelect} />);

    const nav = screen.getByRole("navigation", { name: "Assignment phases" });
    expect(nav).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Submission/ })).toHaveAttribute("aria-current", "step");
    const attempt = screen.getByRole("button", { name: /Attempt — Readiness and start/ });
    expect(attempt).toBeEnabled();
    fireEvent.click(attempt);
    expect(onSelect).toHaveBeenCalledWith("attempt");
  });

  it("marks only the viewed node as current", () => {
    render(<AssignmentSpine view="attempt" onSelect={() => undefined} />);
    const submission = screen.getByRole("button", { name: /Submission — Prepare and accept a version/ });
    const attempt = screen.getByRole("button", { name: /Attempt — Readiness and start/ });
    expect(submission).not.toHaveAttribute("aria-current");
    expect(submission).not.toHaveClass("phase-node--current");
    expect(attempt).toHaveAttribute("aria-current", "step");
    expect(attempt).not.toHaveClass("phase-node--locked");
    expect(attempt).toHaveClass("is-viewing");
  });
});
