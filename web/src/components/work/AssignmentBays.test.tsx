import { render, screen } from "@testing-library/react";
import { AssignmentBay, AssignmentBays } from "./AssignmentBays";

describe("AssignmentBays", () => {
  it("owns assignment bay hull classes without the dense modifier", () => {
    render(
      <AssignmentBays>
        <AssignmentBay headingId="current-assignments" label="Current assignments">
          <p>Plates</p>
        </AssignmentBay>
      </AssignmentBays>,
    );

    const host = document.querySelector(".assignment-bays");
    expect(host).toHaveClass("assignment-bays");
    expect(host).not.toHaveClass("assignment-bays--dense");
    const bay = screen.getByRole("region", { name: "Current assignments" });
    expect(bay).toHaveClass("assignment-bay");
    expect(screen.getByRole("heading", { name: "Current assignments" })).toHaveClass("assignment-bay-head");
    expect(bay).toHaveTextContent("Plates");
  });
});
