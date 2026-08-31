import { render, screen } from "@testing-library/react";
import { AssignmentStatusReadout } from "./AssignmentStatusReadout";

describe("AssignmentStatusReadout", () => {
  it("owns the assignment status dl grammar", () => {
    render(
      <AssignmentStatusReadout
        phase="Intake"
        record={<span>Released</span>}
      />,
    );

    const readout = screen.getByLabelText("Assignment status");
    expect(readout).toHaveClass("status-readout");
    expect(screen.getByText("Phase")).toBeTruthy();
    expect(screen.getByText("Intake")).toBeTruthy();
    expect(screen.getByText("Record")).toBeTruthy();
    expect(screen.getByText("Released")).toBeTruthy();
  });
});
