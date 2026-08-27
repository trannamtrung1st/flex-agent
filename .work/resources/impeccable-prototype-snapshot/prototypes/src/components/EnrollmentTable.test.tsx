import { useState } from "react";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DataTable } from "./EnrollmentTable";
import { EMPTY_SELECTION } from "./tableSelection";
import type { DataTableState, EnrollmentRow } from "../data/types";

const rows: EnrollmentRow[] = [
  {
    id: "P-3121",
    campaign: "CMP-0042",
    stage: "EXAMINATION",
    result: "LIVE",
    deadline: new Date("2026-08-30T00:00:00"),
    attempt: "1 OF 2",
    duration: "42:11",
    submission: "V1",
    evidence: "4 ITEMS",
  },
];

function Harness({ onOpenRecord }: { onOpenRecord?: (row: EnrollmentRow) => void }) {
  const [state, setState] = useState<DataTableState>({
    stageFilter: null,
    search: "",
    sorts: [{ key: "deadline", dir: "asc" }],
    page: 0,
    pageSize: 16,
    selection: EMPTY_SELECTION,
    expandedId: null,
  });
  const announce = vi.fn();
  return (
    <DataTable
      rows={rows}
      state={state}
      setState={(patch) =>
        setState((prev) => (typeof patch === "function" ? patch(prev) : { ...prev, ...patch }))
      }
      announce={announce}
      stages={["BRIEFING", "EXAMINATION"]}
      onOpenRecord={onOpenRecord}
    />
  );
}

describe("DataTable row actions", () => {
  it("shows All stages on the filter trigger when no stage is selected", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    const trigger = screen.getByRole("button", { name: /Filter:\s*All stages/i });
    expect(trigger).toHaveTextContent("All stages");
    await user.click(trigger);
    expect(screen.getByRole("option", { name: "All stages" })).toHaveAttribute("aria-selected", "true");
    await user.click(screen.getByRole("option", { name: "EXAMINATION" }));
    expect(trigger).toHaveTextContent("EXAMINATION");
    await user.click(trigger);
    expect(screen.getByRole("option", { name: "EXAMINATION" })).toHaveAttribute("aria-selected", "true");
    await user.click(screen.getByRole("option", { name: "All stages" }));
    expect(trigger).toHaveTextContent("All stages");
  });

  it("does not expand or open when an ordinary content cell is clicked", async () => {
    const user = userEvent.setup();
    const onOpenRecord = vi.fn();
    render(<Harness onOpenRecord={onOpenRecord} />);

    const table = screen.getByRole("table", { name: /Enrollments for the selected campaign/i });
    const stageCell = within(table).getByRole("cell", { name: "EXAMINATION" });
    await user.click(stageCell);

    expect(screen.queryByRole("button", { name: "View record" })).not.toBeInTheDocument();
    expect(onOpenRecord).not.toHaveBeenCalled();
  });

  it("selects a row from the checkbox without opening or expanding", async () => {
    const user = userEvent.setup();
    const onOpenRecord = vi.fn();
    render(<Harness onOpenRecord={onOpenRecord} />);

    await user.click(screen.getByRole("checkbox", { name: "Select P-3121" }));

    const row = screen.getByRole("checkbox", { name: "Select P-3121" }).closest("tr");
    expect(row).toHaveClass("is-selected");
    expect(screen.queryByRole("button", { name: "View record" })).not.toBeInTheDocument();
    expect(onOpenRecord).not.toHaveBeenCalled();
  });

  it("opens the record from the identifier only", async () => {
    const user = userEvent.setup();
    const onOpenRecord = vi.fn();
    render(<Harness onOpenRecord={onOpenRecord} />);

    await user.click(screen.getByRole("button", { name: "P-3121" }));

    expect(onOpenRecord).toHaveBeenCalledOnce();
    expect(onOpenRecord.mock.calls[0][0].id).toBe("P-3121");
    expect(screen.queryByRole("button", { name: "View record" })).not.toBeInTheDocument();
  });

  it("expands and collapses inline detail from the disclosure control", async () => {
    const user = userEvent.setup();
    render(<Harness />);

    const disclosure = screen.getByRole("button", { name: "Expand enrollment P-3121" });
    expect(disclosure).toHaveAttribute("aria-expanded", "false");

    await user.click(disclosure);
    expect(disclosure).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByRole("button", { name: "View record" })).toBeVisible();
    expect(screen.getByText("42:11")).toBeVisible();

    await user.click(screen.getByRole("button", { name: "Collapse enrollment P-3121" }));
    expect(screen.queryByRole("button", { name: "View record" })).not.toBeInTheDocument();
  });

  it("clears selection when search changes", async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(screen.getByRole("checkbox", { name: "Select P-3121" }));
    expect(screen.getByRole("checkbox", { name: "Select P-3121" })).toBeChecked();

    const search = screen.getByRole("searchbox", { name: /Search participant ID/i });
    await user.type(search, "NOMATCH");
    expect(screen.getByText("No matching enrollments")).toBeVisible();

    await user.clear(search);
    expect(screen.getByRole("checkbox", { name: "Select P-3121" })).not.toBeChecked();
  });

  it("shows the selection band under the filter toolbar when rows are selected", async () => {
    const user = userEvent.setup();
    render(<Harness />);

    expect(screen.queryByRole("button", { name: "Clear" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("checkbox", { name: "Select P-3121" }));
    expect(screen.getByRole("button", { name: "Clear" })).toBeVisible();
    expect(screen.getByText(/01 selected/i)).toBeVisible();
  });

  it("preserves selection across pagination", async () => {
    const user = userEvent.setup();
    const manyRows: EnrollmentRow[] = Array.from({ length: 20 }, (_, i) => ({
      id: `P-${3200 + i}`,
      campaign: "CMP-0042",
      stage: "EXAMINATION",
      result: "LIVE",
      deadline: new Date("2026-08-30T00:00:00"),
      attempt: "1 OF 2",
      duration: "42:11",
      submission: "V1",
      evidence: "4 ITEMS",
    }));

    function ManyHarness() {
      const [state, setState] = useState<DataTableState>({
        stageFilter: null,
        search: "",
        sorts: [{ key: "deadline", dir: "asc" }],
        page: 0,
        pageSize: 8,
        selection: EMPTY_SELECTION,
        expandedId: null,
      });
      return (
        <DataTable
          rows={manyRows}
          state={state}
          setState={(patch) =>
            setState((prev) => (typeof patch === "function" ? patch(prev) : { ...prev, ...patch }))
          }
          announce={vi.fn()}
          stages={["EXAMINATION"]}
        />
      );
    }

    render(<ManyHarness />);
    await user.click(screen.getByRole("checkbox", { name: "Select P-3200" }));
    await user.click(screen.getByRole("button", { name: "Next" }));
    expect(screen.queryByRole("checkbox", { name: "Select P-3200" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Prev" }));
    expect(screen.getByRole("checkbox", { name: "Select P-3200" })).toBeChecked();
  });
});
