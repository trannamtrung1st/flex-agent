import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
  DataTablePagination,
  DataTableShell,
  DataTableToolbar,
  SortableHeader,
  ToolbarReadout,
  ToolbarSearch,
} from ".";

describe("datatable primitives", () => {
  it("composes complete and body-only shells without changing canonical classes", () => {
    const { rerender } = render(
      <DataTableShell
        toolbar={<div>Toolbar slot</div>}
        table={<table aria-label="Rows" />}
        empty={<div>Empty slot</div>}
        footer={<div>Footer slot</div>}
        scrollProps={{ "aria-label": "Scrollable rows", tabIndex: 0 }}
      />,
    );

    const complete = screen.getByLabelText("Scrollable rows").parentElement;
    expect(complete).toHaveClass("datatable");
    expect(complete).not.toHaveClass("datatable--body-only");
    expect(screen.getByLabelText("Scrollable rows")).toHaveClass("datatable-scroll");
    expect(screen.getByText("Toolbar slot")).toBeVisible();
    expect(screen.getByText("Footer slot")).toBeVisible();

    rerender(<DataTableShell variant="bodyOnly" className="queue-datatable" body={<div>Body slot</div>} />);
    expect(screen.getByText("Body slot").parentElement?.parentElement).toHaveClass(
      "datatable",
      "datatable--body-only",
      "queue-datatable",
    );
  });

  it("sizes the sticky head rail from thead height", () => {
    vi.spyOn(HTMLElement.prototype, "getBoundingClientRect").mockImplementation(function (this: HTMLElement) {
      const tag = this.tagName;
      const height = tag === "THEAD" ? 40 : 0;
      return {
        x: 0,
        y: 0,
        width: 200,
        height,
        top: 0,
        right: 200,
        bottom: height,
        left: 0,
        toJSON() {
          return {};
        },
      };
    });

    render(
      <DataTableShell
        scrollProps={{ "aria-label": "Scrollable rows", tabIndex: 0 }}
        table={
          <table className="datatable-table">
            <thead>
              <tr>
                <th>Head</th>
              </tr>
            </thead>
            <tbody>
              <tr className="datatable-row">
                <td>Row</td>
              </tr>
            </tbody>
          </table>
        }
      />,
    );

    expect(screen.getByLabelText("Scrollable rows").style.getPropertyValue("--datatable-sticky-rail")).toBe("40px");
  });

  it("preserves toolbar live regions, labels, ids, and search behavior", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <DataTableToolbar
        ariaLabel="Registry controls"
        leading={<button type="button">Filter</button>}
        readout={<ToolbarReadout label="Showing" value="12 campaigns" valueId="campaignCountValue" />}
        search={
          <ToolbarSearch
            id="campaignSearchInput"
            label="Search campaign ID or name"
            placeholder="SEARCH ID OR NAME"
            value=""
            onChange={onChange}
          />
        }
      />,
    );

    expect(screen.getByLabelText("Registry controls")).toHaveClass("datatable-toolbar");
    expect(screen.getByText("12 campaigns").parentElement).toHaveAttribute("aria-live", "polite");
    expect(screen.getByText("12 campaigns")).toHaveAttribute("id", "campaignCountValue");
    const search = screen.getByRole("searchbox", { name: "Search campaign ID or name" });
    expect(search).toHaveAttribute("id", "campaignSearchInput");
    expect(search).toHaveClass("seg-search");
    await user.type(search, "A");
    expect(onChange).toHaveBeenCalled();
  });

  it("reports sort direction and rank while delegating sort behavior", async () => {
    const user = userEvent.setup();
    const onSort = vi.fn();
    render(
      <table>
        <thead>
          <tr>
            <SortableHeader
              sortKey="campaign"
              label="Campaign"
              sorts={[
                { key: "updated", dir: "desc" },
                { key: "campaign", dir: "asc" },
              ]}
              onSort={onSort}
            />
          </tr>
        </thead>
      </table>,
    );

    const header = screen.getByRole("columnheader", { name: "Campaign2" });
    expect(header).toHaveAttribute("aria-sort", "ascending");
    expect(header).toHaveAttribute("data-sort", "campaign");
    expect(screen.getByText("2")).toHaveClass("col-key-rank");
    await user.click(screen.getByRole("button", { name: "Campaign2" }));
    expect(onSort).toHaveBeenCalledWith("campaign");
  });

  it("renders the range and delegates every pagination transition", async () => {
    const user = userEvent.setup();
    const onPageSizeChange = vi.fn();
    const onPageChange = vi.fn();
    const onPrevious = vi.fn();
    const onNext = vi.fn();
    render(
      <DataTablePagination
        total={25}
        startIndex={8}
        visibleCount={8}
        page={1}
        pageCount={4}
        pageSize={8}
        pageSizeOptions={[8, 16]}
        onPageSizeChange={onPageSizeChange}
        onPageChange={onPageChange}
        onPrevious={onPrevious}
        onNext={onNext}
      />,
    );

    expect(screen.getByText("09–16 OF 25")).toHaveClass("datatable-range");
    await user.click(screen.getByRole("button", { name: "Prev" }));
    await user.click(screen.getByRole("button", { name: "Next" }));
    expect(onPrevious).toHaveBeenCalledOnce();
    expect(onNext).toHaveBeenCalledOnce();

    await user.click(screen.getByRole("button", { name: "Rows08" }));
    expect(screen.getByRole("option", { name: "08 per page" })).toHaveAttribute("aria-selected", "true");
    await user.click(screen.getByRole("option", { name: "16 per page" }));
    expect(onPageSizeChange).toHaveBeenCalledWith(16);

    await user.click(screen.getByRole("button", { name: "Page02" }));
    await user.click(screen.getByRole("option", { name: "03 OF 04" }));
    expect(onPageChange).toHaveBeenCalledWith(2);
  });
});
