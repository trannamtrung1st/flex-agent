import { fireEvent, render, screen } from "@testing-library/react";
import { AssignmentSpine } from "./AssignmentSpine";

describe("AssignmentSpine", () => {
  it("lets the Participant inspect Attempt without unlocking start", () => {
    const onSelect = vi.fn();
    render(<AssignmentSpine view="submission" onSelect={onSelect} />);

    const nav = screen.getByRole("navigation", { name: "Assignment phases" });
    expect(nav).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Submission/ })).toHaveAttribute("aria-current", "step");
    const attempt = screen.getByRole("button", { name: /Attempt — not available from this application/ });
    expect(attempt).toBeEnabled();
    fireEvent.click(attempt);
    expect(onSelect).toHaveBeenCalledWith("attempt");
  });
});
