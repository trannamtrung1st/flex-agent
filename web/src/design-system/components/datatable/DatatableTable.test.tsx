import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { DatatableActions, DatatableCell, DatatableId, DatatableRow, DatatableStateReadout, DatatableTable } from "./DatatableTable";

describe("DatatableTable", () => {
  it("owns the table host class, hidden state, and caption", () => {
    const { rerender } = render(
      <DatatableTable caption="Activities" hidden>
        <tbody>
          <DatatableRow>
            <DatatableCell kind="id" colMin="id">Empty</DatatableCell>
          </DatatableRow>
        </tbody>
      </DatatableTable>,
    );

    const table = screen.getByRole("table", { hidden: true });
    expect(table).toHaveClass("datatable-table");
    expect(table).toHaveAttribute("hidden");
    expect(table.querySelector("caption")).toHaveClass("visually-hidden");
    expect(table.querySelector("caption")).toHaveTextContent("Activities");

    rerender(
      <DatatableTable caption="Activities" className="manifest">
        <tbody>
          <DatatableRow>
            <DatatableCell kind="id" colMin="id">Row</DatatableCell>
          </DatatableRow>
        </tbody>
      </DatatableTable>,
    );
    expect(screen.getByRole("table")).toHaveClass("datatable-table", "manifest");
    expect(screen.getByRole("table")).not.toHaveAttribute("hidden");
  });
});

describe("DatatableRow", () => {
  it("marks selected assign-picker rows", () => {
    render(
      <table>
        <tbody>
          <DatatableRow selected>
            <DatatableCell kind="select">Mark</DatatableCell>
          </DatatableRow>
        </tbody>
      </table>,
    );

    expect(screen.getByRole("row")).toHaveClass("datatable-row", "is-selected");
  });

  it("marks expanded detail rows", () => {
    render(
      <table>
        <tbody>
          <DatatableRow expanded>
            <DatatableCell kind="id" colMin="id">Row</DatatableCell>
          </DatatableRow>
        </tbody>
      </table>,
    );

    expect(screen.getByRole("row")).toHaveClass("datatable-row", "is-expanded");
  });
});

describe("DatatableCell", () => {
  it("emits kind classes and named column floors", () => {
    render(
      <table>
        <tbody>
          <tr>
            <DatatableCell kind="id" colMin="id">Id</DatatableCell>
            <DatatableCell kind="content" colMin="instant">When</DatatableCell>
            <DatatableCell kind="state" colMin="state">State</DatatableCell>
            <DatatableCell kind="select">Select</DatatableCell>
            <DatatableCell kind="content" colMin="result">Released</DatatableCell>
            <DatatableCell kind="action">Go</DatatableCell>
          </tr>
        </tbody>
      </table>,
    );

    const cells = screen.getAllByRole("cell");
    expect(cells[0]).toHaveClass("cell-id");
    expect(cells[0]).toHaveAttribute("data-col-min", "id");
    expect(cells[1]).toHaveClass("cell-content");
    expect(cells[2]).toHaveClass("cell-state");
    expect(cells[3]).toHaveClass("cell-select");
    expect(cells[4]).toHaveClass("cell-content", "cell-result");
    expect(cells[4]).toHaveAttribute("data-col-min", "result");
    expect(cells[5]).toHaveClass("col-action");
  });
});

describe("DatatableId", () => {
  it("renders a registry Link or a button", () => {
    const { rerender } = render(
      <MemoryRouter>
        <DatatableId to="/activities/a1/setup">Campaign</DatatableId>
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: "Campaign" })).toHaveClass("datatable-id");
    expect(screen.getByRole("link", { name: "Campaign" })).toHaveAttribute("href", "/activities/a1/setup");

    rerender(<DatatableId onClick={() => undefined}>Participant</DatatableId>);
    expect(screen.getByRole("button", { name: "Participant" })).toHaveClass("datatable-id");
  });
});

describe("DatatableActions", () => {
  it("owns the Create/Assign action strip without TableActionBar", () => {
    render(
      <DatatableActions>
        <button type="button">Create</button>
      </DatatableActions>,
    );

    const host = document.querySelector(".datatable-actions");
    expect(host).toHaveClass("datatable-actions");
    expect(host).toHaveAttribute("aria-label", "Table actions");
    expect(host?.querySelector(".datatable-actions-keys")).toHaveAttribute("data-flow-justify", "end");
    expect(screen.getByRole("button", { name: "Create" })).toBeTruthy();
  });
});

describe("DatatableStateReadout", () => {
  it("defaults table label styling on the existing state-cell root", () => {
    render(<DatatableStateReadout variant="sealed" solid label="Activated" />);

    expect(screen.getByText("Activated").closest(".state-cell")).toHaveClass("state-cell");
    expect(screen.getByText("Activated")).toHaveClass("state-label");
  });
});
