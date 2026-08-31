import { render, screen } from "@testing-library/react";
import { CampaignCeremonyDialog } from "./CampaignCeremonyDialog";

describe("CampaignCeremonyDialog", () => {
  it("seats campaign fill overlays in the ceremony cut", () => {
    render(
      <CampaignCeremonyDialog open onClose={() => undefined} labelledBy="configTitle">
        <h2 id="configTitle">Campaign Configuration</h2>
      </CampaignCeremonyDialog>,
    );

    const dialog = screen.getByRole("dialog", { name: "Campaign Configuration" });
    expect(dialog).toHaveClass("dialog", "ceremony");
    expect(dialog).not.toHaveClass("release-dialog");
    expect(dialog.querySelector(".dialog-stage > .ceremony-cut")).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Campaign Configuration" }).closest(".ceremony-cut")).toBeTruthy();
  });
});
