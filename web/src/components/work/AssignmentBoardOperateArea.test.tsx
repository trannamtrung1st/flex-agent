import { render, screen } from "@testing-library/react";
import { AssignmentBoardOperateArea } from "./AssignmentBoardOperateArea";

describe("AssignmentBoardOperateArea", () => {
  it("owns assignment-board hug on the workspace work-plane", () => {
    render(
      <AssignmentBoardOperateArea hug="board" label="My work" title="My work">
        <p>Empty</p>
      </AssignmentBoardOperateArea>,
    );

    const region = screen.getByRole("region", { name: "My work" });
    expect(region).toHaveClass("workspace-area", "work-plane", "assignment-board--hug");
  });

  it("omits board hug when the roster is populated", () => {
    render(
      <AssignmentBoardOperateArea framed={false} label="My work" title="My work">
        <p>Plates</p>
      </AssignmentBoardOperateArea>,
    );

    expect(screen.getByRole("region", { name: "My work" })).not.toHaveClass("assignment-board--hug");
  });
});
