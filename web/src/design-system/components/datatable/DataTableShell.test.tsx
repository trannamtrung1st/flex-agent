import { render, screen } from "@testing-library/react";
import { DataTableShell } from "./DataTableShell";

describe("DataTableShell", () => {
  it("composes complete and body-only shells without extra pad variants", () => {
    const { rerender } = render(
      <DataTableShell
        table={
          <table>
            <caption>Queue</caption>
            <tbody>
              <tr>
                <td>Row</td>
              </tr>
            </tbody>
          </table>
        }
      />,
    );

    const complete = screen.getByRole("table", { name: "Queue" }).closest(".datatable");
    expect(complete).toHaveClass("datatable");
    expect(complete).not.toHaveClass("datatable--body-only");
    expect(complete).not.toHaveAttribute("data-pad");

    rerender(
      <DataTableShell
        variant="bodyOnly"
        className="queue-datatable"
        table={
          <table>
            <caption>Docket</caption>
            <tbody>
              <tr>
                <td>Row</td>
              </tr>
            </tbody>
          </table>
        }
      />,
    );

    const bodyOnly = screen.getByRole("table", { name: "Docket" }).closest(".datatable");
    expect(bodyOnly).toHaveClass("datatable", "datatable--body-only", "queue-datatable");
    expect(bodyOnly).not.toHaveAttribute("data-pad");
  });

  it("promotes a labelled scrollport to a named region", () => {
    render(
      <DataTableShell
        scrollProps={{ tabIndex: 0, "aria-label": "Campaign rows, scrollable" }}
        table={
          <table>
            <caption>Campaigns</caption>
            <tbody>
              <tr>
                <td>Row</td>
              </tr>
            </tbody>
          </table>
        }
      />,
    );

    expect(screen.getByRole("region", { name: "Campaign rows, scrollable" })).toHaveClass("datatable-scroll");
  });
});
