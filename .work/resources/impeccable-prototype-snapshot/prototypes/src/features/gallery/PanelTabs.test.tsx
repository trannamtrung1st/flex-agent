import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { PanelTabs } from "./PanelTabs";

describe("PanelTabs", () => {
  it("automatically activates tabs while roving focus", async () => {
    const user = userEvent.setup();
    render(<PanelTabs label="Record panels" tabs={[
      { id: "manifest", label: "Manifest", panel: "Manifest panel" },
      { id: "transcript", label: "Transcript", panel: "Transcript panel" },
      { id: "evaluation", label: "Evaluation", panel: "Evaluation panel" },
    ]} />);

    const manifest = screen.getByRole("tab", { name: "Manifest" });
    const transcript = screen.getByRole("tab", { name: "Transcript" });
    manifest.focus();
    await user.keyboard("{ArrowRight}");
    expect(transcript).toHaveFocus();
    expect(transcript).toHaveAttribute("aria-selected", "true");
    expect(screen.getByRole("tabpanel")).toHaveTextContent("Transcript panel");
    await user.keyboard("{End}");
    const evaluation = screen.getByRole("tab", { name: "Evaluation" });
    expect(evaluation).toHaveFocus();
    expect(evaluation).toHaveAttribute("aria-selected", "true");
    await user.keyboard("{Home}");
    expect(manifest).toHaveFocus();
    expect(manifest).toHaveAttribute("aria-selected", "true");
  });
});
