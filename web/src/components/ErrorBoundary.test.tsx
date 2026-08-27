import { render, screen } from "@testing-library/react";
import { ErrorBoundary } from "./ErrorBoundary";

function Boom(): never {
  throw new Error("render failed");
}

describe("ErrorBoundary", () => {
  it("does not disclose rebuild or cutover language", () => {
    const error = vi.spyOn(console, "error").mockImplementation(() => undefined);

    render(
      <ErrorBoundary>
        <Boom />
      </ErrorBoundary>,
    );

    expect(screen.getByRole("heading", { name: "Something went wrong" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reload" })).toBeInTheDocument();
    expect(screen.queryByText(/cutover/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/SPA/i)).not.toBeInTheDocument();
    error.mockRestore();
  });
});
