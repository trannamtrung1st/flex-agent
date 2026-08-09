import { fireEvent, render, screen } from "@testing-library/react";
import { App } from "./App";

describe("App smoke surface", () => {
  it("renders the development smoke heading and status region", () => {
    render(<App />);

    expect(
      screen.getByRole("heading", { name: /flex agent workspace scaffold/i }),
    ).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /runtime status/i })).toBeInTheDocument();
    expect(
      screen.getByText(/not a product capability/i),
    ).toBeInTheDocument();
  });

  it("toggles the theme when the switch button is activated", () => {
    render(<App />);

    const button = screen.getByRole("button", { name: /switch to light theme/i });
    fireEvent.click(button);

    expect(document.documentElement.dataset.theme).toBe("light");
    expect(
      screen.getByRole("button", { name: /switch to dark theme/i }),
    ).toBeInTheDocument();
  });
});
