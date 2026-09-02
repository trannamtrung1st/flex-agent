import { render, screen } from "@testing-library/react";
import { EMPTY_SELECTION } from "../../patterns/tableSelection";
import { SelectHeader } from "./SelectHeader";

describe("SelectHeader", () => {
  it("owns the select column header host class", () => {
    render(
      <table>
        <thead>
          <tr>
            <SelectHeader
              id="select-all"
              selection={EMPTY_SELECTION}
              pageIds={[]}
              capability={{ mode: "page" }}
              noun="rows"
              onTransition={() => undefined}
            />
          </tr>
        </thead>
      </table>,
    );

    const header = screen.getByRole("columnheader");
    expect(header).toHaveClass("col-select");
    expect(header.querySelector(".select-head")).toBeTruthy();
  });
});
