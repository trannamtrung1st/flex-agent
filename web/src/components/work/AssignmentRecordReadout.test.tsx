import { render, screen } from "@testing-library/react";
import { AssignmentRecordReadout } from "./AssignmentRecordReadout";

describe("AssignmentRecordReadout", () => {
  it("adds the assignment mark on the existing state-cell root", () => {
    render(<AssignmentRecordReadout variant="sealed" solid label="Released" />);

    const cell = screen.getByText("Released").closest(".state-cell");
    expect(cell).toHaveClass("state-cell", "assignment-record");
    expect(screen.getByText("Released")).toHaveClass("assignment-record-label");
  });

  it("keeps additive className and labelClassName with the assignment mark", () => {
    render(
      <AssignmentRecordReadout
        label="Released"
        className="is-hot"
        labelClassName="custom-label"
      />,
    );

    expect(screen.getByText("Released").closest(".state-cell")).toHaveClass("assignment-record", "is-hot");
    expect(screen.getByText("Released")).toHaveClass("assignment-record-label", "custom-label");
  });
});
