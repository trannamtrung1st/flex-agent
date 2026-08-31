import {
  Announcer,
  DemoPlate,
  PARTICIPANT_HOME,
  PARTICIPANT_IDENTITY,
  RecordSeal,
  SignOutCeremony,
  StateIndicator,
  usePrototypeSignOut,
  CATALOG_ROUTE,
} from "../components";
import { InstantReadout, Key, ManagementLayout } from "../../design-system";
import { HomeBoardOperateArea } from "../components/operate";
import { StatusBay, StatusBays } from "../components/plates";
import { AssignmentPlate } from "../../components/work/AssignmentPlate";
import { HOME_BAYS, HOME_DEMO, HOME_DEMO_KEYS } from "../data/fixtures/home";
import type { HomeEnrollment } from "../data/types";
import { useAnnouncer } from "../../lib/useAnnouncer";
import { useDemoParam } from "../lib/useDemoParam";
import { DESIGN_LAB_CAMPAIGN_TIME_ZONE } from "../../lib/format";
import { useSurface } from "../lib/useSurface";

function deadlineValue(entry: HomeEnrollment) {
  if (!entry.deadlineUtc) {
    return entry.deadline;
  }
  return (
    <>
      <InstantReadout value={entry.deadlineUtc} timeZone={DESIGN_LAB_CAMPAIGN_TIME_ZONE} />
      <span className="visually-hidden">{DESIGN_LAB_CAMPAIGN_TIME_ZONE}</span>
    </>
  );
}

export function HomePage() {
  useSurface("participant-home");
  const [demo, setDemo] = useDemoParam(HOME_DEMO_KEYS, "populated");
  const { message, announce } = useAnnouncer();
  const entries = HOME_DEMO[demo];
  const rosterNote =
    entries.length === 0
      ? "No assigned work."
      : `Roster showing ${entries.length} enrollment${entries.length === 1 ? "" : "s"}.`;

  const dense = HOME_BAYS.some((bay) => entries.filter((e) => e.bay === bay.id).length > 1);

  const { actions, signOutOpen, setSignOutOpen } = usePrototypeSignOut();

  return (
    <ManagementLayout
      contain={false}
      commandStrip={{
        homeTo: CATALOG_ROUTE,
        homeLabel: "Channel index",
        nav: [{ to: PARTICIPANT_HOME, label: "Home" }],
        profile: PARTICIPANT_IDENTITY,
        actions,
      }}
      mainLabel="Assigned work by record state"
      footerNote="Synthetic demonstration content — no real participant data."
      footer={
        <DemoPlate
          id="demoState"
          value={demo}
          onChange={(v) => {
            setDemo(v as typeof demo);
            announce(
              HOME_DEMO[v as typeof demo].length === 0
                ? "No assigned work."
                : `Roster showing ${HOME_DEMO[v as typeof demo].length} enrollments.`,
            );
          }}
          options={[
            { value: "populated", label: "Roster populated" },
            { value: "crowded", label: "Crowded roster" },
            { value: "single", label: "Single open assignment" },
            { value: "empty", label: "No enrollments" },
          ]}
        />
      }
      overlays={
        <>
          <Announcer message={message || rosterNote} />
          <SignOutCeremony open={signOutOpen} onClose={() => setSignOutOpen(false)} />
        </>
      }
    >
      <HomeBoardOperateArea
        hug={entries.length === 0 ? "board" : undefined}
        label="Assigned work by record state"
        title="Assigned work"
        description="Open assignments and released records for this participant."
        framed={entries.length === 0}
        empty={
          entries.length === 0
            ? {
                label: "No assigned work",
                note: "Nothing is enrolled to this participant. Assignments appear here the moment an administrator enrolls you.",
              }
            : undefined
        }
      >
        {entries.length === 0 ? null : (
          <StatusBays dense={dense}>
            {HOME_BAYS.map((bay) => {
              const plates = entries.filter((e) => e.bay === bay.id);
              return (
                <StatusBay
                  key={bay.id}
                  id={bay.id}
                  label={bay.label}
                  empty={plates.length ? undefined : "No enrollments in this bay"}
                >
                  {plates.length ? plates.map((entry) => (
                    <StatusBayPlate key={entry.campaign} entry={entry} />
                  )) : undefined}
                </StatusBay>
              );
            })}
          </StatusBays>
        )}
      </HomeBoardOperateArea>
    </ManagementLayout>
  );
}

function StatusBayPlate({ entry }: { entry: HomeEnrollment }) {
  const released = entry.record === "Released";
  return (
    <AssignmentPlate
      label={entry.campaign}
      released={released}
      rows={[
        { term: "Campaign", value: entry.campaign },
        { term: "Assignment", value: entry.title, emphasis: "title" },
        { term: "Deadline", value: deadlineValue(entry) },
        { term: "Phase", value: entry.phase },
        {
          term: "Record",
          emphasis: "inline",
          value: (
            <>
              {entry.record}
              {entry.mark === "live" ? (
                <>
                  {" "}
                  <StateIndicator variant="live" solid />
                  <span className="visually-hidden">— session in progress</span>
                </>
              ) : null}
              {entry.mark === "seal" ? (
                <>
                  {" "}
                  <RecordSeal />
                  <span className="visually-hidden">— result sealed and released</span>
                </>
              ) : null}
            </>
          ),
        },
      ]}
      action={
        entry.key ? (
          <Key
            variant={entry.key.kind === "open" ? "open" : "quiet"}
            to={entry.key.to}
            ariaLabel={`${entry.key.label} ${entry.campaign}`}
          >
            {entry.key.label}
          </Key>
        ) : undefined
      }
    />
  );
}
