import { render, screen } from "@testing-library/react";
import { SortableHeader } from "./SortableHeader";

describe("SortableHeader", () => {
  it("stamps named column min-width onto the header cell", () => {
    render(
      <table>
        <thead>
          <tr>
            <SortableHeader
              sortKey="updated"
              label="Updated"
              sorts={[{ key: "updated", dir: "asc" }]}
              onSort={() => {}}
              colMin="instant"
            />
          </tr>
        </thead>
      </table>,
    );

    expect(screen.getByRole("columnheader", { name: "Updated" })).toHaveAttribute("data-col-min", "instant");
  });
});
