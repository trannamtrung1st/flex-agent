import { fireEvent, render, screen, within } from "@testing-library/react";
import { createCampaigns } from "../../data/fixtures/campaigns";
import { CampaignConfigDialog } from "./CampaignConfigDialog";

describe("CampaignConfigDialog", () => {
  const draft = createCampaigns().find((campaign) => campaign.id === "CMP-0044")!;
  const frozen = createCampaigns().find((campaign) => campaign.id === "CMP-0045")!;

  it("seats frozen activation on a wide ceremony plate with a full-span activated key", () => {
    render(
      <CampaignConfigDialog
        open
        onClose={() => undefined}
        campaign={frozen}
        onSaveDraft={() => undefined}
        onActivate={() => undefined}
      />,
    );

    const dialog = screen.getByRole("dialog", { name: /Campaign Configuration/i });
    expect(dialog.querySelector(".dialog-plate--wide.ceremony-plate.is-frozen")).toBeTruthy();
    expect(within(dialog).getByText("Configuration frozen at activation")).toBeInTheDocument();
    expect(within(dialog).queryByRole("button", { name: "Save draft" })).not.toBeInTheDocument();
    expect(within(dialog).queryByRole("button", { name: "Check readiness" })).not.toBeInTheDocument();

    const activated = within(dialog).getByRole("button", { name: "Activated" });
    expect(activated).toBeDisabled();
    expect(activated).toHaveClass("key--activate", "key--truncate");
    expect(activated.closest(".tip-host:only-child")?.parentElement).toHaveClass("ceremony-foot-row", "key-group");

    fireEvent.mouseEnter(activated.closest(".tip-host")!);
    expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
  });

  it("plaques a disabled confirm key reason without clipping the caption", () => {
    render(
      <CampaignConfigDialog
        open
        onClose={() => undefined}
        campaign={draft}
        onSaveDraft={() => undefined}
        onActivate={() => undefined}
      />,
    );

    const dialog = screen.getByRole("dialog", { name: /Campaign Configuration/i });
    const confirm = within(dialog).getByRole("button", { name: /Confirm activation/ });
    expect(confirm).toBeDisabled();
    expect(confirm).toHaveClass("key--truncate");

    fireEvent.mouseEnter(confirm.closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toHaveTextContent("Check readiness before activation");
  });

  it("groups configuration fields in FormSection legends aligned with campaign setup", () => {
    render(
      <CampaignConfigDialog
        open
        onClose={() => undefined}
        campaign={draft}
        onSaveDraft={() => undefined}
        onActivate={() => undefined}
      />,
    );

    const dialog = screen.getByRole("dialog", { name: /Campaign Configuration/i });
    const sections = dialog.querySelectorAll(".form-section");
    expect(sections).toHaveLength(2);
    expect(within(dialog).getByText("Agent and Harness")).toBeInTheDocument();
    expect(within(dialog).getByText("Timing and attempts")).toBeInTheDocument();
    expect(dialog.querySelector(".form-divider")).toBeNull();
    expect(sections[0]?.parentElement).toHaveClass("composition-stack");
    expect(sections[0]?.parentElement).toHaveAttribute("data-flow-gap", "6");
    expect(sections[0]?.nextElementSibling).toBe(sections[1]);
  });
});
