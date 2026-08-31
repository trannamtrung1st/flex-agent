import { render, screen } from "@testing-library/react";
import { DatatableDetailBody, DatatableDetailRow, DatatableExpandButton, DatatableIdCell } from "./DatatableDetailShell";
import { DatatableDetailField, DatatableDetailKeys, DatatableDetailReadouts } from "./DatatableDetailContent";

describe("DatatableDetailContent", () => {
  it("owns enrollment expand readout and key grammar", () => {
    render(
      <>
        <DatatableDetailReadouts>
          <DatatableDetailField term="Attempt">1</DatatableDetailField>
        </DatatableDetailReadouts>
        <DatatableDetailKeys>
          <button type="button">Open</button>
        </DatatableDetailKeys>
      </>,
    );

    expect(screen.getByText("1").closest(".datatable-detail-field")).toBeTruthy();
    expect(screen.getByText("Attempt").closest("dt")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Open" }).closest(".datatable-detail-keys")).toBeTruthy();
  });
});

describe("DatatableIdCell", () => {
  it("owns the identifier host and optional expand control", () => {
    render(
      <DatatableIdCell expand={<button type="button">Expand</button>}>
        <button type="button" className="datatable-id">P-1</button>
      </DatatableIdCell>,
    );

    const host = screen.getByRole("button", { name: "P-1" }).closest(".datatable-id-cell");
    expect(host).toHaveClass("datatable-id-cell");
    expect(screen.getByRole("button", { name: "Expand" })).toBeTruthy();
  });
});

describe("DatatableExpandButton", () => {
  it("owns the chevron trigger classes and expanded state", () => {
    render(<DatatableExpandButton expanded controls="detail-1" label="Collapse row" onClick={() => undefined} />);

    const trigger = screen.getByRole("button", { name: "Collapse row" });
    expect(trigger).toHaveClass("command-menu-trigger", "command-menu-trigger--icon", "is-open");
    expect(trigger).toHaveAttribute("aria-expanded", "true");
    expect(trigger).toHaveAttribute("aria-controls", "detail-1");
  });
});

describe("DatatableDetailRow", () => {
  it("owns the clipped detail row shell", () => {
    render(
      <table>
        <tbody>
          <DatatableDetailRow colSpan={3} id="detail-1">
            <DatatableDetailBody>
              <p>Attempt 1</p>
            </DatatableDetailBody>
          </DatatableDetailRow>
        </tbody>
      </table>,
    );

    expect(screen.getByText("Attempt 1").closest(".datatable-detail-body")).toBeTruthy();
    expect(document.getElementById("detail-1")).toHaveClass("datatable-detail-cut", "is-revealing");
  });
});
