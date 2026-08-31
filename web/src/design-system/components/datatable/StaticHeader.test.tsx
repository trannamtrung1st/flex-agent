import { render, screen } from "@testing-library/react";
import { StaticHeader } from "./StaticHeader";

describe("StaticHeader", () => {
  it("stamps named column min-width and seats the static col-head pad", () => {
    render(
      <table>
        <thead>
          <tr>
            <StaticHeader label="Participant" colMin="id" />
          </tr>
        </thead>
      </table>,
    );

    const head = screen.getByRole("columnheader", { name: "Participant" });
    expect(head).toHaveAttribute("data-col-min", "id");
    expect(head.querySelector(".col-head")).toHaveTextContent("Participant");
    expect(head.querySelector("button.col-key")).toBeNull();
  });

  it("emits col-state for session state columns", () => {
    render(
      <table>
        <thead>
          <tr>
            <StaticHeader label="Session state" colMin="state" />
          </tr>
        </thead>
      </table>,
    );

    expect(screen.getByRole("columnheader", { name: "Session state" })).toHaveClass("col-state");
  });
});
