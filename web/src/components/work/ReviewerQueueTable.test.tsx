import { render, screen } from "@testing-library/react";
import { ReviewerQueueEmpty, ReviewerQueueTableShell } from "./ReviewerQueueTable";
import { ReviewerQueueOperateArea } from "./ReviewerQueueOperateArea";
import { ReviewerSealedReadout } from "./ReviewerSealedReadout";

describe("production Review queue wrappers", () => {
  it("owns queue-datatable on the shell", () => {
    const { container } = render(
      <ReviewerQueueTableShell table={<table><tbody><tr><td>Row</td></tr></tbody></table>} />,
    );
    expect(container.firstChild).toHaveClass("datatable", "queue-datatable");
  });

  it("owns queue-empty-plate on the empty plate", () => {
    render(
      <ReviewerQueueEmpty
        id="queueEmpty"
        inset
        label="No Review work is assigned to you"
        note="Assigned Review cases appear here when they are assigned to you."
      />,
    );
    expect(screen.getByText("No Review work is assigned to you").closest(".datatable-empty")).toHaveClass("queue-empty-plate");
  });

  it("seats the registry operate bay", () => {
    render(
      <ReviewerQueueOperateArea label="Review work" title="Review work">
        <p>Rows</p>
      </ReviewerQueueOperateArea>,
    );
    expect(screen.getByRole("region", { name: "Review work" })).toHaveClass("workspace-area", "registry-wall");
  });

  it("defaults the sealed Evaluation mark", () => {
    render(<ReviewerSealedReadout label="Internal Evaluation · Not a released Result" />);
    expect(screen.getByText("Internal Evaluation · Not a released Result").closest(".state-cell")).toHaveClass("sealed-mark");
  });
});
