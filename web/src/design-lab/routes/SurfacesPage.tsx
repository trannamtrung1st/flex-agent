import { useEffect, useState } from "react";
import { Announcer, EtchedFrame, Key, LabThemeToggle } from "../components";
import { CATALOG_ROUTE, SURFACE_COUNT, SURFACE_GROUPS } from "../data/fixtures/surfaces";
import { useAnnouncer } from "../../lib/useAnnouncer";
import { prefersReducedMotion } from "../../lib/format";
import { useSurface } from "../lib/useSurface";
import { Stack } from "../../design-system";
import { ReferenceLayout } from "../../design-system/lab";

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
    <ReferenceLayout
      contain={false}
      commandStrip={{
        homeTo: CATALOG_ROUTE,
        homeLabel: "Channel index",
        brandSuffix: "Channel Index",
        readout: `${SURFACE_COUNT} CHANNELS`,
        identLeading: <LabThemeToggle />,
      }}
      mainLabel="Prototype Surfaces"
      mainClassName="index-board"
      footerNote="Prototype channel index — synthetic demonstration routes only."
      overlays={<Announcer message={message} />}
    >
      <EtchedFrame className="index-frame" revealing={revealing}>
        <Stack className="channel-index" gap="8">
          <Stack as="header" className="channel-index-head" gap="3">
            <h1 id="channel-index-title" className="channel-index-title">
              Prototype Surfaces
            </h1>
            <p className="channel-index-lead">
              Route to any demonstration channel in this workspace. Each surface ships synthetic
              content only — use the demo controls on individual consoles to exercise states.
            </p>
          </Stack>

          {SURFACE_GROUPS.map((group) => (
            <Stack
              as="section"
              key={group.id}
              className="channel-group"
              gap="2.5"
              aria-labelledby={`group-${group.id}`}
            >
              <h2 className="channel-group-label" id={`group-${group.id}`}>
                {group.label}
              </h2>
              <Stack as="ul" className="channel-roster" gap="none">
                {group.channels.map((channel) => (
                  <li key={channel.path} className="channel-row">
                    <span className="channel-code" aria-hidden="true">
                      {channel.code}
                    </span>
                    <Stack className="channel-copy" gap="1">
                      <span className="channel-title">{channel.title}</span>
                      <span className="channel-note">{channel.note}</span>
                    </Stack>
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
              </Stack>
            </Stack>
          ))}
        </Stack>
      </EtchedFrame>
    </ReferenceLayout>
  );
}
