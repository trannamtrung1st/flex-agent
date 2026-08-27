import { render, screen } from "@testing-library/react";
import { App } from "./App";

describe("App", () => {
  it("identifies the candidate workspace without implying production cutover", () => {
    render(<App />);
    expect(
      screen.getByRole("heading", { name: "Flex Agent candidate workspace" }),
    ).toBeInTheDocument();
    expect(screen.getByText(/production traffic continues/i)).toBeInTheDocument();
  });
});
