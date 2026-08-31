import { render, screen } from "@testing-library/react";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  CampaignCeremonyBody,
  CampaignCeremonyFootActions,
  CampaignCeremonyFootRow,
  CampaignCeremonyFooter,
  CampaignCeremonyHead,
  CampaignCeremonyNote,
  CampaignCeremonyPlate,
  CampaignCeremonyConfigGrid,
} from "./CampaignCeremonyPlate";

describe("CampaignCeremonyPlate", () => {
  it("seats campaign ceremony plates without DialogPlate presentation", () => {
    render(
      <CampaignCeremonyPlate frozen>
        <CampaignCeremonyHead title="Campaign Configuration" titleId="configTitle" />
        <CampaignCeremonyBody>
          <p>Body</p>
        </CampaignCeremonyBody>
        <CampaignCeremonyFooter>
          <CampaignCeremonyFootActions>
            <CampaignCeremonyNote>Standing helper</CampaignCeremonyNote>
            <CampaignCeremonyFootRow aria-label="Actions">
              <button type="button">Save</button>
            </CampaignCeremonyFootRow>
          </CampaignCeremonyFootActions>
        </CampaignCeremonyFooter>
      </CampaignCeremonyPlate>,
    );

    expect(document.querySelector(".dialog-plate--wide.ceremony-plate.is-frozen")).toBeTruthy();
    expect(document.querySelector(".ceremony-head .ceremony-title")).toHaveTextContent("Campaign Configuration");
    expect(document.querySelector(".ceremony-trace-node")).toBeTruthy();
    expect(document.querySelector(".warn-triangle")).toBeNull();
    expect(document.querySelector(".ceremony-body")).toHaveTextContent("Body");
    const foot = document.querySelector(".ceremony-foot");
    expect(foot).toBeTruthy();
    expect(foot).not.toHaveClass("plate-foot");
    expect(foot?.querySelector(".ceremony-foot-actions .ceremony-note")).toHaveTextContent("Standing helper");
    expect(foot?.querySelector(".ceremony-foot-row.key-group")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Save" })).toBeInTheDocument();
  });

  it("owns the campaign config field grid class", () => {
    const { container } = render(
      <CampaignCeremonyConfigGrid>
        <span>Session limit</span>
      </CampaignCeremonyConfigGrid>,
    );
    expect(container.firstElementChild).toHaveClass("ceremony-config-grid", "composition-grid");
  });

  it("seats the campaign ceremony plate on frame inset tokens", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const ceremonyCss = readFileSync(join(here, "../../../styles/surfaces/admin-console.css"), "utf8");
    const plate = ceremonyCss.match(/\.ceremony-plate \{[^}]+\}/)?.[0] ?? "";
    expect(plate).toMatch(/padding:\s*0/);
    expect(plate).not.toMatch(/34px 44px 30px/);
    const compact = ceremonyCss.split("@media (max-width: 720px)")[1] ?? "";
    expect(compact).not.toMatch(/\.ceremony-plate \{ padding:/);
    const body = ceremonyCss.match(/\.ceremony-body \{[^}]+\}/)?.[0] ?? "";
    expect(body).toMatch(/padding-inline:\s*var\(--space-6\)/);
    expect(body).toMatch(/padding-block-end:\s*var\(--space-6\)/);
    expect(body).toMatch(/scrollbar-gutter:\s*auto/);
    expect(body).toMatch(/scrollbar-width:\s*thin/);
    expect(body).not.toMatch(/padding-inline-end:\s*var\(--space-3\)/);
    const note = ceremonyCss.match(/\.ceremony-note \{[^}]+\}/)?.[0] ?? "";
    expect(note).not.toMatch(/4px 0 8px/);
    expect(note).not.toMatch(/padding:\s*4px/);
    expect(ceremonyCss).toMatch(
      /\.ceremony-body > \.composition-stack \{[^}]*flex:\s*1 1 auto/,
    );
    expect(ceremonyCss).toMatch(
      /\.ceremony-body > \.ceremony-note \{[^}]*margin-block-start:\s*var\(--space-6\)/,
    );
    expect(ceremonyCss).toMatch(
      /\.ceremony-body > \.ceremony-note \{[^}]*max-width:\s*none/,
    );
  });

  it("keeps campaign config grid and fill-foot contracts in admin-console CSS", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const ceremonyCss = readFileSync(join(here, "../../../styles/surfaces/admin-console.css"), "utf8");
    expect(ceremonyCss).toMatch(
      /\.ceremony-head \{[^}]*padding-block-start:\s*var\(--space-6\)/,
    );
    expect(ceremonyCss).toMatch(
      /\.ceremony-foot \{[^}]*padding:\s*var\(--space-6\)/,
    );
    expect(ceremonyCss).toMatch(
      /\.ceremony-foot \{[^}]*border-block-start:\s*1px solid var\(--hairline-dim\)/,
    );
    expect(ceremonyCss).not.toMatch(/\.ceremony-foot \{[^}]*padding-inline-end:\s*var\(--space-3\)/);
    expect(ceremonyCss).not.toMatch(/\.ceremony-foot::before/);
    expect(ceremonyCss).toMatch(
      /\.ceremony-foot-actions > \.ceremony-note \{[^}]*max-width:\s*none/,
    );
    expect(ceremonyCss).not.toMatch(/\.ceremony-foot > \.ceremony-note/);
    expect(ceremonyCss).toMatch(
      /\.dialog-plate--wide\.ceremony-plate \{[^}]*--dialog-w:\s*840px/,
    );
    expect(ceremonyCss).toMatch(
      /\.ceremony-config-grid \{[^}]*grid-template-columns:\s*repeat\(4,\s*minmax\(0,\s*1fr\)\)/,
    );
    const compact = ceremonyCss.split("@media (max-width: 720px)")[1] ?? "";
    expect(compact).toMatch(
      /\.ceremony-config-grid \{[^}]*grid-template-columns:\s*repeat\(2,\s*minmax\(0,\s*1fr\)\)/,
    );
    expect(ceremonyCss).not.toMatch(/\.ceremony-receipt/);
  });
});
