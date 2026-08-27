import type { ReactNode } from "react";
import {
  BackKey,
  GuidedTaskLayout,
  LayoutAssignment,
  LiveSessionLayout,
  ManagementLayout,
  OperateArea,
  ReadoutGrid,
  ReadoutGridField,
  ReadoutGridRow,
  ReadoutList,
  SplitBay,
  type ManagementNavigation,
} from "../../../../design-system";
import { ReferenceLayout } from "../../../../design-system/lab";
import { GallerySection, Spec } from "./GallerySection";

function LayoutSlot({
  label,
  variant = "bay",
}: {
  label: string;
  variant?: "bay" | "rail" | "heading" | "foot" | "examiner";
}) {
  return (
    <div className={`layout-slot layout-slot--${variant}`}>
      <span className="layout-slot__name">{label}</span>
    </div>
  );
}

function managementNav(currentLabel: string, bulkheadId: string): ManagementNavigation {
  return {
    title: "Administrator",
    currentLabel,
    bulkheadId,
    groups: [
      {
        label: "Assessment operations",
        items: [
          {
            to: "/shared/gallery#layout-management-index",
            label: "Campaigns",
            abbr: "CAM",
            current: currentLabel === "Campaigns",
          },
          {
            to: "/shared/gallery#layout-management-empty",
            label: "Enrollments",
            abbr: "ENR",
          },
        ],
      },
    ],
  };
}

function ManagementPageSpec({
  tag,
  bulkheadId,
  currentLabel = "Campaigns",
  contain,
  children,
}: {
  tag: string;
  bulkheadId: string;
  currentLabel?: string;
  contain?: boolean;
  children: ReactNode;
}) {
  return (
    <Spec wide tag={tag}>
      <div className="layout-spec">
        <LayoutAssignment id="management">
          <ManagementLayout
            nested
            contain={contain}
            commandStrip={{ homeTo: "/shared/gallery", homeLabel: "Channel index", brandSuffix: "Specimen" }}
            navigation={managementNav(currentLabel, bulkheadId)}
            footerNote="Quiet footer"
          >
            {children}
          </ManagementLayout>
        </LayoutAssignment>
      </div>
    </Spec>
  );
}

export function LayoutSections() {
  return (
    <>
      <GallerySection
        id="layout-management"
        title="Management layout"
        note="Command strip, optional gangway, main work bay, quiet footer. Production and lab management consoles must use this family. Work-bay content is an OperateArea, not a custom heading stack. Main defaults to shell Inset (22px inline / 16px block, --shell-main-inset-*); pass contain={false} for flush bays."
      >
        <div className="spec-row spec-row--layout-contain">
          <ManagementPageSpec
            contain
            tag='contain={true} · shell inset (--shell-main-inset-*)'
            bulkheadId="layoutSpecContainNav"
          >
            <LayoutSlot label="Main work bay" />
          </ManagementPageSpec>
          <ManagementPageSpec
            contain={false}
            tag='contain={false} · flush bay'
            bulkheadId="layoutSpecFlushNav"
          >
            <LayoutSlot label="Main work bay" />
          </ManagementPageSpec>
        </div>
      </GallerySection>
      <GallerySection
        id="layout-management-index"
        title="Management index"
        note="Console list or registry. OperateArea supplies the page title and description. Omit BackKey — the gangway (or command-strip home) is the return path."
      >
        <ManagementPageSpec tag="OperateArea · title · description · registry body · no back" bulkheadId="layoutSpecIndexNav">
          <OperateArea
            className="workspace-area"
            label="Campaign registry"
            title="Campaign Registry"
            description="Find a campaign, then open its record to inspect or configure."
          >
            <ReadoutList
              label="Listed campaigns"
              rows={[
                { term: "CAMP-2204", value: "Shoreline Operations" },
                { term: "CAMP-2205", value: "Harbor Watch" },
              ]}
            />
          </OperateArea>
        </ManagementPageSpec>
      </GallerySection>
      <GallerySection
        id="layout-management-record"
        title="Management record"
        note="Nested record under a console index. BackKey returns to that index. Keep the same OperateArea title/description/body stack; do not invent a second page head."
      >
        <ManagementPageSpec tag="OperateArea · BackKey · title · description · record body" bulkheadId="layoutSpecRecordNav">
          <OperateArea
            className="workspace-area"
            label="Campaign configuration"
            title="Campaign Record"
            description="Configuration and activation for CAMP-2204 / Shoreline Operations."
            back={<BackKey label="Campaigns" onClick={() => undefined} />}
          >
            <ReadoutGrid label="Campaign record">
              <ReadoutGridRow label="Campaign summary">
                <ReadoutGridField term="Campaign" span={2}>
                  CAMP-2204
                </ReadoutGridField>
                <ReadoutGridField term="Name" span={4}>
                  Shoreline Operations
                </ReadoutGridField>
              </ReadoutGridRow>
            </ReadoutGrid>
          </OperateArea>
        </ManagementPageSpec>
      </GallerySection>
      <GallerySection
        id="layout-management-empty"
        title="Management empty"
        note="Same OperateArea head as the index. Absence lives in the empty plate inside the etched frame, not as a replacement for the page title."
      >
        <ManagementPageSpec tag="OperateArea · title · description · empty plate · no back" bulkheadId="layoutSpecEmptyNav">
          <OperateArea
            className="workspace-area"
            label="Campaign registry"
            title="Campaign Registry"
            description="Find a campaign, then open its record to inspect or configure."
            empty={{
              label: "No campaigns listed",
              note: "Create or import a campaign before opening a record.",
            }}
          />
        </ManagementPageSpec>
      </GallerySection>
      <GallerySection
        id="layout-management-split"
        title="Management split"
        note="Full-bleed ledger: plaque OperateHead (back, centered title, session) above SplitBay start/main/end, decision foot as a sibling. Pass contain={false}. Live-session remains the examination shell; this variant stays inside management chrome."
      >
        <ManagementPageSpec
          contain={false}
          tag='contain={false} · plaque head · SplitBay'
          bulkheadId="layoutSpecSplitNav"
        >
          <OperateArea
            className="workspace-area"
            label="Evaluation record"
            title="Evaluation Record"
            description="Inspect transcript evidence beside criterion evaluations."
            framed={false}
            headArrangement="plaque"
            back={<BackKey label="Queue" onClick={() => undefined} />}
          >
            <SplitBay
              start={<LayoutSlot label="Manifest rail" variant="rail" />}
              end={<LayoutSlot label="Marginalia rail" variant="rail" />}
            >
              <LayoutSlot label="Transcript" />
            </SplitBay>
            <LayoutSlot label="Decision bar" variant="foot" />
          </OperateArea>
        </ManagementPageSpec>
      </GallerySection>
      <GallerySection
        id="layout-guided-task"
        title="Guided-task layout"
        note="Full-height instrument rail, assignment heading, work well, action footer."
      >
        <Spec wide tag='data-layout="guided-task"'>
          <div className="layout-spec">
            <LayoutAssignment id="guided-task">
              <GuidedTaskLayout
                nested
                homeTo="/shared/gallery"
                homeLabel="Channel index"
                railLabel="Specimen instruments"
                brandSuffix="Assignment Station"
                instruments={<LayoutSlot label="Instrument rail" variant="rail" />}
                heading={<LayoutSlot label="Assignment heading" variant="heading" />}
                actions={<LayoutSlot label="Action footer" variant="foot" />}
              >
                <LayoutSlot label="Work well" />
              </GuidedTaskLayout>
            </LayoutAssignment>
          </div>
        </Spec>
      </GallerySection>
      <GallerySection
        id="layout-live-session"
        title="Live-session layout"
        note="Instrument rail, transcript column, composer footer, examiner plate."
      >
        <Spec wide tag='data-layout="live-session"'>
          <div className="layout-spec">
            <LayoutAssignment id="live-session">
              <LiveSessionLayout
                nested
                homeTo="/shared/gallery"
                homeLabel="Channel index"
                railLabel="Specimen instruments"
                brandSuffix="Examination Console"
                instruments={<LayoutSlot label="Instrument rail" variant="rail" />}
                composer={<LayoutSlot label="Composer footer" variant="foot" />}
                examiner={<LayoutSlot label="Examiner plate" variant="examiner" />}
              >
                <LayoutSlot label="Transcript" />
              </LiveSessionLayout>
            </LayoutAssignment>
          </div>
        </Spec>
      </GallerySection>
      <GallerySection
        id="layout-reference"
        title="Reference layout"
        note="Design-lab catalog and component deck only. Never assigned to a production route."
      >
        <Spec wide tag='data-layout="reference"'>
          <div className="layout-spec">
            <LayoutAssignment id="reference">
              <ReferenceLayout
                nested
                commandStrip={{ homeTo: "/shared/gallery", homeLabel: "Channel index", brandSuffix: "Catalog" }}
                footerNote="Quiet footer"
              >
                <LayoutSlot label="Catalog main" />
              </ReferenceLayout>
            </LayoutAssignment>
          </div>
        </Spec>
      </GallerySection>
    </>
  );
}
