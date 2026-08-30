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
import { AssignmentPlate, InstantReadout, Key, ManagementLayout, OperateArea, Stack } from "../../design-system";
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
      <OperateArea
        className={entries.length === 0 ? "workspace-area board assignment-board--hug" : "workspace-area board"}
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
          <div className={`bays${dense ? " bays--dense" : ""}`}>
            {HOME_BAYS.map((bay) => {
              const plates = entries.filter((e) => e.bay === bay.id);
              return (
                <Stack as="section" className="bay" gap="none" aria-labelledby={`bay-${bay.id}`} key={bay.id}>
                  <h2 className="bay-head" id={`bay-${bay.id}`}>
                    {bay.label}
                  </h2>
                  <Stack gap="4" className="bay-plates">
                    {plates.length ? plates.map((entry) => (
                      <StatusBayPlate key={entry.campaign} entry={entry} />
                    )) : (
                      <p className="bay-empty">No enrollments in this bay</p>
                    )}
                  </Stack>
                </Stack>
              );
            })}
          </div>
        )}
      </OperateArea>
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
        { term: "Assignment", value: entry.title, className: "readout--title" },
        { term: "Deadline", value: deadlineValue(entry) },
        { term: "Phase", value: entry.phase },
        {
          term: "Record",
          className: "readout--record",
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
