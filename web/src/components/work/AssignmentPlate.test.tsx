import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { Key } from "../../design-system";
import { AssignmentPlate } from "./AssignmentPlate";

describe("AssignmentPlate", () => {
  it("seats the next-action key on a trailing plate foot", () => {
    render(
      <MemoryRouter>
        <AssignmentPlate
          label="Campaign A"
          rows={[{ term: "Campaign", value: "Campaign A" }]}
          action={
            <Key variant="open" to="/my-work/enr-1" ariaLabel="Open Campaign A">
              Open
            </Key>
          }
        />
      </MemoryRouter>,
    );

    const open = screen.getByRole("link", { name: "Open Campaign A" });
    const foot = open.closest("footer");
    expect(foot).toHaveClass("plate-foot", "assignment-plate-keys");
    expect(foot).toHaveAttribute("data-arrangement", "end");
    expect(foot).toHaveAttribute("data-flow-justify", "end");
    expect(foot).not.toHaveClass("assignment-plate-keys--empty");
  });

  it("reserves an empty trailing foot when no action is authorized", () => {
    const { container } = render(
      <AssignmentPlate label="Activities" rows={[{ term: "Purpose", value: "Create drafts" }]} />,
    );

    const foot = container.querySelector("footer.assignment-plate-keys");
    expect(foot).toHaveClass("assignment-plate-keys--empty");
    expect(foot).toHaveAttribute("data-arrangement", "end");
  });
});
