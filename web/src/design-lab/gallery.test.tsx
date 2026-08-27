import { render, screen } from "@testing-library/react";
import { DesignLabHome } from "./gallery";

describe("DesignLabHome", () => {
  it("identifies the isolated design-lab scaffold", () => {
    render(<DesignLabHome />);
    expect(screen.getByRole("heading", { name: "Design lab scaffold" })).toBeInTheDocument();
    expect(screen.getByText(/isolated from production/i)).toBeInTheDocument();
  });
});
