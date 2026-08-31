import { render, screen } from "@testing-library/react";
import { AssignmentHead } from "./AssignmentHead";

describe("AssignmentHead", () => {
  it("owns the assignment heading grammar for title-only denied chrome", () => {
    render(<AssignmentHead title="Access denied" />);

    const heading = screen.getByRole("heading", { name: "Access denied" });
    const head = heading.closest(".assignment-head");
    expect(head).toHaveClass("assignment-head");
    expect(head?.querySelector(".assignment-ident")).toContainElement(heading);
    expect(heading).toHaveClass("assignment-title");
    expect(head?.querySelector(".assignment-meta")).toBeNull();
    expect(head?.querySelector(".status-readout")).toBeNull();
  });

  it("places optional meta and status beside the ident", () => {
    render(
      <AssignmentHead
        title="Shoreline Operations"
        meta="Activity · Text examination"
        status={(
          <dl className="status-readout" aria-label="Assignment status">
            <div className="status-item">
              <dt>Phase</dt>
              <dd>Intake</dd>
            </div>
          </dl>
        )}
      />,
    );

    const head = screen.getByRole("heading", { name: "Shoreline Operations" }).closest(".assignment-head");
    expect(head?.querySelector(".assignment-meta")).toHaveTextContent("Activity · Text examination");
    expect(screen.getByLabelText("Assignment status")).toHaveClass("status-readout");
  });
});
