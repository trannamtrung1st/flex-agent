import { render, screen } from "@testing-library/react";
import { ProtocolPlate } from "./ProtocolPlate";

describe("ProtocolPlate", () => {
  it("renders protocol label and value on a dim pane", () => {
    render(<ProtocolPlate label="Protocol" value="V7.3.1" />);
    const plate = screen.getByText("Protocol").closest(".protocol-plate");
    expect(plate).toHaveClass("pane", "pane--dim", "pane--br");
    expect(screen.getByText("V7.3.1")).toHaveClass("protocol-value");
  });
});
