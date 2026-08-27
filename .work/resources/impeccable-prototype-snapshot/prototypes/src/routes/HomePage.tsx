import { useEffect, useState } from "react";
import { Link } from "react-router";
import {
  Announcer,
  CommandStrip,
  ConsoleFoot,
  DemoPlate,
  EmptyPlate,
  EtchedFrame,
  PARTICIPANT_HOME,
  PARTICIPANT_IDENTITY,
  RecordSeal,
  SignOutCeremony,
  StateIndicator,
  usePrototypeSignOut,
} from "../components";
import { HOME_BAYS, HOME_DEMO, HOME_DEMO_KEYS } from "../data/fixtures/home";
import type { HomeEnrollment } from "../data/types";
import { useAnnouncer } from "../lib/useAnnouncer";
import { useDemoParam } from "../lib/useDemoParam";
import { prefersReducedMotion } from "../lib/format";
import { useSurface } from "../lib/useSurface";

export function HomePage() {
  useSurface("participant-home");
  const [demo, setDemo] = useDemoParam(HOME_DEMO_KEYS, "populated");
  const { message, announce } = useAnnouncer();
  const [revealing, setRevealing] = useState(() => !prefersReducedMotion());
  const entries = HOME_DEMO[demo];
  const rosterNote =
    entries.length === 0
      ? "No assigned work."
      : `Roster showing ${entries.length} enrollment${entries.length === 1 ? "" : "s"}.`;

  useEffect(() => {
    if (!revealing) return;
    const t = window.setTimeout(() => setRevealing(false), 640);
    return () => window.clearTimeout(t);
  }, [revealing]);

  const dense = HOME_BAYS.some((bay) => entries.filter((e) => e.bay === bay.id).length > 1);

  const { actions, signOutOpen, setSignOutOpen } = usePrototypeSignOut();

  return (
    <>
      <CommandStrip
        nav={[{ to: PARTICIPANT_HOME, label: "Home" }]}
        profile={PARTICIPANT_IDENTITY}
        actions={actions}
      />
      <main className="board" aria-label="Assigned work by record state">
        <h1 className="visually-hidden">Assigned work</h1>
        <EtchedFrame className="board-frame" revealing={revealing}>
          {entries.length === 0 ? (
            <div className="board-empty">
              <EmptyPlate
                label="No assigned work"
                note="Nothing is enrolled to this participant. Assignments appear here the moment an administrator enrolls you."
              />
            </div>
          ) : (
            <div className={`bays${dense ? " bays--dense" : ""}`}>
              {HOME_BAYS.map((bay) => {
                const plates = entries.filter((e) => e.bay === bay.id);
                return (
                  <section className="bay" aria-labelledby={`bay-${bay.id}`} key={bay.id}>
                    <h2 className="bay-head" id={`bay-${bay.id}`}>
                      {bay.label}
                    </h2>
                    <div className="bay-plates">
                      {plates.length ? plates.map((entry) => <Plate key={entry.campaign} entry={entry} />) : (
                        <p className="bay-empty">No enrollments in this bay</p>
                      )}
                    </div>
                  </section>
                );
              })}
            </div>
          )}
        </EtchedFrame>
      </main>
      <ConsoleFoot note="Synthetic demonstration content — no real participant data.">
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
      </ConsoleFoot>
      <Announcer message={message || rosterNote} />
      <SignOutCeremony open={signOutOpen} onClose={() => setSignOutOpen(false)} />
    </>
  );
}

function Plate({ entry }: { entry: HomeEnrollment }) {
  return (
    <article className={`plate${entry.record === "Released" ? " plate--released" : ""}`}>
      <div className="plate-in">
        <dl className="plate-readout">
          <div className="plate-row">
            <dt>Campaign</dt>
            <dd>{entry.campaign}</dd>
          </div>
          <div className="plate-row plate-row--title">
            <dt>Assignment</dt>
            <dd>{entry.title}</dd>
          </div>
          <div className="plate-row">
            <dt>Deadline</dt>
            <dd>{entry.deadline}</dd>
          </div>
          <div className="plate-row">
            <dt>Phase</dt>
            <dd>{entry.phase}</dd>
          </div>
          <div className="plate-row plate-row--record">
            <dt>Record</dt>
            <dd>
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
            </dd>
          </div>
        </dl>
        {entry.key ? (
          <div className="plate-keys">
            <Link className={entry.key.kind === "open" ? "key key--open" : "key key--quiet"} to={entry.key.to}>
              {entry.key.label}
            </Link>
          </div>
        ) : (
          <div className="plate-keys" aria-hidden="true" />
        )}
      </div>
    </article>
  );
}
