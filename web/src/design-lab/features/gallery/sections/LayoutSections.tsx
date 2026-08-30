import type { ReactNode } from "react";
import {
  BackKey,
  BreadcrumbNav,
  CeremonyArea,
  CeremonyUnavailable,
  CeremonyWait,
  EllipsisKey,
  FieldInput,
  FormField,
  CAMPAIGN_TITLE_PLACEHOLDER,
  GuidedTaskLayout,
  KeyGroup,
  LayoutAssignment,
  LiveSessionLayout,
  ManagementLayout,
  OperateArea,
  PlateFoot,
  ReadoutGrid,
  ReadoutGridField,
  ReadoutGridRow,
  ReadoutList,
  SplitBay,
  Stack,
  StateReadout,
  type ManagementNavigation,
} from "../../../../design-system";
import { ReferenceLayout } from "../../../../design-system/lab";
import { GallerySection, Spec } from "./GallerySection";
import { LayoutSlot } from "./LayoutSlot";

function SetupRecordSpec({
  titleId,
  ready,
}: {
  titleId: string;
  ready: boolean;
}) {
  return (
    <OperateArea
      className="workspace-area work-plane record-plane record-plane--setup"
      frameClassName="record-frame"
      label="Setup and readiness"
      title="Setup and readiness"
      description={
        ready
          ? "Activate this cohort. The browser is not activation authority."
          : "Check readiness on revision 1, then activate this cohort."
      }
      back={<BackKey label="Activities" onClick={() => undefined} />}
    >
      <Stack gap="none" className="setup-ceremony">
        <ReadoutGrid label="Setup tracks" columns={4} className="assignment-instruments">
          <ReadoutGridRow label="Local through cohort">
            <ReadoutGridField term="Local">
              <StateReadout variant="rest" label="Seated" />
            </ReadoutGridField>
            <ReadoutGridField term="Draft">
              <StateReadout variant="rest" label="Revision 1" />
            </ReadoutGridField>
            <ReadoutGridField term="Readiness">
              <StateReadout
                variant={ready ? "sealed" : "dim"}
                solid={ready}
                label={ready ? "Ready" : "Not checked"}
                className={ready ? undefined : "setup-track-now"}
              />
            </ReadoutGridField>
            <ReadoutGridField term="Cohort">
              <StateReadout
                variant={ready ? "live" : "rest"}
                label="Unactivated"
                className={ready ? "setup-track-now" : undefined}
              />
            </ReadoutGridField>
          </ReadoutGridRow>
        </ReadoutGrid>
        <Stack gap="4" className="create-ceremony__scroll">
          <FormField id={titleId} layout="stack" label="Campaign title">
            {(control) => (
              <FieldInput
                {...control}
                defaultValue="Accessibility Standards Review"
                placeholder={CAMPAIGN_TITLE_PLACEHOLDER}
                width="wide"
              />
            )}
          </FormField>
          <p className="setup-ceremony__memory">Stable — new long-term learning disabled</p>
        </Stack>
        <PlateFoot
          className="setup-ceremony__foot"
          arrangement={ready ? "split" : "start"}
          secondary={
            ready ? (
              <KeyGroup aria-label="Draft actions">
                <EllipsisKey variant="quiet" type="button">Save draft</EllipsisKey>
                <EllipsisKey variant="quiet" type="button">Check readiness</EllipsisKey>
              </KeyGroup>
            ) : undefined
          }
          primary={ready ? <EllipsisKey variant="activate" type="button">Activate cohort</EllipsisKey> : undefined}
        >
          {ready ? null : (
            <KeyGroup aria-label="Draft actions">
              <EllipsisKey variant="quiet" type="button">Save draft</EllipsisKey>
              <EllipsisKey variant="quiet" type="button">Check readiness</EllipsisKey>
            </KeyGroup>
          )}
        </PlateFoot>
      </Stack>
    </OperateArea>
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
  breadcrumbs,
  children,
}: {
  tag: string;
  bulkheadId: string;
  currentLabel?: string;
  contain?: boolean;
  breadcrumbs?: ReactNode;
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
            breadcrumbs={breadcrumbs}
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
        note="Console list or registry. OperateArea supplies the page title and description. Omit BackKey — the gangway (or command-strip home) is the return path. Etch lists and tables; destination, assignment, and Status Bay plate grids set framed={false}."
      >
        <ManagementPageSpec tag="OperateArea · title · description · registry body · no back" bulkheadId="layoutSpecIndexNav" breadcrumbs={
          <BreadcrumbNav homeHref="/shared/gallery" items={[{ label: "Campaigns", current: true }]} />
        }>
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
        note="Nested record under a console index. BackKey returns to that index and trails the copy cluster (title + description) at desktop widths. Keep the same OperateArea title/description/body stack; do not invent a second page head or put Back on the breadcrumb trail. Stacked records (ReadoutGrid + WorkWells) set framed={false}; do not wrap already-sectioned body — or the readout grid alone — in an etched grouping box. The grid is a rule band."
      >
        <ManagementPageSpec tag="OperateArea · BackKey · title · description · record body" bulkheadId="layoutSpecRecordNav" breadcrumbs={
          <BreadcrumbNav
            homeHref="/shared/gallery"
            items={[
              { label: "Campaigns", href: "/shared/gallery#layout-management-index" },
              { label: "Campaign record", current: true },
            ]}
          />
        }>
          <OperateArea
            className="workspace-area"
            framed={false}
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
        id="layout-management-setup"
        title="Management setup"
        note="Production Setup and readiness is this nested record: ReadoutGrid tracks on the etched ceremony plate, campaign title in the same well, and unarmed keys omitted. The Campaign Configuration dialog remains the keys and disabledReason specimen, not this station."
      >
        <ManagementPageSpec
          tag="OperateArea · ReadoutGrid tracks · Save and Check · Activate omitted"
          bulkheadId="layoutSpecSetupDraftNav"
          breadcrumbs={
            <BreadcrumbNav
              homeHref="/shared/gallery"
              items={[
                { label: "Activities", href: "/shared/gallery#layout-management-index" },
                { label: "Setup and readiness", current: true },
              ]}
            />
          }
        >
          <SetupRecordSpec titleId="lab-setup-title-draft" ready={false} />
        </ManagementPageSpec>
      </GallerySection>
      <GallerySection
        id="layout-management-empty"
        title="Management empty"
        note="Same OperateArea head as the index. Absence lives in the empty plate inside the etched frame, not as a replacement for the page title."
      >
        <ManagementPageSpec tag="OperateArea · title · description · empty plate · no back" bulkheadId="layoutSpecEmptyNav" breadcrumbs={
          <BreadcrumbNav homeHref="/shared/gallery" items={[{ label: "Campaigns", current: true }]} />
        }>
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
        id="layout-management-ceremony"
        title="Management ceremony"
        note="Unavailable and recovery planes use CeremonyUnavailable: a hug column that sizes to the inset empty well (hugMeasure auto), with the operate-head inset by the frame cut so the title shares the visible top edge. Pass danger for Access denied / failed-sign-in titles (fault phosphor, not teal). Recovery is a quiet key centered in the well, never the amber Open-session skin. Pin sm/md/lg only when a dialog-width well is required. Do not stretch this well across the main landmark on desktop."
      >
        <div className="spec-row spec-row--layout-contain">
          <ManagementPageSpec tag="CeremonyUnavailable · hug · quiet recovery" bulkheadId="layoutSpecCeremonyNav">
            <CeremonyUnavailable
              title="This destination is not available"
              note="The current authorized relationship cannot use this locator."
              recovery={{ label: "Return to Home", to: "/shared/gallery" }}
            />
          </ManagementPageSpec>
          <ManagementPageSpec tag="CeremonyUnavailable · danger · --fg-danger" bulkheadId="layoutSpecCeremonyDangerNav">
            <CeremonyUnavailable
              title="Access denied"
              note="My work is not available for the current authorized relationship."
              danger
              recovery={{ label: "Return to Home", to: "/shared/gallery" }}
            />
          </ManagementPageSpec>
        </div>
      </GallerySection>
      <GallerySection
        id="layout-management-loading"
        title="Management loading"
        note="Protected page-level wait uses CeremonyWait: inset wait-plate (wait-mark, label, scan-track) inside the hug etched well. Do not drop an inline WaitPanel into this frame."
      >
        <ManagementPageSpec tag="CeremonyArea · hug column · inset wait-plate" bulkheadId="layoutSpecLoadingNav">
          <CeremonyArea
            label="Establishing session"
            title="Establishing session"
            description="Confirming the production application session for this organization."
          >
            <CeremonyWait label="Establishing session context…" />
          </CeremonyArea>
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
          breadcrumbs={
            <BreadcrumbNav
              homeHref="/shared/gallery"
              items={[
                { label: "Review work", href: "/shared/gallery#layout-management-split" },
                { label: "Evaluation record", current: true },
              ]}
            />
          }
        >
          <OperateArea
            className="workspace-area record-view"
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
