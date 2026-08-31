import { render } from "@testing-library/react";
import { WorkWellReleasedSeal } from "./WorkWellReleasedSeal";

describe("WorkWellReleasedSeal", () => {
  it("emits the released-result seal mark", () => {
    const { container } = render(<WorkWellReleasedSeal />);
    expect(container.querySelector("svg.work-well__seal")).toBeTruthy();
  });
});
