import { render, screen, within } from "@testing-library/react";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { MemoryRouter } from "react-router-dom";
import { GalleryDeck } from "./GalleryDeck";
import { gallerySectionItems } from "./gallerySections";

function renderGalleryDeck() {
  return render(
    <MemoryRouter initialEntries={["/design-lab/shared/gallery"]}>
      <GalleryDeck />
    </MemoryRouter>,
  );
}

describe("GalleryDeck", () => {
  it("renders every registered section with a matching anchor target", () => {
    renderGalleryDeck();
    for (const item of gallerySectionItems) {
      expect(document.getElementById(item.id)).toBeTruthy();
    }
  });

  it("renders sections in registry scroll order", () => {
    renderGalleryDeck();
    const renderedIds = Array.from(document.querySelectorAll<HTMLElement>(".deck-sec")).map(
      (section) => section.id,
    );
    expect(renderedIds).toEqual(gallerySectionItems.map((item) => item.id));
  });

  it("renders Key group as a dedicated section", () => {
    renderGalleryDeck();
    expect(screen.getByRole("heading", { name: "Key group" })).toBeInTheDocument();
    expect(screen.getByRole("group", { name: "Ceremony actions" })).toBeInTheDocument();
  });

  it("lists Keys and Key group under Foundations in the index rail", () => {
    renderGalleryDeck();
    const index = screen.getByRole("navigation", { name: "Component index" });
    const labels = within(index).getAllByRole("link").map((link) => link.textContent);
    const typeIdx = labels.indexOf("Type voices");
    const typographyIdx = labels.indexOf("Typography");
    const keysIdx = labels.indexOf("Keys");
    const keyGroupIdx = labels.indexOf("Key group");
    const paneIdx = labels.indexOf("Pane");
    expect(within(index).getByRole("link", { name: "Keys" })).toHaveAttribute("href", "#keys");
    expect(within(index).getByRole("link", { name: "Key group" })).toHaveAttribute("href", "#key-group");
    expect(typeIdx).toBeLessThan(typographyIdx);
    expect(typographyIdx).toBeLessThan(keysIdx);
    expect(keysIdx).toBeLessThan(keyGroupIdx);
    expect(keyGroupIdx).toBeLessThan(paneIdx);
    expect(screen.queryByRole("group", { name: "Keys" })).not.toBeInTheDocument();
  });

  it("renders full-width layout specimens with labeled slot regions", () => {
    renderGalleryDeck();
    const management = document.getElementById("layout-management");
    expect(management?.querySelectorAll(".layout-spec")).toHaveLength(2);
    expect(management?.querySelector(".spec--wide")).toBeTruthy();
    expect(within(management!).getAllByRole("navigation", { name: "Primary navigation" })).toHaveLength(2);
    expect(within(management!).getAllByText("Main work bay")).toHaveLength(2);
    expect(within(management!).getAllByText("Quiet footer")).toHaveLength(2);
    expect(within(document.getElementById("layout-guided-task")!).getByText("Instrument rail")).toBeInTheDocument();
    expect(within(document.getElementById("layout-live-session")!).getByText("Examiner plate")).toBeInTheDocument();
    expect(within(document.getElementById("layout-reference")!).getByText("Catalog main")).toBeInTheDocument();
  });

  it("keeps a single page main and skip link on the component deck", () => {
    renderGalleryDeck();
    expect(screen.getAllByRole("main")).toHaveLength(1);
    expect(screen.getAllByRole("link", { name: "Skip to main content" })).toHaveLength(1);
    expect(document.querySelectorAll("#main-content")).toHaveLength(1);
    expect(document.getElementById("layout-management")?.querySelector("main")).toBeNull();
    expect(document.getElementById("layout-guided-task")?.querySelector("main")).toBeNull();
    expect(document.getElementById("layout-live-session")?.querySelector("main")).toBeNull();
    expect(document.getElementById("layout-reference")?.querySelector("main")).toBeNull();
  });

  it("stretches guided-task specimen slots through the spec frame", () => {
    renderGalleryDeck();
    const guided = document.getElementById("layout-guided-task")!;
    expect(guided.querySelector('[data-layout="guided-task"]')).toBeTruthy();
    expect(guided.querySelector(".layout-guided__main .layout-slot")).toBeTruthy();
    expect(guided.querySelector(".phase-rail-scroll .layout-slot--rail")).toBeTruthy();
    const galleryCss = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../../../styles/surfaces/gallery.css"),
      "utf8",
    );
    expect(galleryCss).toContain('[data-layout="guided-task"] .phase-rail-scroll > .layout-slot');
    expect(galleryCss).toContain('[data-layout="guided-task"] .layout-guided__main > .layout-slot');
    expect(galleryCss).toContain('[data-layout="live-session"] .layout-session__main > .layout-slot');
  });

  it("constrains management work-bay variants to title, description, optional back, and body", () => {
    renderGalleryDeck();

    const index = document.getElementById("layout-management-index")!;
    expect(within(index).getByRole("heading", { level: 1, name: "Campaign Registry" })).toBeInTheDocument();
    expect(within(index).getByText("Find a campaign, then open its record to inspect or configure.")).toBeInTheDocument();
    expect(within(index).queryByRole("button", { name: "Campaigns" })).not.toBeInTheDocument();
    expect(within(index).getByRole("region", { name: "Campaign registry" })).toHaveTextContent("Shoreline Operations");
    expect(index.querySelector(".layout-management__main > .composition-inset")).toBeTruthy();

    const management = document.getElementById("layout-management")!;
    const containVariants = management.querySelectorAll(".layout-management__main > .composition-inset");
    expect(containVariants).toHaveLength(1);
    expect(management.querySelectorAll(".layout-spec")).toHaveLength(2);
    expect(management.querySelector(".spec-row--layout-contain")).toBeTruthy();

    const record = document.getElementById("layout-management-record")!;
    expect(within(record).getByRole("heading", { level: 1, name: "Campaign Record" })).toBeInTheDocument();
    expect(within(record).getByRole("button", { name: "Campaigns" })).toBeInTheDocument();
    expect(within(record).getByText("Configuration and activation for CAMP-2204 / Shoreline Operations.")).toBeInTheDocument();
    expect(within(record).getByRole("region", { name: "Campaign configuration" })).toHaveTextContent("CAMP-2204");

    const empty = document.getElementById("layout-management-empty")!;
    expect(within(empty).getByRole("heading", { level: 1, name: "Campaign Registry" })).toBeInTheDocument();
    expect(within(empty).getByText("No campaigns listed")).toBeInTheDocument();
    expect(within(empty).queryByRole("button", { name: "Campaigns" })).not.toBeInTheDocument();

    const split = document.getElementById("layout-management-split")!;
    expect(within(split).getByRole("heading", { level: 1, name: "Evaluation Record" })).toBeInTheDocument();
    expect(within(split).getByRole("button", { name: "Queue" })).toBeInTheDocument();
    expect(split.querySelector(".layout-management__main > .composition-inset")).toBeNull();
    expect(split.querySelector(".composition-split")).toHaveAttribute("data-flow-split", "bay");
    expect(split.querySelector(".operate-head--plaque")).toBeTruthy();
    expect(split.querySelector(".composition-split__head")).toBeNull();
    expect(split.querySelector(".composition-split__foot")).toBeNull();
    expect(within(split).getByText("Manifest rail")).toBeInTheDocument();
    expect(within(split).getByText("Transcript")).toBeInTheDocument();
    expect(within(split).getByText("Marginalia rail")).toBeInTheDocument();
    expect(within(split).getByText("Decision bar")).toBeInTheDocument();
  });

  it("renders a typography scale without extra document headings", () => {
    renderGalleryDeck();
    const section = document.getElementById("typography");
    expect(section).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Typography" })).toBeInTheDocument();
    expect(within(section!).getByText("Campaign Registry")).toBeInTheDocument();
    expect(within(section!).getByText("The Examiner will ask follow-up questions about your work.")).toBeInTheDocument();
    expect(within(section!).getByText("CAMP-2204")).toBeInTheDocument();
    expect(within(section!).getByRole("link", { name: "frozen configuration" })).toHaveAttribute("href", "#typography");
    expect(within(section!).getByRole("link", { name: "Campaigns" })).toHaveAttribute("aria-current", "location");
    expect(section!.querySelectorAll("h1, h2, h3, h4, h5, h6")).toHaveLength(1);
    expect(section!.querySelectorAll("h2")).toHaveLength(1);
  });

  it("renders composition live specimens without code samples", { timeout: 15_000 }, () => {
    renderGalleryDeck();
    const compositionIds = [
      "composition-stack",
      "composition-inline",
      "composition-grid",
      "composition-split",
      "composition-container",
      "composition-inset",
      "composition-recipes",
    ] as const;
    for (const id of compositionIds) {
      const section = document.getElementById(id);
      expect(section).toBeTruthy();
      expect(section?.querySelector("pre, .composition-usage, .composition-guide")).toBeNull();
      expect(section?.textContent).not.toMatch(/import \{/);
    }
    expect(screen.getByRole("heading", { name: "Stack" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Inline" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Grid" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Split bay" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Container" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Inset" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Composition recipes" })).toBeInTheDocument();
    expect(document.getElementById("composition-stack")?.querySelector(".composition-stack")).toBeTruthy();
    expect(document.getElementById("composition-split")?.querySelector(".composition-split")).toBeTruthy();
    expect(document.getElementById("composition-recipes")?.querySelector(".composition-stack")).toBeTruthy();
    expect(document.querySelector(".deck-sec")).toHaveClass("composition-stack");
    expect(document.querySelector(".spec--wide")).toHaveClass("composition-stack");
    expect(document.querySelector(".spec--wide")).toHaveAttribute("data-flow-align", "stretch");
    expect(document.querySelector(".chip")).toHaveClass("composition-stack");
    expect(screen.getByRole("group", { name: "Recipe actions" })).toHaveClass("composition-inline");
    expect(screen.getByRole("group", { name: "Recipe actions" }).parentElement).toHaveAttribute(
      "data-flow-wrap",
      "false",
    );
    expect(
      screen.getByRole("button", { name: "Confirm activation after readiness checks complete" }),
    ).toHaveClass("key--truncate");
    expect(document.querySelector("#empty .spec--center")).toHaveAttribute("data-flow-align", "center");
    expect(document.getElementById("composition-grid")?.querySelector('[data-flow-min="wide"]')).toBeTruthy();
    expect(document.querySelectorAll('#composition-grid [data-flow-min="wide"] .composition-demo-tile')).toHaveLength(6);
    expect(screen.getByRole("group", { name: "Participant channels" })).toBeInTheDocument();
    const galleryCss = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../../../styles/surfaces/gallery.css"),
      "utf8",
    );
    expect(galleryCss).not.toMatch(/\.spec--wide\s*\{[^}]*align-items:\s*stretch/);
  });

  it("renders text and number field specimens in Form controls", () => {
    renderGalleryDeck();
    const form = document.getElementById("form")!;
    expect(within(form).getByRole("textbox", { name: "Callsign" })).toHaveAttribute("type", "text");
    expect(within(form).getByRole("spinbutton", { name: "Score" })).toHaveAttribute("type", "number");
    const score = within(form).getByRole("spinbutton", { name: "Score" }).closest(".field-number") as HTMLElement;
    expect(score).toBeTruthy();
    expect(within(score).getByRole("button", { name: "Increase score" })).toBeInTheDocument();
    expect(within(score).getByRole("button", { name: "Decrease score" })).toBeInTheDocument();
    const frozenScore = within(form).getByRole("spinbutton", { name: "Committed score" });
    expect(frozenScore).toHaveAttribute("readOnly");
    expect(frozenScore.closest(".field-number")).toHaveClass("is-frozen");
    expect(within(frozenScore.closest(".field-number") as HTMLElement).queryByRole("button")).not.toBeInTheDocument();
  });

  it("keeps dialog specimens free of standalone key-group open triggers", () => {
    renderGalleryDeck();
    const dialog = document.getElementById("dialog");
    expect(dialog).toBeTruthy();
    expect(within(dialog!).queryByRole("group", { name: "Open dialog specimens" })).not.toBeInTheDocument();
  });

  it("keeps the index current item on Bright Text plus a reserved teal tick", () => {
    renderGalleryDeck();
    const current = screen.getByRole("navigation", { name: "Component index" })
      .querySelector(".nav-link.is-current");
    expect(current?.querySelector(".gangway-tick")).toBeTruthy();

    const galleryCss = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../../../styles/surfaces/gallery.css"),
      "utf8",
    );
    expect(galleryCss).not.toMatch(/\.deck-index \.nav-link\.is-current \{ color: var\(--teal\)/);
    expect(galleryCss).toMatch(/\.sec-note\s*\{[^}]*max-width:\s*100ch/);
  });

  it("pins the deck index bulkhead hairline to the full sticky scrollport on desktop", () => {
    const galleryCss = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../../../styles/surfaces/gallery.css"),
      "utf8",
    );
    expect(galleryCss).toMatch(/\.deck-rail\s*\{[^}]*border-right:\s*1px solid var\(--hairline-dim\)/);
    expect(galleryCss).not.toMatch(/\.deck-rail::after/);
    expect(galleryCss).toMatch(/\.deck-rail\s*\{[^}]*min-height:\s*0/);
    expect(galleryCss).toMatch(/\.deck-rail \.nav-rail\s*\{[^}]*overflow-y:\s*auto/);
    expect(galleryCss).toMatch(
      /@media \(min-width: 901px\)[\s\S]*\.deck-rail\s*\{[^}]*min-height:\s*calc\(100dvh - var\(--gallery-header-h\) - \(2 \* var\(--gallery-deck-pad\)\)\)/,
    );
  });

  it("uses one deck hull pad so top and bottom block inset match", () => {
    const galleryCss = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../../../styles/surfaces/gallery.css"),
      "utf8",
    );
    expect(galleryCss).toMatch(/--gallery-deck-pad:\s*24px/);
    expect(galleryCss).not.toMatch(/--gallery-deck-pad-top/);
    expect(galleryCss).not.toMatch(/--gallery-deck-pad-bottom/);
    expect(galleryCss).toMatch(
      /\.layout-reference--deck \.deck\s*\{[^}]*padding:\s*var\(--gallery-deck-pad\) 20px var\(--gallery-deck-pad\) 0/,
    );
  });
});
