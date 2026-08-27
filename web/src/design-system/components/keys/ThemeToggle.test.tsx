import { render, screen } from "@testing-library/react";
import { ThemeToggle } from "./ThemeToggle";

describe("ThemeToggle", () => {
  it("keeps visible text and does not name the decorative icon", () => {
    render(<ThemeToggle theme="dark" onToggle={() => undefined} />);
    const toggle = screen.getByRole("button", { name: /switch to light theme/i });
    expect(toggle).toHaveTextContent(/theme/i);
    expect(toggle.querySelector("svg")).toHaveAttribute("aria-hidden", "true");
    expect(toggle.querySelector(".key-label")).toBeTruthy();
    expect(screen.queryByRole("img")).not.toBeInTheDocument();
  });

  it("names the light-to-dark switch when light is active", () => {
    render(<ThemeToggle theme="light" onToggle={() => undefined} />);
    expect(screen.getByRole("button", { name: /switch to dark theme/i })).toBeInTheDocument();
  });
});
