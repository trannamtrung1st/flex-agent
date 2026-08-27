import { useState } from "react";
import {
  BackKey,
  Bulkhead,
  CommandStrip,
  ConsoleFoot,
  DropdownSelect,
  FieldInput,
  FormField,
  Gangway,
  Key,
  OperateHead,
  PARTICIPANT_IDENTITY,
  ReadoutList,
  prototypeAccountActions,
  type CommandStripNavItem,
  type GangwayGroup,
} from "../../../components";
import { PanelTabs } from "../PanelTabs";
import { GallerySection, Spec } from "./GallerySection";

const gangwayGroups: GangwayGroup[] = [{
  label: "Assessment operations",
  items: [
    { to: "/shared/gallery#gangway", label: "Campaigns", abbr: "CAM" },
    { to: "/shared/gallery#gangway", label: "Enrollments", abbr: "ENR", current: true },
  ],
}];

const stripRoleNav: CommandStripNavItem[] = [
  { to: "/shared/gallery#strip", label: "Home", current: true },
  { to: "/shared/gallery#strip", label: "Assignments", inactive: true },
  { to: "/shared/gallery#strip", label: "Results", inactive: true },
];

export function NavigationSections() {
  const [collapsed, setCollapsed] = useState(false);
  const [drawer, setDrawer] = useState<"leading" | "trailing" | "form" | null>(null);
  const [footerState, setFooterState] = useState("Populated roster");

  return (
    <>
      <GallerySection id="strip" title="Command strip" note="Full-width top chrome: brand placard, product navigation with the teal underline current marker, and the ident cluster (mode placard · operator profile). Above 720px the strip stays one row with a full-width underline inset 22px from each token edge. At ≤720px the nav row drops below with a hairline top and a fixed 42px underline bar.">
        <Spec wide tag=".strip-ident groups mode · .strip-profile">
          <div className="strip-demo"><CommandStrip mode="Review" readout="REV-2204-07" /></div>
        </Spec>
        <Spec wide tag=">720px row specimen · brand · nav · ident cluster with full-width underline">
          <div className="strip-demo strip-demo--wide">
            <CommandStrip
              nav={stripRoleNav}
              profile={PARTICIPANT_IDENTITY}
              actions={prototypeAccountActions(() => undefined)}
            />
          </div>
        </Spec>
        <Spec wide tag="≤720px wrap specimen · brand + ident row, nav scrolls below with 42px underline">
          <div className="strip-demo strip-demo--narrow">
            <CommandStrip nav={stripRoleNav} readout="CND-8842-19" />
          </div>
        </Spec>
      </GallerySection>

      <GallerySection id="nav-rail" title="Nav rail" note="Shared side navigation in the listbox selected grammar — teal tick and Bright Text on the current item, no popover plate. Teal selection voice only; navigation is never amber.">
        <div className="spec-row">
          <Spec tag=".nav-rail > .nav-list > .nav-link">
            <nav className="nav-rail nav-demo" aria-label="Nav rail specimen">
              <span className="nav-rail-label">Console</span>
              <ul className="nav-list">
                {["Home", "Assignments", "Examination", "Results"].map((label, index) => (
                  <li key={label}><a className={`nav-link${index === 0 ? " is-current" : ""}`} href="#nav-rail" aria-current={index === 0 ? "location" : undefined}>{label}</a></li>
                ))}
              </ul>
            </nav>
          </Spec>
        </div>
        <Spec wide tag="OperateHead · BackKey · shared page heading and visible return control">
          <OperateHead
            title="Campaign Record"
            description="Configuration, activation state, and enrollment context."
            back={<BackKey label="Campaigns" onClick={() => undefined} />}
          />
        </Spec>
      </GallerySection>

      <GallerySection id="gangway" title="Gangway side menu" note="Persistent collapsible side menu for shell layouts — the in-layout counterpart to the bulkhead drawer. Width rides --gangway-w (232px); the toggle folds it to a 76px rail of engraved channel codes.">
        <Spec wide tag=".gangway · head / body / foot · .gangway-abbr · .is-collapsed — click the chevron key to fold">
          <div className="gangway-demo">
            <Gangway
              title="Administrator"
              groups={gangwayGroups}
              collapsed={collapsed}
              onCollapsedChange={setCollapsed}
              ariaLabel="Gangway specimen"
              footer={<ReadoutList rows={[{ term: "Operator", value: "ADM-7X92-19" }]} />}
            />
            <div className="gangway-demo-canvas"><span className="gangway-demo-note">Shell content region — the gangway is a grid track; the fold reflows this column.</span></div>
          </div>
        </Spec>
      </GallerySection>

      <GallerySection id="drawer" title="Bulkhead drawer" note="Smoked-glass off-canvas panel over an 82% ground scrim. Leading slides from the left; trailing slides from the right. Escape, scrim click, and Close dismiss; focus returns to the trigger.">
        <div className="spec-row">
          <Spec tag=".bulkhead--leading"><Key onClick={() => setDrawer("leading")}>Open left drawer</Key></Spec>
          <Spec tag=".bulkhead--trailing"><Key onClick={() => setDrawer("trailing")}>Open right drawer</Key></Spec>
          <Spec tag=".bulkhead--trailing.bulkhead--wide · .form-row inside"><Key onClick={() => setDrawer("form")}>Open form drawer</Key></Spec>
        </div>
        <Bulkhead id="demoBulkheadLeading" open={drawer === "leading"} onClose={() => setDrawer(null)} title="Navigation" titleId="bulkheadLeadingTitle">
          <nav className="nav-rail" aria-label="Left drawer navigation"><ul className="nav-list"><li><a className="nav-link" href="#colors" onClick={() => setDrawer(null)}>Colors</a></li><li><a className="nav-link" href="#dialog" onClick={() => setDrawer(null)}>Dialog</a></li></ul></nav>
        </Bulkhead>
        <Bulkhead id="demoBulkheadTrailing" open={drawer === "trailing"} onClose={() => setDrawer(null)} side="trailing" title="Marginalia" titleId="bulkheadTrailingTitle" footer={<Key size="compact" onClick={() => setDrawer(null)}>Dismiss</Key>}>
          <ReadoutList rows={[{ term: "Criterion", value: "Evidence linkage" }, { term: "Score", value: "3.5 / 4.0" }, { term: "Confidence", value: "0.82" }]} />
        </Bulkhead>
        <Bulkhead id="demoBulkheadForm" open={drawer === "form"} onClose={() => setDrawer(null)} side="trailing" wide title="Adjust Criterion" titleId="bulkheadFormTitle" footer={<Key size="compact" onClick={() => setDrawer(null)}>Save adjustment</Key>}>
          <FormField id="bkScore" label="Revised score">
            {(controlProps) => <FieldInput {...controlProps} width="narrow" defaultValue="3.5" />}
          </FormField>
        </Bulkhead>
      </GallerySection>

      <GallerySection id="tabs" title="Panel tabs" note="In-page tab set — not the strip's campaign tabs. Current token seats the 2px teal underline; panels switch with a 200ms opacity ease. Arrow, Home, and End keys move focus and automatically select the focused tab.">
        <Spec wide tag=".panel-tabs · .panel-tablist · .panel-tab · .panel-panel">
          <PanelTabs label="Record panels specimen" tabs={[
            { id: "manifest", label: "Manifest", panel: "Enrollment manifest — sortable sticky head, instrument marks in the session-state column, teal tick on row hover." },
            { id: "transcript", label: "Transcript", panel: "Sealed transcript ledger — channel cards at 88% width, agent left with amber wash, participant right with teal glass." },
            { id: "evaluation", label: "Evaluation", panel: "Criterion marginalia — score, rationale, confidence readout, dashed Agent original beneath human revision." },
          ]} />
        </Spec>
      </GallerySection>

      <GallerySection id="footer" title="Console footer" note="Quiet page foot: hairline top, dim synthetic-content note left, optional readout or quiet keys right. Stacks at ≤720px.">
        <Spec wide tag=".console-foot · .console-foot-actions · .foot-note">
          <ConsoleFoot note="Synthetic demonstration content — no real participant data.">
            <div className="console-foot-actions">
              <span className="console-foot-readout">Protocol V7.3.1</span>
              <div className="demo-plate">
                <span className="demo-label" id="footerDemoStateLabel">Demo state</span>
                <DropdownSelect id="footerDemoState" labelId="footerDemoStateLabel" value={footerState} options={["Populated roster", "Empty roster"]} onChange={setFooterState} />
              </div>
            </div>
          </ConsoleFoot>
        </Spec>
      </GallerySection>
    </>
  );
}
