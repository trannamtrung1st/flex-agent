import { render, screen } from "@testing-library/react";
import { FrozenLine } from "./FrozenLine";

describe("FrozenLine", () => {
  it("owns the frozen standing-condition class", () => {
    render(<FrozenLine>Configuration frozen at activation</FrozenLine>);
    expect(screen.getByText("Configuration frozen at activation")).toHaveClass("frozen-line");
  });
});
