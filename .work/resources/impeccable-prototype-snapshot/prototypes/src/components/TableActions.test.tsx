import { useState } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
  HeaderSelectionControl,
  RowActionMenu,
  SelectMark,
  TableActionBar,
  TableSelectionBand,
  type TableAction,
} from "./TableActions";
import {
  EMPTY_SELECTION,
  isSelected,
  selectAllMatching,
  togglePage,
  toggleRow,
} from "./tableSelection";

type Row = { id: string; frozen: boolean };

const actions: TableAction<Row>[] = [
  {
    id: "configure",
    label: "Configure campaign",
    kind: "standard",
    placement: "overflow",
    surfaces: ["row"],
    eligibility: (records) =>
      records.some((row) => row.frozen)
        ? { allowed: false, reason: "Configuration frozen at activation" }
        : { allowed: true },
    run: () => ({ ok: true }),
  },
  {
    id: "export",
    label: "Export summary",
    kind: "standard",
    placement: "primary",
    eligibility: () => ({ allowed: true }),
    run: () => ({ ok: true }),
  },
  {
    id: "delete",
    label: "Delete",
    kind: "destructive",
    placement: "overflow",
    eligibility: () => ({ allowed: true }),
    run: () => ({ ok: true }),
  },
];

const tableAction: TableAction<{ id: string }> = {
  id: "create",
  label: "Create",
  surfaces: ["table"],
  kind: "standard",
  placement: "primary",
  eligibility: () => ({ allowed: true }),
  run: () => ({ ok: true }),
};

const bulkExport: TableAction<{ id: string }> = {
  id: "export",
  label: "Export summary",
  compactLabel: "Export",
  tooltip: "Export summary",
  kind: "standard",
  placement: "primary",
  eligibility: () => ({ allowed: true }),
  run: () => ({ ok: true }),
};

describe("TableActionBar", () => {
  it("stays mounted at rest with enabled table actions and disabled bulk controls", () => {
    render(
      <TableActionBar
        selection={EMPTY_SELECTION}
        pageIds={["a"]}
        matchingIds={["a", "b"]}
        noun="campaigns"
        actions={[tableAction, bulkExport]}
        records={[]}
        onChoose={() => {}}
      />,
    );

    expect(screen.getByLabelText("Table actions")).toBeVisible();
    expect(screen.getByRole("button", { name: "Create" })).toBeEnabled();
    expect(screen.getByRole("button", { name: /Export summary/i })).toBeDisabled();
    expect(screen.getByText("Export")).toBeVisible();
    expect(screen.queryByRole("button", { name: "Clear" })).not.toBeInTheDocument();
    expect(screen.queryByText(/Select all/i)).not.toBeInTheDocument();
  });

  it("disables More at rest and exposes the selection reason", () => {
    const bulkMore: TableAction<{ id: string }> = {
      id: "delete",
      label: "Delete",
      kind: "destructive",
      placement: "overflow",
      eligibility: () => ({ allowed: true }),
      run: () => ({ ok: true }),
    };
    render(
      <TableActionBar
        selection={EMPTY_SELECTION}
        pageIds={["a"]}
        matchingIds={["a"]}
        noun="campaigns"
        actions={[bulkExport, bulkMore]}
        records={[]}
        onChoose={() => {}}
      />,
    );

    expect(screen.getByRole("button", { name: /More actions/i })).toBeDisabled();
    expect(screen.getByText("More", { exact: true })).toBeVisible();
  });
});

describe("TableSelectionBand", () => {
  it("shows compact selection copy and unframed Clear with focus restoration", async () => {
    const user = userEvent.setup();
    const onClear = vi.fn();
    const pageSel = togglePage(EMPTY_SELECTION, ["a"], true);
    render(
      <TableSelectionBand
        selection={pageSel}
        pageIds={["a"]}
        matchingIds={["a", "b"]}
        noun="campaigns"
        headerSelectId="campaignSelectAll"
        onClear={onClear}
      />,
    );

    expect(screen.getByText(/01 selected on this page/i)).toBeVisible();
    const clear = screen.getByRole("button", { name: "Clear" });
    expect(clear).toHaveClass("clear-action");
    await user.click(clear);
    expect(onClear).toHaveBeenCalled();
  });

  it("renders nothing when the selection is empty", () => {
    const { container } = render(
      <TableSelectionBand
        selection={EMPTY_SELECTION}
        pageIds={["a"]}
        matchingIds={["a", "b"]}
        noun="campaigns"
        onClear={() => {}}
      />,
    );
    expect(container.querySelector(".datatable-selection-band")).toBeNull();
  });
});

describe("HeaderSelectionControl", () => {
  it("ignores the native checked flip and escalates page to matching", async () => {
    const user = userEvent.setup();
    const onTransition = vi.fn();
    const pageSel = togglePage(EMPTY_SELECTION, ["a", "b"], true);
    render(
      <HeaderSelectionControl
        id="campaignSelectAll"
        selection={pageSel}
        pageIds={["a", "b"]}
        matchingIds={["a", "b", "c"]}
        queryKey="q"
        noun="campaigns"
        onTransition={onTransition}
      />,
    );

    const checkbox = screen.getByRole("checkbox");
    expect(checkbox).toBeChecked();
    await user.click(checkbox);
    expect(onTransition).toHaveBeenCalledWith(selectAllMatching(["a", "b", "c"], "q"));
  });

  it("clears from matching when the header is activated again", async () => {
    const user = userEvent.setup();
    const onTransition = vi.fn();
    const all = selectAllMatching(["a", "b", "c"], "q");
    render(
      <HeaderSelectionControl
        id="campaignSelectAll"
        selection={all}
        pageIds={["a", "b"]}
        matchingIds={["a", "b", "c"]}
        queryKey="q"
        noun="campaigns"
        onTransition={onTransition}
      />,
    );

    await user.click(screen.getByRole("checkbox"));
    expect(onTransition).toHaveBeenCalledWith(EMPTY_SELECTION);
  });

  it("cycles partial, page, matching, and clear with the shared mark classes", async () => {
    const user = userEvent.setup();
    const pageIds = ["a", "b"];
    const matchingIds = ["a", "b", "c"];

    function HeaderCycleHarness() {
      const [selection, setSelection] = useState(EMPTY_SELECTION);
      return (
        <>
          <HeaderSelectionControl
            id="campaignSelectAll"
            selection={selection}
            pageIds={pageIds}
            matchingIds={matchingIds}
            queryKey="q"
            noun="campaigns"
            onTransition={setSelection}
          />
          <SelectMark
            checked={isSelected(selection, "a")}
            label="Select a"
            onChange={(checked) => setSelection(toggleRow(selection, "a", checked))}
          />
        </>
      );
    }

    const { container } = render(<HeaderCycleHarness />);
    const mark = () => container.querySelector(".select-head .select-mark");

    expect(mark()).toHaveClass("select-mark");
    expect(mark()).not.toHaveClass("select-mark--partial");

    await user.click(screen.getByRole("checkbox", { name: "Select a" }));
    expect(mark()).toHaveClass("select-mark--partial", "is-indeterminate");

    await user.click(screen.getByRole("checkbox", { name: /Select all visible campaigns/i }));
    expect(mark()).toHaveClass("select-mark--page");
    expect(mark()).not.toHaveClass("select-mark--partial");

    await user.click(screen.getByRole("checkbox", { name: /matching campaigns/i }));
    expect(mark()).toHaveClass("select-mark--matching");

    await user.click(screen.getByRole("checkbox", { name: /Clear selection/i }));
    expect(mark()).toHaveClass("select-mark");
    expect(mark()).not.toHaveClass("select-mark--matching");
  });
});

describe("RowActionMenu", () => {
  it("uses the compact icon trigger, not a labeled key", () => {
    render(
      <RowActionMenu
        open={false}
        onOpenChange={() => {}}
        label="Actions for CMP-1 / Alpha"
        records={[{ id: "CMP-1", frozen: false }]}
        actions={actions}
        onChoose={() => {}}
      />,
    );

    const trigger = screen.getByRole("button", { name: "Actions for CMP-1 / Alpha" });
    expect(trigger).toHaveClass("icon-button", "command-menu-trigger--icon");
    expect(trigger).not.toHaveClass("key");
    expect(trigger.querySelector(".action-menu-glyph")).toBeTruthy();
  });

  it("opens with keyboard, skips disabled items, and restores focus on Escape", async () => {
    const user = userEvent.setup();
    const onChoose = vi.fn();
    const onOpenChange = vi.fn();
    render(
      <RowActionMenu
        open
        onOpenChange={onOpenChange}
        label="Actions for CMP-1 / Alpha"
        records={[{ id: "CMP-1", frozen: true }]}
        actions={actions}
        onChoose={onChoose}
      />,
    );

    const exportItem = await screen.findByRole("menuitem", { name: /Export summary/i });
    expect(exportItem).toHaveFocus();
    await user.keyboard("{ArrowDown}");
    expect(screen.getByRole("menuitem", { name: /^Delete$/i })).toHaveFocus();
    expect(screen.getByRole("menuitem", { name: /Configure campaign/i })).toHaveAttribute("aria-disabled", "true");
    await user.keyboard("{Escape}");
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it("moves focus with Home and End and closes on Tab", async () => {
    const user = userEvent.setup();
    const onOpenChange = vi.fn();
    render(
      <RowActionMenu
        open
        onOpenChange={onOpenChange}
        label="Actions for CMP-1 / Alpha"
        records={[{ id: "CMP-1", frozen: false }]}
        actions={actions}
        onChoose={() => {}}
      />,
    );

    const configureItem = screen.getByRole("menuitem", { name: /Configure campaign/i });
    const exportItem = screen.getByRole("menuitem", { name: /Export summary/i });
    expect(configureItem).toHaveFocus();

    await user.keyboard("{End}");
    expect(screen.getByRole("menuitem", { name: /^Delete$/i })).toHaveFocus();

    await user.keyboard("{Home}");
    expect(configureItem).toHaveFocus();

    await user.keyboard("{ArrowDown}");
    expect(exportItem).toHaveFocus();

    await user.keyboard("{Tab}");
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });
});
