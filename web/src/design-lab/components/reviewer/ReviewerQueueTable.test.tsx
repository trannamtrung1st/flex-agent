import { render, screen } from "@testing-library/react";
import { ReviewerQueueEmpty, ReviewerQueueTableShell } from "./ReviewerQueueTable";
import { ReviewerSealedReadout } from "./ReviewerSealedReadout";

describe("reviewer queue table and sealed readout", () => {
  it("owns queue-datatable on the shell", () => {
    render(
      <ReviewerQueueTableShell
        variant="bodyOnly"
        table={
          <table>
            <caption>Docket</caption>
            <tbody>
              <tr>
                <td>Row</td>
              </tr>
            </tbody>
          </table>
        }
      />,
    );

    const shell = screen.getByRole("table", { name: "Docket" }).closest(".datatable");
    expect(shell).toHaveClass("datatable", "datatable--body-only", "queue-datatable");
  });

  it("owns queue-empty-plate on the empty plate", () => {
    render(<ReviewerQueueEmpty id="queueEmpty" inset label="Queue clear" note="No sessions." />);

    expect(screen.getByText("Queue clear").closest(".empty-plate")).toHaveClass(
      "datatable-empty",
      "queue-empty-plate",
    );
  });

  it("owns sealed-mark on the record head readout", () => {
    render(<ReviewerSealedReadout label="Sealed" />);

    expect(screen.getByText("Sealed").closest(".state-cell")).toHaveClass("state-cell", "sealed-mark");
  });
});
