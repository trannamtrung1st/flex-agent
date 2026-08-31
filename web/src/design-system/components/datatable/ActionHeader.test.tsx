import { render, screen } from "@testing-library/react";
import { ActionHeader } from "./ActionHeader";

describe("ActionHeader", () => {
  it("renders a visually hidden actions column head with the action floor", () => {
    render(
      <table>
        <thead>
          <tr>
            <ActionHeader />
          </tr>
        </thead>
      </table>,
    );

    const head = screen.getByText("Actions").closest("th");
    expect(head).toHaveClass("col-action");
    expect(head).toHaveAttribute("data-col-min", "action");
    expect(screen.getByText("Actions")).toHaveClass("visually-hidden");
  });
});
