import { render, screen } from "@testing-library/react";
import { ReviewerLedgerOperateArea } from "./ReviewerLedgerOperateArea";
import { ReviewerQueueOperateArea } from "./ReviewerQueueOperateArea";

describe("reviewer OperateArea host wrappers", () => {
  it("owns the queue host without work-plane", () => {
    render(
      <ReviewerQueueOperateArea label="Review queue" title="Review queue" hug="registry">
        <p>Rows</p>
      </ReviewerQueueOperateArea>,
    );

    const region = screen.getByRole("region", { name: "Review queue" });
    expect(region).toHaveClass("workspace-area", "queue-view", "registry-wall--hug");
    expect(region).not.toHaveClass("work-plane");
  });

  it("owns the ledger host and plaque head", () => {
    render(
      <ReviewerLedgerOperateArea
        label="Evaluation record"
        title="Evaluation record"
        className="is-released"
      >
        <p>Ledger</p>
      </ReviewerLedgerOperateArea>,
    );

    const region = screen.getByRole("region", { name: "Evaluation record" });
    expect(region).toHaveClass("workspace-area", "record-view", "is-released");
    expect(region).not.toHaveClass("work-plane");
    expect(region.querySelector(".operate-head")).toHaveClass("operate-head--plaque");
    expect(region).toHaveAttribute("data-flow-gap", "none");
  });
});
