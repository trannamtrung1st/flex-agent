import { act, fireEvent, render, screen, within } from "@testing-library/react";
import { ITEM_LIST_LOAD_DELAY_MS } from "./sections/DataSections";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { MemoryRouter } from "react-router-dom";
import { SETUP_RESOLVED_NOTE } from "../../../design-system/components/fields/fieldFormat";
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

  it("renders Compact ID specimens with a truncated registry value", () => {
    renderGalleryDeck();
    const section = document.getElementById("compact-id")!;
    expect(within(section).getByRole("heading", { name: "Compact ID" })).toBeInTheDocument();
    expect(within(section).getByText("a1000000…000007")).toBeInTheDocument();
    expect(within(section).getByText("solo")).toBeInTheDocument();
    expect(within(section).getByText("GOV…01")).toBeInTheDocument();
  });

  it("renders a nested-scroll Item list specimen with Load more", () => {
    vi.useFakeTimers();
    try {
      renderGalleryDeck();
      const section = document.getElementById("item-list")!;
      expect(within(section).getByRole("heading", { name: "Item list" })).toBeInTheDocument();
      expect(section.querySelectorAll(".item-list-demo.frame-cut--flush")).toHaveLength(2);
      const keyed = within(section).getByRole("region", { name: "Campaigns, scrollable" });
      expect(keyed).toHaveClass("item-list-scroll");
      expect(within(keyed).getByRole("list", { name: "Campaigns" })).toBeInTheDocument();
      expect(within(keyed).getByRole("button", { name: "Open Access Review" })).toBeInTheDocument();
      expect(within(keyed).getByRole("button", { name: "Load more campaigns" })).toBeInTheDocument();
      expect(within(keyed).queryByRole("button", { name: "Open Field Observation" })).not.toBeInTheDocument();

      const loadMore = within(keyed).getByRole("button", { name: "Load more campaigns" });
      fireEvent.click(loadMore);
      expect(loadMore).toHaveAttribute("aria-busy", "true");
      expect(within(keyed).queryByRole("button", { name: "Open Field Observation" })).not.toBeInTheDocument();
      act(() => {
        vi.advanceTimersByTime(ITEM_LIST_LOAD_DELAY_MS);
      });
      expect(within(keyed).getByRole("button", { name: "Open Field Observation" })).toBeInTheDocument();

      const ended = within(section).getByRole("region", { name: "End-paged campaigns, scrollable" });
      expect(ended).toHaveClass("item-list-scroll");
      expect(within(ended).getByRole("list", { name: "End-paged campaigns" })).toBeInTheDocument();
      expect(within(ended).queryByRole("button", { name: /Load more/i })).not.toBeInTheDocument();
      expect(ended.querySelector(".item-list__end")).toBeTruthy();
    } finally {
      vi.useRealTimers();
    }
  });

  it("renders a many-column datatable specimen that names a horizontal scroll region", () => {
    renderGalleryDeck();
    const section = document.getElementById("datatable-scroll")!;
    expect(within(section).getByRole("heading", { name: "Datatable scroll" })).toBeInTheDocument();
    expect(within(section).getByRole("region", { name: "Wide registry rows, scrollable" })).toBeInTheDocument();
    expect(within(section).getByRole("columnheader", { name: "Reviewer" })).toBeInTheDocument();
    expect(within(section).getByRole("columnheader", { name: "Locale" })).toBeInTheDocument();
  });

  it("renders canonical datatable deadlines with InstantReadout time marks", () => {
    renderGalleryDeck();
    const section = document.getElementById("datatable")!;
    const region = within(section).getByRole("region", { name: "Enrollment rows, scrollable" });
    expect(within(region).getByRole("columnheader", { name: "Deadline" })).toBeInTheDocument();
    expect(within(region).getAllByRole("time").length).toBeGreaterThan(0);
  });

  it("renders Alert Note specimens including frozen-cluster provenance", () => {
    renderGalleryDeck();
    const section = document.getElementById("alert")!;
    expect(within(section).getByRole("heading", { name: "Alert" })).toBeInTheDocument();
    expect(within(section).getByText("Draft saved")).toHaveClass("advisory-copy");
    expect(within(section).getByText(SETUP_RESOLVED_NOTE)).toHaveClass("advisory-copy");
    expect(within(section).getByText(SETUP_RESOLVED_NOTE).closest(".workspace-alert")).toBeTruthy();
  });

  it("renders a FormSection specimen on the form deck", () => {
    renderGalleryDeck();
    const section = document.getElementById("form")!;
    expect(within(section).getByRole("group", { name: "Agent and Harness" })).toHaveClass("form-section");
    expect(within(section).getByRole("group", { name: "Source set" })).toHaveClass("form-section");
    expect(within(section).getByRole("group", { name: "Agent and Harness" }).nextElementSibling).toBe(
      within(section).getByRole("group", { name: "Source set" }),
    );
    expect(within(section).getByRole("group", { name: "Agent and Harness" }).parentElement).toHaveAttribute(
      "data-flow-gap",
      "6",
    );
  });

  it("renders cloneable form recipes with commission, invalid, pair, ledger, and dialog specimens", () => {
    renderGalleryDeck();
    const section = document.getElementById("form-recipes")!;
    expect(within(section).getByRole("heading", { name: "Form recipes" })).toBeInTheDocument();

    const ready = within(section).getByRole("region", { name: "Commission form recipe" });
    expect(within(ready).getByRole("heading", { name: "Create assessment Campaign" })).toBeInTheDocument();
    expect(within(ready).getByRole("textbox", { name: "Campaign title" })).toHaveValue("Structural Audit Q3");
    expect(within(ready).getByRole("group", { name: "Agent and Harness" })).toHaveClass("form-section");
    expect(within(ready).getByRole("group", { name: "Source set" })).toHaveClass("form-section");
    expect(within(ready).getByRole("group", { name: "Agent and Harness" }).parentElement).toHaveAttribute(
      "data-flow-gap",
      "6",
    );
    expect(within(ready).getByRole("button", { name: "Create" })).toHaveAttribute("type", "submit");
    expect(within(ready).queryByRole("alert")).not.toBeInTheDocument();

    const invalid = within(section).getByRole("region", { name: "Commission form recipe — invalid" });
    expect(within(invalid).getByRole("alert")).toHaveAccessibleName("Correct the following");
    expect(within(invalid).getByRole("link", { name: "Enter a Campaign title" })).toHaveAttribute(
      "href",
      "#recipeInvalidTitle",
    );
    expect(within(invalid).getByRole("textbox", { name: "Campaign title" })).toHaveAccessibleDescription(
      /Enter a Campaign title/,
    );

    const instruments = within(section).getByRole("region", { name: "Instrument form recipe" });
    expect(within(instruments).getByRole("textbox", { name: "Session limit" })).toHaveValue("60:00");
    expect(within(instruments).getByRole("textbox", { name: "Time warning at" })).toHaveValue("10:00");
    expect(within(instruments).getByRole("textbox", { name: "Adjusted rationale" })).toBeInTheDocument();
    expect(within(instruments).getByRole("group", { name: "Timing" }).parentElement).toHaveAttribute(
      "data-flow-gap",
      "6",
    );
    expect(within(instruments).getByRole("group", { name: "Timing" }).nextElementSibling).toBe(
      within(instruments).getByRole("group", { name: "Adjustment" }),
    );
    expect(within(instruments).getByRole("button", { name: "Record" })).toHaveAttribute("type", "submit");

    expect(within(section).getByRole("heading", { name: "Record accommodation" })).toBeInTheDocument();
    expect(within(section).getByRole("textbox", { name: "Time extension" })).toBeInTheDocument();
    expect(within(section).getByRole("button", { name: "Record accommodation" })).toBeInTheDocument();

    const ledger = within(section).getByRole("region", { name: "Ledger form recipe" });
    expect(within(ledger).getByRole("heading", { name: "Campaign configuration" })).toBeInTheDocument();
    const campaignIdentity = within(ledger).getByLabelText("Campaign identity");
    expect(ledger.querySelector(".frame-cut")).toContainElement(campaignIdentity);
    expect(within(ledger).getByText("a1000000…000007", { selector: "[aria-hidden]" })).toBeInTheDocument();
    expect(within(ledger).getByText("Frozen at activation")).toBeInTheDocument();
    expect(within(ledger).getByRole("group", { name: "Committed sources" })).toHaveClass("form-section");
    expect(within(ledger).getByRole("group", { name: "Score and notes" })).toHaveClass("form-section");
    expect(within(ledger).getByRole("group", { name: "Committed sources" }).parentElement).toHaveAttribute(
      "data-flow-gap",
      "6",
    );
    expect(within(ledger).getByRole("group", { name: "Timing and attempts" }).parentElement).toHaveClass(
      "composition-grid",
    );
    expect(within(ledger).getByRole("group", { name: "Timing and attempts" }).parentElement).toHaveAttribute(
      "data-flow-gap",
      "6",
    );
    expect(within(ledger).getByRole("group", { name: "Timing and attempts" }).nextElementSibling).toBe(
      within(ledger).getByRole("group", { name: "Window" }),
    );
    expect(within(ledger).getByRole("textbox", { name: "Campaign title" })).not.toHaveAttribute("readOnly");
    expect(within(ledger).getByRole("textbox", { name: "Cooldown" })).toHaveAttribute("readOnly");
    expect(within(ledger).getByRole("textbox", { name: "Cooldown" }).closest(".field-input")).toHaveClass("is-frozen");
    expect(ledger.querySelector("#recipeMixOpened")?.closest(".select-shell")).toHaveClass("is-frozen");
    expect(ledger.querySelector("#recipeMixWindow")?.closest(".select-shell")).not.toHaveClass("is-frozen");
    expect(ledger.querySelector(".select-shell.is-frozen")).toBeTruthy();
    expect(within(ledger).getByRole("textbox", { name: "Session limit" })).not.toHaveAttribute("readOnly");
    expect(within(ledger).getByRole("switch", { name: "Time warnings" })).toBeChecked();
    expect(within(ledger).getByRole("button", { name: "Save draft" })).toHaveAttribute("type", "button");
    expect(within(ledger).getByRole("button", { name: "Record" })).toHaveAttribute("type", "submit");

    fireEvent.click(within(ledger).getByRole("switch", { name: "Time warnings" }));
    expect(within(ledger).getByRole("switch", { name: "Time warnings" })).not.toBeChecked();
    fireEvent.change(within(ledger).getByRole("textbox", { name: "Campaign title" }), {
      target: { value: "Ledger title check" },
    });
    expect(within(ledger).getByRole("textbox", { name: "Campaign title" })).toHaveValue("Ledger title check");
  });

  it("shows the commission error summary after an empty-title submit", () => {
    renderGalleryDeck();
    const ready = screen.getByRole("region", { name: "Commission form recipe" });
    fireEvent.change(within(ready).getByRole("textbox", { name: "Campaign title" }), {
      target: { value: "" },
    });
    fireEvent.click(within(ready).getByRole("button", { name: "Create" }));
    expect(within(ready).getByRole("alert")).toHaveAccessibleName("Correct the following");
    expect(within(ready).getByRole("link", { name: "Enter a Campaign title" })).toHaveAttribute(
      "href",
      "#recipeCommissionTitle",
    );
  });

  it("shows work-well section labels without a leading tick specimen", () => {
    renderGalleryDeck();
    const pane = document.getElementById("pane")!;
    expect(within(pane).getByRole("heading", { name: "Assignment briefing" })).toBeInTheDocument();
    expect(within(pane).getByRole("heading", { name: "What you are completing" })).toBeInTheDocument();
    expect(within(pane).getByRole("heading", { name: "Before you begin" })).toBeInTheDocument();
    expect(pane.querySelector(".work-well__section ul")).not.toBeNull();
    expect(pane.querySelector(".work-well__section ol")).not.toBeNull();
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
    expect(paneIdx).toBeLessThan(labels.indexOf("Etched frame"));
    expect(labels.indexOf("Etched frame")).toBeLessThan(labels.indexOf("Assignment plate"));
    expect(within(index).getByRole("link", { name: "Assignment plate" })).toHaveAttribute("href", "#assignment-plate");
    expect(screen.queryByRole("group", { name: "Keys" })).not.toBeInTheDocument();
  });

  it("renders an assignment plate specimen with a trailing Open key", () => {
    renderGalleryDeck();
    const section = document.getElementById("assignment-plate")!;
    expect(within(section).getByRole("heading", { name: "Assignment plate" })).toBeInTheDocument();
    const plate = within(section).getByRole("article", { name: "Activities" });
    expect(plate).toHaveClass("assignment-plate", "frame-cut");
    expect(within(plate).getByRole("link", { name: "Open Activities" })).toHaveAttribute("href", "/activities");
    expect(within(section).getByRole("article", { name: "Campaign A" })).toHaveClass("assignment-plate--released");
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
    expect(galleryCss).toMatch(/\.layout-spec \.phase-rail-scroll,\s*\.layout-spec \.rail-scroll/);
    expect(galleryCss).toMatch(/\.layout-spec \.operate-scroll/);
    const slotStretch = galleryCss.match(
      /\[data-layout="guided-task"\] \.phase-rail-scroll > \.layout-slot,[\s\S]*?\{[^}]+\}/,
    )?.[0] ?? "";
    expect(slotStretch).toMatch(/flex:\s*none/);
    expect(slotStretch).not.toMatch(/min-height:\s*0/);
  });

  it("constrains management work-bay variants to title, description, optional back, and body", () => {
    renderGalleryDeck();

    const index = document.getElementById("layout-management-index")!;
    expect(within(index).queryByRole("navigation", { name: "Breadcrumb" })).not.toBeInTheDocument();
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
    const recordTrail = within(record).getByRole("navigation", { name: "Breadcrumb" });
    expect(within(recordTrail).getByRole("link", { name: "Campaigns" })).toHaveClass("text-link");
    expect(within(record).getByRole("heading", { level: 1, name: "Campaign Record" })).toBeInTheDocument();
    expect(within(record).getByRole("button", { name: "Campaigns" })).toBeInTheDocument();
    expect(within(record).getByText("Configuration and activation for CAMP-2204 / Shoreline Operations.")).toBeInTheDocument();
    expect(within(record).getByRole("region", { name: "Campaign configuration" })).toHaveTextContent("CAMP-2204");
    expect(within(record).getByRole("region", { name: "Campaign configuration" }).querySelector(".frame-cut")).toBeNull();
    expect(within(record).getByLabelText("Campaign record").closest(".frame-cut")).toBeNull();

    const setup = document.getElementById("layout-management-setup")!;
    expect(within(setup).getAllByRole("heading", { level: 1, name: "Setup and readiness" })).toHaveLength(2);
    expect(within(setup).queryByText("Activity")).not.toBeInTheDocument();
    const setupTrails = within(setup).getAllByRole("navigation", { name: "Breadcrumb" });
    expect(setupTrails).toHaveLength(3);
    expect(within(setupTrails[0]).getByRole("link", { name: "Activities" })).toHaveClass("text-link");
    expect(within(setupTrails[0]).getByText("Setup and readiness")).toHaveAttribute("aria-current", "page");
    expect(within(setupTrails[1]).getByText("Setup and readiness")).toHaveAttribute("aria-current", "page");
    expect(within(setupTrails[2]).getByText("Activated cohort")).toHaveAttribute("aria-current", "page");
    const setupTracks = within(setup).getAllByLabelText("Setup tracks");
    expect(setupTracks).toHaveLength(3);
    expect(within(setup).getAllByRole("group", { name: "Task and Submission requirements" })[0].parentElement).toHaveAttribute(
      "data-flow-gap",
      "6",
    );
    expect(setup.querySelector(".frame-cut")).toContainElement(setupTracks[0]);
    expect(setupTracks[0].closest(".create-ceremony__scroll")).toBeNull();
    expect(setup.querySelector(".readout-grid--columns-4")).toBeTruthy();
    expect(within(setup).getAllByRole("button", { name: "Save draft" })[0].closest(".create-ceremony__scroll")).toBeNull();
    expect(within(setup).getAllByRole("button", { name: "Check readiness" })).toHaveLength(2);
    expect(within(setup).queryByRole("button", { name: "Activate cohort" })).not.toBeInTheDocument();
    expect(within(setup).getByRole("heading", { name: "Readiness blocked" })).toBeInTheDocument();
    const timezone = within(setup).getAllByRole("textbox", { name: "Timezone" })[1];
    expect(within(setup).getByRole("link", { name: "Set a valid session window." })).toHaveAttribute(
      "href",
      `#${timezone.getAttribute("id")}`,
    );
    const resolvedNotes = within(setup).getAllByText(SETUP_RESOLVED_NOTE);
    expect(resolvedNotes).toHaveLength(3);
    expect(resolvedNotes[0]).toHaveClass("advisory-copy");
    expect(resolvedNotes[0].closest(".workspace-alert-body")).toBeNull();
    expect(resolvedNotes[1].closest(".workspace-alert-body")).toBeNull();
    expect(resolvedNotes[2].closest(".workspace-alert-body")).toBeTruthy();
    expect(within(setup).getByRole("heading", { level: 1, name: "Activated cohort" })).toBeInTheDocument();
    expect(within(setup).getByRole("link", { name: "Assign Participants" })).toBeInTheDocument();
    expect(within(setup).getByRole("link", { name: "Assign Participants" }).closest(".create-ceremony__scroll")).toBeNull();
    for (const memory of within(setup).getAllByRole("textbox", { name: "Memory" })) {
      expect(memory).not.toHaveAccessibleDescription(SETUP_RESOLVED_NOTE);
    }

    const empty = document.getElementById("layout-management-empty")!;
    expect(within(empty).queryByRole("navigation", { name: "Breadcrumb" })).not.toBeInTheDocument();
    expect(within(empty).getByRole("heading", { level: 1, name: "Campaign Registry" })).toBeInTheDocument();
    expect(within(empty).getByText("No campaigns listed")).toBeInTheDocument();
    expect(within(empty).queryByRole("button", { name: "Campaigns" })).not.toBeInTheDocument();

    const ceremony = document.getElementById("layout-management-ceremony")!;
    expect(within(ceremony).queryByRole("navigation", { name: "Breadcrumb" })).not.toBeInTheDocument();
    expect(within(ceremony).getByRole("heading", { level: 1, name: "This destination is not available" })).toBeInTheDocument();
    expect(ceremony.querySelector(".operate-column--hug")).toHaveAttribute("data-hug-measure", "auto");
    expect(within(ceremony).getByText("The current authorized relationship cannot use this locator.")).toBeInTheDocument();
    expect(within(ceremony).getAllByRole("link", { name: "Return to Home" })).toHaveLength(2);
    expect(within(ceremony).getAllByRole("link", { name: "Return to Home" })[0]).toHaveAttribute("href", "/shared/gallery");
    expect(within(ceremony).getAllByRole("link", { name: "Return to Home" })[0]).toHaveClass("key--quiet");
    expect(within(ceremony).getAllByRole("link", { name: "Return to Home" })[0]).not.toHaveClass("key--open");
    const denied = within(ceremony).getByRole("heading", { level: 1, name: "Access denied" });
    expect(denied.closest(".workspace-area")).toHaveClass("workspace-area--danger");
    expect(within(ceremony).getByText("My work is not available for the current authorized relationship.")).toBeInTheDocument();
    const authCommit = within(ceremony).getByRole("button", { name: "Continue to sign in" });
    expect(authCommit).toHaveClass("key", "key--transmit", "key--large");
    expect(authCommit).not.toHaveClass("key--open");
    expect(within(ceremony).getByRole("heading", { level: 1, name: "Sign-in could not be completed" }).closest(".workspace-area")).toHaveClass(
      "workspace-area--danger",
    );

    const loading = document.getElementById("layout-management-loading")!;
    expect(within(loading).getByRole("heading", { level: 1, name: "Establishing session" })).toBeInTheDocument();
    expect(loading.querySelector(".operate-column--hug")).toHaveAttribute("data-hug-measure", "auto");
    const wait = within(loading).getByRole("status");
    expect(wait).toHaveClass("wait-plate", "wait-plate--inset", "ceremony-wait");
    expect(within(loading).getByText("Establishing session context…")).toBeVisible();
    expect(wait.querySelector(".scan-track.is-waiting")).toBeTruthy();

    const split = document.getElementById("layout-management-split")!;
    expect(within(split).getByRole("heading", { level: 1, name: "Evaluation Record" })).toBeInTheDocument();
    expect(within(split).getByRole("button", { name: "Queue" })).toBeInTheDocument();
    expect(split.querySelector(".layout-management__main > .composition-inset")).toBeNull();
    expect(split.querySelector(".composition-split")).toHaveAttribute("data-flow-split", "bay");
    expect(split.querySelector(".operate-head--plaque")).toBeTruthy();
    expect(split.querySelector(".record-view > .operate-scroll")).toBeTruthy();
    expect(split.querySelector(".composition-split__head")).toBeNull();
    expect(split.querySelector(".composition-split__foot")).toBeNull();
    expect(within(split).getByText("Manifest rail")).toBeInTheDocument();
    expect(within(split).getByText("Transcript")).toBeInTheDocument();
    expect(within(split).getByText("Marginalia rail")).toBeInTheDocument();
    expect(within(split).getByText("Decision bar")).toBeInTheDocument();
  });

  it("seats page-level wait as a hug ceremony wait-plate beside the inline wait panel", () => {
    renderGalleryDeck();
    const waitPanel = document.getElementById("wait-panel")!;
    expect(within(waitPanel).getByText("Loading activities…").closest(".loading-panel")).toHaveAttribute("role", "status");
    const waitPlate = within(waitPanel).getByText("Establishing session context…").closest("[role='status']");
    expect(waitPlate).toHaveClass("wait-plate", "wait-plate--inset", "ceremony-wait");
    expect(waitPanel.querySelector(".operate-column--hug")).toHaveAttribute("data-hug-measure", "auto");
    expect(waitPlate?.closest(".work-plane--ceremony")).toBeTruthy();
  });

  it("renders shared BreadcrumbNav index and nested trails", () => {
    renderGalleryDeck();
    const section = document.getElementById("breadcrumbs")!;
    expect(within(section).getByRole("heading", { name: "Breadcrumbs" })).toBeInTheDocument();
    const trails = within(section).getAllByRole("navigation", { name: "Breadcrumb" });
    expect(trails).toHaveLength(3);
    expect(within(trails[0]!).getByText("My work")).toHaveAttribute("aria-current", "page");
    expect(within(trails[1]!).getByRole("link", { name: "Activities" })).toHaveAttribute("href", "/activities");
    expect(within(trails[1]!).getByText("Setup and readiness")).toHaveAttribute("aria-current", "page");
    expect(within(trails[2]!).getByRole("link", { name: "Setup and readiness" })).toHaveAttribute(
      "href",
      "/activities/act-1/setup",
    );
    expect(within(trails[2]!).getByText("Participants")).toHaveAttribute("aria-current", "page");
    expect(trails[0]!.querySelector("a")?.className).toMatch(/text-link/);
  });

  it("catalogs hull, phosphor, amber, and fault-phosphor danger tokens", () => {
    renderGalleryDeck();
    const section = document.getElementById("colors")!;
    expect(within(section).getByRole("heading", { name: "Colors" })).toBeInTheDocument();
    const names = Array.from(section.querySelectorAll(".chip-name")).map((el) => el.textContent);
    expect(names).toEqual(expect.arrayContaining(["danger", "danger-bright", "danger-glow", "success"]));
    expect(within(section).getByText("#f05c58")).toBeInTheDocument();
    expect(within(section).getByText("#ff7468")).toBeInTheDocument();
    expect(within(section).getByText("rgba(240, 92, 88, 0.32)")).toBeInTheDocument();
    expect(within(section).getByText("#53d28a")).toBeInTheDocument();
    expect(within(section).getByText("Campaign registry")).toHaveClass("type-placard", "type-placard--system");
    expect(within(section).getByText("Confirm activation")).toHaveClass("type-placard", "type-placard--attention");
    expect(within(section).getByText("Access denied")).toHaveClass("type-placard", "type-placard--danger");
    expect(within(section).getByText("Approved")).toHaveClass("type-placard", "type-placard--success");
    expect(within(section).getByText("Sign-in could not be completed")).toHaveClass("type-placard", "type-placard--danger");
    expect(within(section).getByText("Your access changed")).toHaveClass("type-placard", "type-placard--danger");
    const galleryCss = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../../../styles/surfaces/gallery.css"),
      "utf8",
    );
    expect(galleryCss).toMatch(/#colors \.spec-row--voices \.spec\[data-flow-gap="2\.5"\]/);
    expect(galleryCss).toMatch(/#colors \.type-placard \{[^}]*padding-block:\s*12px 18px/);
    expect(galleryCss).not.toMatch(/^\.type-placard \{[^}]*padding-block:\s*12px 18px/m);
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
    const splitBaySection = document.getElementById("composition-split")!;
    expect(splitBaySection.querySelectorAll(".composition-split-demo")).toHaveLength(2);
    expect(splitBaySection.querySelector(".composition-split-demo .layout-slot")).toBeTruthy();
    expect(splitBaySection.querySelector(".composition-split-demo .composition-demo-tile")).toBeNull();
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
    expect(document.getElementById("composition-grid")?.querySelector('[data-flow-fit="fill"]')).toHaveAttribute(
      "data-flow-min",
      "control",
    );
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
    const stackedTitles = within(form).getAllByRole("textbox", { name: "Campaign title" });
    expect(stackedTitles).toHaveLength(2);
    expect(stackedTitles[0].closest(".field-stack")).toBeTruthy();
    expect(stackedTitles[0]).not.toHaveClass("is-frozen");
    expect(stackedTitles[1]).toHaveClass("is-frozen");
    expect(stackedTitles[1].closest(".field-stack")).toBeTruthy();
    expect(within(form).getByRole("spinbutton", { name: "Adjusted score" }).closest(".field-stack")).toBeTruthy();
  });

  it("renders File intake specimens for multiple, single, disabled, and invalid", () => {
    renderGalleryDeck();
    const file = document.getElementById("file")!;
    expect(within(file).getByRole("heading", { name: "File intake" })).toBeInTheDocument();
    expect(within(file).getByRole("list", { name: "Selected files" })).toHaveTextContent("briefing.md");
    expect(within(file).getByRole("group", { name: "Briefing file" })).toBeInTheDocument();
    expect(within(file).getByRole("group", { name: "Locked intake" })).toHaveAttribute("aria-disabled", "true");
    expect(within(file).getByRole("group", { name: "Required attachment" })).toHaveAttribute("aria-invalid", "true");
    const chooseMany = within(file).getAllByRole("button", { name: "Choose files" });
    expect(chooseMany.some((button) => button.hasAttribute("disabled"))).toBe(true);
    expect(chooseMany.some((button) => !button.hasAttribute("disabled"))).toBe(true);
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
    const navigationCss = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../../../styles/components/navigation.css"),
      "utf8",
    );
    expect(navigationCss).toMatch(/summary\.gangway-section-label \{[^}]*cursor:\s*pointer/);
    expect(galleryCss).not.toMatch(/summary\.gangway-section-label \{ cursor: default; \}/);
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
