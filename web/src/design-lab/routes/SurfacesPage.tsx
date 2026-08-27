import { useEffect, useState } from "react";
import { Announcer, CommandStrip, ConsoleFoot, EtchedFrame, Key } from "../components";
import { SURFACE_COUNT, SURFACE_GROUPS } from "../data/fixtures/surfaces";
import { useAnnouncer } from "../../lib/useAnnouncer";
import { prefersReducedMotion } from "../../lib/format";
import { useSurface } from "../lib/useSurface";

export function SurfacesPage() {
  useSurface("surfaces-index");
  const { message, announce } = useAnnouncer();
  const [revealing, setRevealing] = useState(() => !prefersReducedMotion());

  useEffect(() => {
    if (!revealing) return;
    const t = window.setTimeout(() => setRevealing(false), 640);
    return () => window.clearTimeout(t);
  }, [revealing]);

  return (
    <>
      <CommandStrip
        origin
        brandSuffix="Channel Index"
        readout={`${SURFACE_COUNT} CHANNELS`}
      />
      <main className="index-board" aria-labelledby="channel-index-title">
        <EtchedFrame className="index-frame" revealing={revealing}>
          <div className="channel-index">
            <header className="channel-index-head">
              <h1 id="channel-index-title" className="channel-index-title">
                Prototype Surfaces
              </h1>
              <p className="channel-index-lead">
                Route to any demonstration channel in this workspace. Each surface ships synthetic
                content only — use the demo controls on individual consoles to exercise states.
              </p>
            </header>

            {SURFACE_GROUPS.map((group) => (
              <section
                key={group.id}
                className="channel-group"
                aria-labelledby={`group-${group.id}`}
              >
                <h2 className="channel-group-label" id={`group-${group.id}`}>
                  {group.label}
                </h2>
                <ul className="channel-roster">
                  {group.channels.map((channel) => (
                    <li key={channel.path} className="channel-row">
                      <span className="channel-code" aria-hidden="true">
                        {channel.code}
                      </span>
                      <div className="channel-copy">
                        <span className="channel-title">{channel.title}</span>
                        <span className="channel-note">{channel.note}</span>
                      </div>
                      <Key
                        variant="open"
                        to={channel.path}
                        ariaLabel={`Open ${channel.title}`}
                        onClick={() => announce(`Opening ${channel.title}.`)}
                      >
                        Open
                      </Key>
                    </li>
                  ))}
                </ul>
              </section>
            ))}
          </div>
        </EtchedFrame>
      </main>
      <ConsoleFoot note="Prototype channel index — synthetic demonstration routes only." />
      <Announcer message={message} />
    </>
  );
}
