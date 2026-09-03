import { render, screen } from "@testing-library/react";
import { AgentStatusLine, StageBars } from "./SessionMarks";

describe("AgentStatusLine", () => {
  it("renders the examiner status as the clamped agent line", () => {
    render(<AgentStatusLine>Considering your reply…</AgentStatusLine>);
    const line = screen.getByText("Considering your reply…");
    expect(line).toHaveClass("agent-line");
    expect(line.closest(".tip-host")).not.toBeNull();
  });
});

describe("StageBars", () => {
  it("marks completed and current stages", () => {
    const { container } = render(<StageBars stage={3} total={5} />);
    const bars = container.querySelectorAll(".stage-bars span");
    expect(bars).toHaveLength(5);
    expect(bars[0]).toHaveClass("is-done");
    expect(bars[1]).toHaveClass("is-done");
    expect(bars[2]).toHaveClass("is-now");
    expect(bars[3]).not.toHaveClass("is-done");
    expect(bars[3]).not.toHaveClass("is-now");
  });

  it("marks every bar done when complete", () => {
    const { container } = render(<StageBars stage={3} total={4} complete />);
    const bars = container.querySelectorAll(".stage-bars span");
    expect([...bars].every((bar) => bar.classList.contains("is-done"))).toBe(true);
  });
});
