import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { CommandStrip } from "./CommandStrip";

describe("CommandStrip brand cell", () => {
  it("keeps a wordmark-only brand cell when there is no suffix", () => {
    const { container } = render(
      <MemoryRouter>
        <CommandStrip homeTo="/" homeLabel="Home" />
      </MemoryRouter>,
    );

    expect(container.querySelector(".strip-brand")).not.toHaveClass("strip-brand--origin");
    expect(container.querySelector(".strip-brand .strip-mode")).toBeNull();
    expect(screen.getByRole("link", { name: "Home" })).toBeInTheDocument();
  });

  it("shares one origin segment when brandSuffix is set", () => {
    const { container } = render(
      <MemoryRouter>
        <CommandStrip homeTo="/" homeLabel="Channel index" brandSuffix="Component Deck" />
      </MemoryRouter>,
    );

    const brand = container.querySelector(".strip-brand");
    expect(brand).toHaveClass("strip-brand--origin");
    expect(brand?.querySelector(".strip-mode")).toHaveTextContent("Component Deck");
  });
});
