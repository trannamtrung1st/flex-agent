import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { ContractUnavailablePage } from "./ContractUnavailablePage";

describe("ContractUnavailablePage", () => {
  it("states the missing contract without exposing Start or Session commands", () => {
    render(
      <MemoryRouter>
        <ContractUnavailablePage
          title="Text Session"
          note="Session command and snapshot HTTP are not exposed to this SPA. The host maps SSE events only. The Session remains on the server."
        />
      </MemoryRouter>,
    );

    expect(screen.getByRole("heading", { name: "Text Session" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Text Session" })).toHaveClass("work-plane--ceremony");
    expect(document.querySelector(".operate-column--hug")).toBeTruthy();
    expect(screen.getByText(/not exposed to this SPA/i)).toBeInTheDocument();
    expect(screen.queryByText("Unavailable")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /start session|begin attempt|open transcript/i })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Return to Home" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: "Return to Home" })).toHaveClass("key--quiet");
  });
});
