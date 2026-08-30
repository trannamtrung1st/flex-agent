import { fireEvent, render, screen } from "@testing-library/react";
import { type ReactElement } from "react";
import { MemoryRouter } from "react-router-dom";
import {
  APPROVED_LAYOUT_IDS,
  PRODUCTION_LAYOUT_IDS,
  GuidedTaskFoot,
  GuidedTaskLayout,
  LayoutAssignment,
  LiveSessionLayout,
  ManagementLayout,
  ReferenceLayout,
  isProductionLayoutId,
} from "./index";

function wrap(ui: ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

describe("shared layout library", () => {
  it("exposes four approved families and excludes reference from production ids", () => {
    expect([...APPROVED_LAYOUT_IDS]).toEqual(["management", "guided-task", "live-session", "reference"]);
    expect([...PRODUCTION_LAYOUT_IDS]).toEqual(["management", "guided-task", "live-session"]);
    expect(isProductionLayoutId("reference")).toBe(false);
  });

  it("renders management landmarks, skip target, and optional gangway", () => {
    wrap(
      <ManagementLayout
        commandStrip={{ homeTo: "/", homeLabel: "Home", brandSuffix: "Ops" }}
        navigation={{
          title: "Administrator",
          groups: [{ label: "Areas", items: [{ to: "/", label: "Home", abbr: "HOM", current: true }] }],
          currentLabel: "Home",
        }}
        footerNote="Quiet footer"
      >
        <p>Work bay</p>
      </ManagementLayout>,
    );
    expect(document.querySelector('[data-layout="management"]')).toBeTruthy();
    expect(screen.getByRole("link", { name: "Skip to main content" })).toHaveAttribute("href", "#main-content");
    expect(document.querySelector("#main-content")).toBeTruthy();
    expect(screen.getByRole("navigation", { name: "Primary navigation" })).toBeInTheDocument();
    expect(screen.getByText("Work bay")).toBeInTheDocument();
    expect(screen.getByText("Quiet footer")).toBeInTheDocument();
    const main = document.querySelector("#main-content");
    const well = main?.querySelector(".composition-inset");
    expect(well).toHaveClass("composition-inset--shell-main");
    expect(well).toHaveAttribute("data-flow-space", "none");
    expect(well).toHaveAttribute("data-flow-inline", "5.5");
    expect(well).toHaveAttribute("data-flow-block", "4");
    expect(well).toHaveTextContent("Work bay");
    expect(document.querySelector(".strip-brand")).toHaveClass("strip-brand--origin");
    expect(document.querySelector(".strip-brand .strip-mode")).toHaveTextContent("Ops");
  });

  it("lets management skip the main content inset", () => {
    wrap(
      <ManagementLayout contain={false} commandStrip={{ homeTo: "/", homeLabel: "Home" }}>
        <p>Full bay</p>
      </ManagementLayout>,
    );
    const main = document.querySelector("#main-content");
    expect(main?.querySelector(".composition-inset")).toBeNull();
    expect(main).toHaveTextContent("Full bay");
    expect(document.querySelector(".strip-brand")).not.toHaveClass("strip-brand--origin");
  });

  it("keeps guided-task rail brand outside the scroller", () => {
    wrap(
      <GuidedTaskLayout
        railLabel="Assignment phases"
        brandSuffix="Assignment Station"
        brandExtras={<span>Brand extra</span>}
        instruments={<p>Instrument body</p>}
        heading={<p>Heading</p>}
        actions={<GuidedTaskFoot arrangement="end"><button type="button">Continue</button></GuidedTaskFoot>}
      >
        <p>Well</p>
      </GuidedTaskLayout>,
    );
    const root = document.querySelector('[data-layout="guided-task"]');
    expect(root).toBeTruthy();
    const rail = screen.getByRole("complementary", { name: "Assignment phases" });
    const scroller = rail.querySelector(".phase-rail-scroll");
    expect(scroller).toBeTruthy();
    expect(rail.textContent).toContain("Assignment Station");
    expect(scroller?.textContent).toContain("Instrument body");
    expect(scroller?.textContent).not.toContain("Assignment Station");
    expect(document.querySelector("#main-content")).toBeTruthy();
    expect(document.querySelector("#main-content")?.querySelector(".composition-inset")).toBeNull();
    expect(screen.getByRole("button", { name: "Continue" }).closest(".layout-guided__actions")).toHaveAttribute("data-arrangement", "end");
  });

  it("renders guided-task split feet with secondary and primary slots", () => {
    wrap(
      <GuidedTaskLayout
        railLabel="Assignment phases"
        brandSuffix="Assignment Station"
        instruments={<p>Instrument body</p>}
        heading={<p>Heading</p>}
        actions={(
          <GuidedTaskFoot
            arrangement="split"
            secondary={<button type="button">Cancel</button>}
            primary={<button type="button">Save</button>}
          />
        )}
      >
        <p>Well</p>
      </GuidedTaskLayout>,
    );
    const foot = document.querySelector(".layout-guided__actions");
    expect(foot).toHaveAttribute("data-arrangement", "split");
    expect(foot?.querySelector(".plate-foot-slot--secondary")).toHaveTextContent("Cancel");
    expect(foot?.querySelector(".plate-foot-slot--primary")).toHaveTextContent("Save");
  });

  it("can wrap a guided-task well in the content inset", () => {
    wrap(
      <GuidedTaskLayout
        contain
        railLabel="Assignment phases"
        brandSuffix="Assignment Station"
        instruments={<p>Instrument body</p>}
        heading={<p>Heading</p>}
      >
        <p>Contained well</p>
      </GuidedTaskLayout>,
    );
    const main = document.querySelector("#main-content");
    expect(main?.querySelector(".composition-inset")).toHaveTextContent("Contained well");
  });

  it("keeps live-session rail brand outside the scroller", () => {
    wrap(
      <LiveSessionLayout
        railLabel="Session instruments"
        brandSuffix="Examination Console"
        brandExtras={<span>Brand extra</span>}
        instruments={<p>Instrument body</p>}
        composer={<p>Composer slot</p>}
        examiner={<p>Examiner slot</p>}
      >
        <p>Transcript slot</p>
      </LiveSessionLayout>,
    );
    const rail = screen.getByRole("complementary", { name: "Session instruments" });
    const scroller = rail.querySelector(".rail-scroll");
    expect(scroller).toBeTruthy();
    expect(rail.textContent).toContain("Examination Console");
    expect(rail.textContent).toContain("Brand extra");
    expect(scroller?.textContent).toContain("Instrument body");
    expect(scroller?.textContent).not.toContain("Examination Console");
    expect(scroller?.textContent).not.toContain("Brand extra");
  });

  it("places live-session rail, transcript, composer, and examiner", () => {
    wrap(
      <LiveSessionLayout
        railLabel="Session instruments"
        brandSuffix="Examination Console"
        instruments={<p>Feed</p>}
        composer={<p>Composer slot</p>}
        examiner={<p>Examiner slot</p>}
      >
        <p>Transcript slot</p>
      </LiveSessionLayout>,
    );
    expect(document.querySelector('[data-layout="live-session"]')).toBeTruthy();
    expect(screen.getByRole("complementary", { name: "Session instruments" })).toBeInTheDocument();
    expect(screen.getByRole("main", { name: "Examination transcript" })).toHaveTextContent("Transcript slot");
    expect(screen.getByText("Composer slot")).toBeInTheDocument();
    expect(screen.getByRole("complementary", { name: "Examiner station" })).toHaveTextContent("Examiner slot");
    expect(screen.getByRole("main", { name: "Examination transcript" }).querySelector(".composition-inset")).toBeNull();
  });

  it("renders reference catalog without production-only regions", () => {
    wrap(
      <ReferenceLayout
        commandStrip={{ homeTo: "/surfaces", homeLabel: "Channel index" }}
        footerNote="Catalog foot"
      >
        <p>Catalog body</p>
      </ReferenceLayout>,
    );
    expect(document.querySelector('[data-layout="reference"]')).toBeTruthy();
    expect(screen.queryByRole("navigation", { name: "Administrator areas" })).not.toBeInTheDocument();
    expect(screen.getByText("Catalog body")).toBeInTheDocument();
    expect(document.querySelector("#main-content")?.querySelector(".composition-inset")).toHaveTextContent("Catalog body");
  });

  it("renders the reference deck variant with an index rail", () => {
    wrap(
      <ReferenceLayout
        commandStrip={{ homeTo: "/surfaces", homeLabel: "Channel index" }}
        index={{ groups: [{ id: "foundations", label: "Foundations", items: [{ id: "color", label: "Color" }] }] }}
      >
        <p>Deck body</p>
      </ReferenceLayout>,
    );
    expect(document.querySelector('[data-layout="reference"]')).toHaveClass("layout-reference--deck");
    expect(screen.getByRole("navigation", { name: "Component index" })).toBeInTheDocument();
    expect(document.querySelector("#main-content")?.querySelector(".composition-inset")).toBeNull();
  });

  it("keeps skip link, banner, gangway, and a single main in document order", () => {
    wrap(
      <ManagementLayout
        commandStrip={{ homeTo: "/", homeLabel: "Home", brandSuffix: "Ops" }}
        navigation={{
          title: "Administrator",
          groups: [{ label: "Areas", items: [{ to: "/", label: "Home", abbr: "HOM", current: true }] }],
          currentLabel: "Home",
        }}
      >
        <p>Work bay</p>
      </ManagementLayout>,
    );
    const skip = screen.getByRole("link", { name: "Skip to main content" });
    const banner = document.querySelector("header.command-strip");
    const nav = screen.getByRole("navigation", { name: "Primary navigation" });
    const mains = screen.getAllByRole("main");
    expect(mains).toHaveLength(1);
    expect(skip.compareDocumentPosition(banner!)).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
    expect(banner!.compareDocumentPosition(nav)).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
    expect(nav.compareDocumentPosition(mains[0])).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
  });

  it("owns gangway collapse on the layout, not the page", () => {
    wrap(
      <ManagementLayout
        commandStrip={{ homeTo: "/", homeLabel: "Home" }}
        navigation={{
          title: "Administrator",
          groups: [{ label: "Areas", items: [{ to: "/", label: "Home", abbr: "HOM", current: true }] }],
          currentLabel: "Home",
        }}
      >
        <p>Work bay</p>
      </ManagementLayout>,
    );
    const nav = screen.getByRole("navigation", { name: "Primary navigation" });
    expect(nav).not.toHaveClass("is-collapsed");
    fireEvent.click(screen.getByRole("button", { name: "Collapse menu" }));
    expect(nav).toHaveClass("is-collapsed");
  });

  it("owns the 1080px bulkhead instead of the gangway", () => {
    const previous = window.matchMedia;
    window.matchMedia = (query: string) => ({
      matches: query.includes("1080"),
      media: query,
      onchange: null,
      addListener: () => undefined,
      removeListener: () => undefined,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      dispatchEvent: () => false,
    });
    try {
      wrap(
        <ManagementLayout
          commandStrip={{ homeTo: "/", homeLabel: "Home" }}
          navigation={{
            title: "Administrator",
            groups: [{ label: "Areas", items: [{ to: "/", label: "Home", abbr: "HOM", current: true }] }],
            currentLabel: "Campaigns",
            bulkheadId: "layoutNavBulkhead",
          }}
        >
          <p>Work bay</p>
        </ManagementLayout>,
      );
      expect(screen.queryByRole("navigation", { name: "Primary navigation" })).not.toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Menu" })).toHaveAttribute("aria-controls", "layoutNavBulkhead");
    } finally {
      window.matchMedia = previous;
    }
  });

  it("omits skip link, main landmark, and skip target when nested in a specimen", () => {
    wrap(
      <ManagementLayout nested commandStrip={{ homeTo: "/", homeLabel: "Home" }}>
        <p>Specimen bay</p>
      </ManagementLayout>,
    );
    expect(screen.queryByRole("link", { name: "Skip to main content" })).not.toBeInTheDocument();
    expect(screen.queryByRole("main")).not.toBeInTheDocument();
    expect(document.querySelector("#main-content")).toBeNull();
    expect(document.querySelector(".layout-management__main")).toHaveTextContent("Specimen bay");
  });

  it("keeps structural wrappers when slots are empty-ish", () => {
    wrap(
      <ManagementLayout commandStrip={{ homeTo: "/", homeLabel: "Home" }}>
        <p>Only content</p>
      </ManagementLayout>,
    );
    expect(document.querySelector(".layout-management__main")).toBeTruthy();
    expect(document.querySelector("header.command-strip")).toBeTruthy();
  });

  it("rejects a layout family that does not match the assigned route", () => {
    expect(() => wrap(
      <LayoutAssignment id="reference">
        <ManagementLayout commandStrip={{ homeTo: "/", homeLabel: "Home" }}>
          <p>Wrong family</p>
        </ManagementLayout>
      </LayoutAssignment>,
    )).toThrow(/Rendered layout 'management' where 'reference' is assigned/);
  });
});
