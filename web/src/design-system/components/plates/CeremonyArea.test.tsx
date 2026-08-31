import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { CeremonyArea, CeremonyUnavailable, CeremonyWait } from "./CeremonyArea";

describe("CeremonyUnavailable", () => {
  it("is a hug ceremony plane with an inset empty well and a quiet recovery key", () => {
    render(
      <MemoryRouter>
        <CeremonyUnavailable
          label="This destination is not available"
          title="This destination is not available"
          note="The current authorized relationship cannot use this locator."
          recovery={{ label: "Return to Home", to: "/" }}
        />
      </MemoryRouter>,
    );

    const region = screen.getByRole("region", { name: "This destination is not available" });
    expect(region).toHaveClass("work-plane--ceremony");
    expect(region.querySelector(":scope > .operate-column--hug")).toHaveAttribute("data-hug-measure", "auto");
    expect(region.querySelector(":scope > .operate-column--hug")).toBeTruthy();
    expect(screen.getByText("The current authorized relationship cannot use this locator.").closest(".empty-plate")).toHaveClass(
      "empty-plate--inset",
      "ceremony-empty",
    );
    const recovery = screen.getByRole("link", { name: "Return to Home" });
    expect(recovery).toHaveAttribute("href", "/");
    expect(recovery).toHaveClass("key", "key--quiet");
    expect(recovery).not.toHaveClass("key--open");
    expect(recovery).not.toHaveClass("key--transmit");
    expect(recovery.parentElement).toHaveClass("tip-host");
  });

  it("lights Continue to sign in as a large transmit recovery, not a quiet Return key", () => {
    render(
      <CeremonyUnavailable
        title="Sign-in could not be completed"
        note="Sign-in could not be completed. No application session was created."
        danger
        alert
        recovery={{ label: "Continue to sign in", variant: "transmit", onClick: () => {} }}
      />,
    );

    const recovery = screen.getByRole("button", { name: "Continue to sign in" });
    expect(recovery).toHaveClass("key", "key--transmit", "key--large");
    expect(recovery).not.toHaveClass("key--quiet");
    expect(recovery).not.toHaveClass("key--open");
    expect(screen.getByRole("heading", { name: "Sign-in could not be completed" }).closest(".workspace-area")).toHaveClass(
      "workspace-area--danger",
    );
    expect(screen.getByRole("alert")).toHaveTextContent("Sign-in could not be completed. No application session was created.");
  });

  it("lights denied ceremony titles with fault phosphor, not teal emission", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const tokens = readFileSync(join(here, "../../../styles/tokens.css"), "utf8");
    const appShell = readFileSync(join(here, "../../../styles/app-shell.css"), "utf8");
    expect(tokens).toMatch(/--danger:\s*#f05c58/i);
    expect(tokens).toMatch(/--danger-bright:\s*#ff7468/i);
    expect(tokens).toMatch(/--danger-glow:\s*rgba\(240,\s*92,\s*88,\s*0\.32\)/);
    const dangerTitle = appShell.match(/\.workspace-area--danger \.operate-title \{[^}]+\}/)?.[0] ?? "";
    expect(dangerTitle).toContain("var(--fg-danger)");
    expect(dangerTitle).toContain("var(--danger-glow)");
    expect(dangerTitle).not.toContain("192, 191");
    const lightDanger = appShell.match(
      /\[data-theme="light"\] \.workspace-area--danger \.operate-title \{[^}]+\}/,
    )?.[0] ?? "";
    expect(lightDanger).toContain("var(--danger-glow)");
  });

  it("hugs auto empty wells to the note track instead of stretching to the wait cap", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const plates = readFileSync(join(here, "../../../styles/components/plates.css"), "utf8");
    expect(plates).toMatch(
      /\.operate-column--hug\[data-hug-measure="auto"\]\s+\.empty-plate--inset\s*\{[^}]*grid-template-columns:\s*7px max-content/,
    );
    expect(plates).not.toMatch(
      /\.operate-column--hug\[data-hug-measure="auto"\]:is\(:has\(\.wait-plate--inset\), :has\(\.empty-plate--inset\)\)/,
    );
    expect(plates).toMatch(
      /\.operate-column--hug\[data-hug-measure="auto"\]:has\(\.wait-plate--inset\)\s*\{[^}]*--operate-hug-w:\s*var\(--operate-column-max\)/,
    );
    const appShell = readFileSync(join(here, "../../../styles/app-shell.css"), "utf8");
    expect(appShell).toMatch(
      /@media \(max-width: 720px\)[\s\S]*?\.work-plane--ceremony > \.operate-column--hug\[data-hug-measure="auto"\] \.empty-plate--inset \{[^}]*grid-template-columns:\s*7px minmax\(0,\s*1fr\)/,
    );
  });

  it("centers the recovery key across the empty well, not the note start edge", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const appShell = readFileSync(join(here, "../../../styles/app-shell.css"), "utf8");
    const recovery = appShell.match(
      /\.ceremony-empty\.empty-plate--inset > \.tip-host,\s*\.ceremony-empty\.empty-plate--inset > \.key \{[^}]+\}/,
    )?.[0] ?? "";
    expect(recovery).toMatch(/grid-column:\s*1\s*\/\s*-1/);
    expect(recovery).toMatch(/justify-self:\s*center/);
  });
});

describe("CeremonyArea", () => {

  it("fills the management main slot so hug centering applies on both axes", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const layouts = readFileSync(join(here, "../../../styles/components/layouts.css"), "utf8");
    const gallery = readFileSync(join(here, "../../../styles/surfaces/gallery.css"), "utf8");
    expect(layouts).toMatch(
      /\.layout-management__main > \.work-plane--ceremony,\s*\.layout-management__main > \.composition-inset > \.work-plane--ceremony \{[^}]*flex:\s*1\s*1\s*auto/,
    );
    expect(layouts).toMatch(
      /\.layout-management__main > \.work-plane--ceremony,\s*\.layout-management__main > \.composition-inset > \.work-plane--ceremony \{[^}]*min-height:\s*0/,
    );
    expect(gallery).toMatch(
      /\.layout-spec \.workspace-area\.work-plane--ceremony \{[^}]*flex:\s*1\s*1\s*auto/,
    );
    expect(layouts).toMatch(
      /\.layout-management__main:has\(\.breadcrumb-nav\) > \.work-plane--ceremony,\s*\.layout-management__main:has\(\.breadcrumb-nav\) > \.composition-inset > \.work-plane--ceremony \{[^}]*padding-block:/,
    );
  });

  it("seats an inset wait plate in the hug ceremony well", () => {
    render(
      <CeremonyArea
        label="Establishing session"
        title="Establishing session"
        description="Confirming the production application session for this organization."
      >
        <CeremonyWait label="Establishing session context…" />
      </CeremonyArea>,
    );

    const region = screen.getByRole("region", { name: "Establishing session" });
    expect(region.querySelector(":scope > .operate-column--hug")).toHaveAttribute("data-hug-measure", "auto");
    const status = screen.getByRole("status");
    expect(status).toHaveClass("wait-plate", "wait-plate--inset", "ceremony-wait");
    expect(status.closest(".frame-in")).toBeTruthy();
    expect(screen.getByText("Establishing session context…")).toBeVisible();
    expect(status.querySelector(".scan-track.is-waiting")).toBeTruthy();
  });
});
