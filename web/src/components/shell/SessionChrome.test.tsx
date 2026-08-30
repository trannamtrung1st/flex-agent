import type { ComponentProps } from "react";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { SigningOutScreen } from "./SessionChrome";

function renderSigningOutScreen(props: ComponentProps<typeof SigningOutScreen>) {
  return render(
    <MemoryRouter>
      <SigningOutScreen {...props} />
    </MemoryRouter>,
  );
}

describe("SigningOutScreen", () => {
  it("seats signing out as a ceremony wait plate", () => {
    renderSigningOutScreen({ onRetry: () => {} });

    const status = screen.getByRole("status");
    expect(status).toHaveClass("wait-plate", "wait-plate--inset", "ceremony-wait");
    expect(screen.getByText("Signing out…")).toBeVisible();
    expect(status.querySelector(".scan-track.is-waiting")).toBeTruthy();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("keeps logout failures on the ceremony empty well with retry", () => {
    renderSigningOutScreen({
      errorMessage: "Sign out status could not be confirmed. Try again.",
      onRetry: () => {},
    });

    expect(screen.getByRole("alert")).toHaveTextContent("Sign out status could not be confirmed. Try again.");
    expect(screen.getByRole("button", { name: "Try again" })).toBeInTheDocument();
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });
});
