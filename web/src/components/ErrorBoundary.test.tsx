import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { ErrorBoundary } from "./ErrorBoundary";

function Boom(): never {
  throw new Error("render failed");
}

describe("ErrorBoundary", () => {
  it("does not disclose rebuild or cutover language", () => {
    const error = vi.spyOn(console, "error").mockImplementation(() => undefined);

    render(
      <MemoryRouter>
        <ErrorBoundary>
          <Boom />
        </ErrorBoundary>
      </MemoryRouter>,
    );

    expect(screen.getByRole("heading", { name: "Something went wrong" })).toBeInTheDocument();
    expect(screen.getAllByRole("main")).toHaveLength(1);
    expect(screen.getByRole("region", { name: "Something went wrong" })).toHaveClass("work-plane--ceremony");
    expect(document.querySelector(".operate-column--hug")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Reload" })).toBeInTheDocument();
    expect(document.querySelector(".strip-brand")).not.toHaveClass("strip-brand--origin");
    expect(screen.getByRole("button", { name: "Switch to light theme" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /operator menu/i })).not.toBeInTheDocument();
    expect(screen.queryByText(/cutover/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/SPA/i)).not.toBeInTheDocument();
    error.mockRestore();
  });
});
