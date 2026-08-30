import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { StripBrand } from "./Brand";

describe("StripBrand origin cell", () => {
  it("does not use the origin cell when there is no mode suffix", () => {
    const { container } = render(
      <MemoryRouter>
        <StripBrand homeTo="/" homeLabel="Home" />
      </MemoryRouter>,
    );

    expect(container.querySelector(".strip-brand")).not.toHaveClass("strip-brand--origin");
    expect(screen.getByRole("link", { name: "Home" })).toBeInTheDocument();
  });

  it("uses the origin cell when a mode suffix shares the left segment", () => {
    const { container } = render(
      <MemoryRouter>
        <StripBrand homeTo="/" homeLabel="Channel index" suffix="Component Deck" />
      </MemoryRouter>,
    );

    const brand = container.querySelector(".strip-brand");
    expect(brand).toHaveClass("strip-brand--origin");
    expect(brand?.querySelector(".strip-mode")).toHaveTextContent("Component Deck");
  });
});
