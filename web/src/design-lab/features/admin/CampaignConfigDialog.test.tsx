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
    expect(dialog).toHaveClass("dialog", "ceremony");
    expect(dialog.querySelector(".ceremony-cut")).toBeTruthy();
    expect(dialog.querySelector(".dialog-plate--wide.ceremony-plate.is-frozen")).toBeTruthy();
    expect(within(dialog).getByText("Configuration frozen at activation")).toBeInTheDocument();
    expect(within(dialog).queryByRole("button", { name: "Save draft" })).not.toBeInTheDocument();
    expect(within(dialog).queryByRole("button", { name: "Check readiness" })).not.toBeInTheDocument();

    const activated = within(dialog).getByRole("button", { name: "Activated" });
    expect(activated).toBeDisabled();
    expect(activated).toHaveClass("key--activate", "key--truncate");
    expect(activated.closest(".tip-host:only-child")?.parentElement).toHaveClass("ceremony-foot-row", "key-group");

    const body = dialog.querySelector(".ceremony-body") as HTMLElement;
    const foot = dialog.querySelector(".ceremony-foot") as HTMLElement;
    const helper = body.querySelector(":scope > .ceremony-note");
    expect(helper).toBeTruthy();
    expect(helper?.parentElement).toBe(body);
    expect(within(foot).queryByRole("status")).not.toBeInTheDocument();
    expect(foot.querySelector(".ceremony-foot-actions")).toBeTruthy();

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
    const plaque = screen.getByRole("tooltip");
    expect(plaque).toHaveTextContent("Check readiness before activation");
    expect(plaque.parentElement).toBe(dialog);
    expect(dialog.querySelector(":scope > .dialog-stage")?.contains(plaque)).toBe(false);
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
    const helper = within(dialog).getByText(/Save a draft and check readiness/);
    expect(helper).toHaveClass("ceremony-note");
    expect(helper.parentElement).toHaveClass("ceremony-body");
    expect(helper.previousElementSibling).toHaveClass("composition-stack");
  });

  it("lays out timing instruments in one row inside the ceremony config grid", () => {
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
    const timingSection = within(dialog).getByRole("group", { name: "Timing and attempts" });
    const timingGrid = timingSection.querySelector(".ceremony-config-grid");
    expect(timingGrid).toBeTruthy();
    expect(timingGrid?.children).toHaveLength(4);
    expect(within(timingSection).getByLabelText("Session limit")).toBeInTheDocument();
    expect(within(timingSection).getByLabelText("Time warning at")).toBeInTheDocument();
    expect(within(timingSection).getByLabelText("Max attempts")).toBeInTheDocument();
    expect(within(timingSection).getByLabelText("Cooldown")).toBeInTheDocument();
  });

  it("keeps the save receipt inside the consistently padded action foot", async () => {
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
    const body = dialog.querySelector(".ceremony-body") as HTMLElement;
    const foot = dialog.querySelector(".ceremony-foot") as HTMLElement;
    expect(within(body).getByText(/Save a draft and check readiness/)).toBeInTheDocument();
    expect(within(body).queryByRole("status")).not.toBeInTheDocument();
    expect(within(foot).queryByRole("status")).not.toBeInTheDocument();

    fireEvent.click(within(dialog).getByRole("button", { name: "Save draft" }));

    const receipt = await within(foot).findByRole("status");
    expect(receipt).toHaveTextContent(/Draft saved locally/);
    expect(receipt).toHaveClass("ceremony-note");
    expect(receipt.parentElement).toHaveClass("ceremony-foot-actions");
    expect(receipt.nextElementSibling).toHaveClass("key-group", "ceremony-foot-row");
    expect(within(body).queryByText(/Draft saved locally/)).not.toBeInTheDocument();
    expect(within(body).getByText(/Save a draft and check readiness/)).toBeInTheDocument();
  });

  it("clears the pinned receipt when confirming activation", async () => {
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
    fireEvent.click(within(dialog).getByRole("button", { name: "Check readiness" }));
    expect(await within(dialog).findByRole("status")).toHaveTextContent(/Readiness check passed/);
    fireEvent.click(within(dialog).getByRole("button", { name: "Confirm activation" }));
    expect(within(dialog).queryByRole("status")).not.toBeInTheDocument();
    expect(within(dialog).getByText(/Confirm activation. This design lab/)).toBeInTheDocument();
  });

  it("presents blocked readiness as an error summary instead of a foot note", async () => {
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
    fireEvent.change(within(dialog).getByRole("textbox", { name: "Session limit" }), { target: { value: "bad" } });
    fireEvent.click(within(dialog).getByRole("button", { name: "Check readiness" }));

    const summary = await within(dialog).findByRole("alert");
    expect(within(summary).getByRole("heading", { name: "Readiness blocked" })).toBeInTheDocument();
    expect(within(summary).getByRole("link", { name: /Session limit must read/ })).toHaveAttribute("href", "#sessionLimit");
    expect(within(dialog).queryByRole("status")).not.toBeInTheDocument();
  });
});
