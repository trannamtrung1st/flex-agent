import { render } from "@testing-library/react";
import { RecordSeal, StageBars } from "./SessionMarks";

describe("RecordSeal", () => {
  it("emits the record seal mark", () => {
    const { container } = render(<RecordSeal />);
    expect(container.querySelector("svg.record-seal")).toBeTruthy();
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
