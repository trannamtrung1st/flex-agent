import { render, screen } from "@testing-library/react";
import { CeremonyDialog } from "./CeremonyDialog";

describe("CeremonyDialog", () => {
  it("is a generic dialog shell without campaign or release skins", () => {
    render(
      <CeremonyDialog open onClose={() => undefined} labelledBy="title">
        <h2 id="title">Confirm</h2>
      </CeremonyDialog>,
    );

    const dialog = screen.getByRole("dialog", { name: "Confirm" });
    expect(dialog).toHaveClass("dialog");
    expect(dialog).not.toHaveClass("ceremony");
    expect(dialog).not.toHaveClass("release-dialog");
    expect(dialog.querySelector(".ceremony-cut")).toBeNull();
  });
});
