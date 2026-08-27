import { useState } from "react";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { CampaignRegistry } from "./CampaignRegistry";
import { createCampaignActions } from "./campaignActions";
import { EMPTY_SELECTION } from "../../components/tableSelection";
import type { Campaign, CampaignRegistryRow, CampaignRegistryState } from "../../data/types";

const registryRows: CampaignRegistryRow[] = [
  { id: "CMP-1", name: "Alpha", frozen: false, enrollments: 2, deadline: null, updatedAt: new Date("2026-01-01") },
  { id: "CMP-2", name: "Bravo", frozen: true, enrollments: 4, deadline: null, updatedAt: new Date("2026-01-02") },
];

const campaigns: Campaign[] = [
  {
    id: "CMP-1",
    name: "Alpha",
    frozen: false,
    config: {
      harness: "H-1",
      agent: "A-1",
      sessionLimit: "60:00",
      timeWarning: "10:00",
      maxAttempts: "1",
      cooldown: "24H",
    },
    rows: [],
    updatedAt: new Date("2026-01-01"),
  },
  {
    id: "CMP-2",
    name: "Bravo",
    frozen: true,
    config: {
      harness: "H-2",
      agent: "A-2",
      sessionLimit: "60:00",
      timeWarning: "10:00",
      maxAttempts: "1",
      cooldown: "24H",
    },
    rows: [],
    updatedAt: new Date("2026-01-02"),
  },
];

function Harness({
  rows = registryRows,
  campaignList = campaigns,
  pageSize = 16,
}: {
  rows?: CampaignRegistryRow[];
  campaignList?: Campaign[];
  pageSize?: number;
} = {}) {
  const [state, setState] = useState<CampaignRegistryState>({
    search: "",
    activationFilter: "all",
    sorts: [{ key: "campaign", dir: "asc" }],
    page: 0,
    pageSize,
    selection: EMPTY_SELECTION,
  });
  const actions = createCampaignActions({ configure: vi.fn(), deleteCampaigns: vi.fn() });
  return (
    <CampaignRegistry
      rows={rows}
      campaigns={campaignList}
      state={state}
      setState={(patch) =>
        setState((prev) => (typeof patch === "function" ? patch(prev) : { ...prev, ...patch }))
      }
      announce={vi.fn()}
      onOpen={vi.fn()}
      actions={actions}
      onChoose={vi.fn()}
      busyActionId={null}
      confirm={{
        open: false,
        confirmation: null,
        error: null,
        waiting: false,
        onCancel: () => {},
        onConfirm: () => {},
      }}
    />
  );
}

describe("CampaignRegistry", () => {
  it("shows persistent Export and Download controls before selection", () => {
    render(<Harness />);
    expect(screen.getByRole("button", { name: /Export summary/i })).toBeDisabled();
    expect(screen.getByRole("button", { name: /Download configuration/i })).toBeDisabled();
    expect(screen.getByText("Export")).toBeVisible();
    expect(screen.getByText("Download")).toBeVisible();
    expect(screen.queryByText(/Select all matching/i)).not.toBeInTheDocument();
  });

  it("uses the shared header selector with four-state labels", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    const header = screen.getByRole("checkbox", { name: /Select all visible campaigns/i });
    await user.click(header);
    expect(screen.getByRole("checkbox", { name: /Clear selection/i })).toBeInTheDocument();
  });

  it("sorts from the shared header and clears selection when search changes", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    const table = screen.getByRole("table", { name: "Campaign registry" });

    await user.click(within(table).getByRole("button", { name: "Campaign" }));
    const rows = within(table).getAllByRole("row");
    expect(rows[1]).toHaveTextContent("CMP-2 / Bravo");

    await user.click(screen.getByRole("checkbox", { name: "Select CMP-2 / Bravo" }));
    const search = screen.getByRole("searchbox", { name: "Search campaign ID or name" });
    await user.type(search, "Bravo");
    await user.clear(search);
    expect(screen.getByRole("checkbox", { name: "Select CMP-2 / Bravo" })).not.toBeChecked();
  });

  it("shows All campaigns on the filter trigger and selects that option", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    const trigger = screen.getByRole("button", { name: /Filter:\s*All campaigns/i });
    expect(trigger).toHaveTextContent("All campaigns");
    await user.click(trigger);
    expect(screen.getByRole("option", { name: "All campaigns" })).toHaveAttribute("aria-selected", "true");
    await user.click(screen.getByRole("option", { name: "Frozen" }));
    expect(trigger).toHaveTextContent("Frozen");
    await user.click(trigger);
    expect(screen.getByRole("option", { name: "Frozen" })).toHaveAttribute("aria-selected", "true");
    await user.click(screen.getByRole("option", { name: "All campaigns" }));
    expect(trigger).toHaveTextContent("All campaigns");
  });

  it("paginates campaign rows through the shared footer callbacks", async () => {
    const user = userEvent.setup();
    const rows = Array.from({ length: 18 }, (_, index): CampaignRegistryRow => ({
      id: `CMP-${String(index + 1).padStart(4, "0")}`,
      name: `Campaign ${String(index + 1).padStart(2, "0")}`,
      frozen: false,
      enrollments: index,
      deadline: null,
      updatedAt: new Date(`2026-01-${String(index + 1).padStart(2, "0")}`),
    }));
    const campaignList = rows.map((row): Campaign => ({
      id: row.id,
      name: row.name,
      frozen: row.frozen,
      config: campaigns[0].config,
      rows: [],
      updatedAt: row.updatedAt,
    }));
    render(<Harness rows={rows} campaignList={campaignList} pageSize={8} />);

    expect(screen.getByText("01–08 OF 18")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Next" }));
    expect(screen.getByText("09–16 OF 18")).toBeVisible();
    expect(screen.getByRole("button", { name: "CMP-0009 / Campaign 09" })).toBeVisible();
  });
});
