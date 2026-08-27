import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { createCampaigns } from "../../data/fixtures/campaigns";
import { EnrollmentsArea } from "./EnrollmentsArea";

const campaigns = createCampaigns();

const announce = vi.fn();

vi.mock("./adminContext", () => ({
  useAdminContext: vi.fn(),
}));

import { useAdminContext } from "./adminContext";

function mockCampaign(campaignId: string) {
  vi.mocked(useAdminContext).mockReturnValue({
    campaigns,
    campaign: campaigns.find((item) => item.id === campaignId),
    campaignId,
    setCampaignId: vi.fn(),
    announce,
    sealing: false,
    setCampaigns: vi.fn(),
    pushToast: vi.fn(),
    setSealing: vi.fn(),
  } as ReturnType<typeof useAdminContext>);
}

describe("EnrollmentsArea", () => {
  it("resets table state when the campaign changes", async () => {
    const user = userEvent.setup();
    mockCampaign("CMP-0042");
    const { rerender } = render(<EnrollmentsArea />);

    await user.click(screen.getByRole("checkbox", { name: "Select P-3121" }));
    expect(screen.getByRole("checkbox", { name: "Select P-3121" })).toBeChecked();

    const search = screen.getByRole("searchbox", { name: /Search participant ID/i });
    await user.type(search, "P-3121");
    expect(search).toHaveValue("P-3121");

    mockCampaign("CMP-0043");
    rerender(<EnrollmentsArea />);

    expect(screen.getByRole("searchbox", { name: /Search participant ID/i })).toHaveValue("");
    expect(screen.queryByRole("checkbox", { name: "Select P-3121" })).not.toBeInTheDocument();
    expect(screen.getByRole("checkbox", { name: "Select P-4201" })).not.toBeChecked();
  });
});
