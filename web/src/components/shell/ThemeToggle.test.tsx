import { fireEvent, render, screen } from "@testing-library/react";
import { ThemeToggle } from "./ThemeToggle";

describe("ThemeToggle", () => {
  afterEach(() => {
    window.localStorage.clear();
    document.documentElement.removeAttribute("data-theme");
  });

  it("keeps visible text and does not name the decorative icon", () => {
    render(<ThemeToggle />);
    const toggle = screen.getByRole("button", { name: /switch to (light|dark) theme/i });
    expect(toggle).toHaveTextContent(/theme/i);
    expect(toggle.querySelector("svg")).toHaveAttribute("aria-hidden", "true");
    expect(screen.queryByRole("img")).not.toBeInTheDocument();
  });

  it("writes the next theme onto the document", () => {
    render(<ThemeToggle />);
    fireEvent.click(screen.getByRole("button", { name: /switch to light theme/i }));
    expect(document.documentElement.dataset.theme).toBe("light");
    expect(window.localStorage.getItem("flex-agent-theme")).toBe("light");
  });
});
