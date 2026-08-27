import { render, screen } from "@testing-library/react";
import { OperateHead } from "./OperateHead";

describe("OperateHead", () => {
  it("keeps the default stack order for index pages", () => {
    render(<OperateHead title="Review queue" description="Ranked by receipt time." />);
    const head = screen.getByRole("heading", { name: "Review queue" }).closest(".operate-head");
    expect(head).not.toHaveClass("operate-head--plaque");
    expect(head).not.toHaveAttribute("data-head-arrange", "plaque");
  });

  it("arranges a ledger plaque as back, centered title plus status, and trailing session", () => {
    render(
      <OperateHead
        arrangement="plaque"
        title="Examination Transcript — The Overlay Ledger"
        description="Session 07 · FXA-7C19-2A07"
        back={<button type="button">Queue</button>}
        headExtra={<span>Sealed</span>}
      />,
    );
    const head = screen.getByRole("heading", { name: "Examination Transcript — The Overlay Ledger" }).closest(".operate-head");
    expect(head?.tagName).toBe("HEADER");
    expect(head).toHaveClass("operate-head--plaque");
    expect(head).toHaveAttribute("data-head-arrange", "plaque");
    expect(screen.getByRole("button", { name: "Queue" }).closest(".operate-head")).toBe(head);
    expect(screen.getByText("Sealed").closest(".operate-head-cluster")).toBeTruthy();
  });
});
