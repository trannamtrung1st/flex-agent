import { CeremonyUnavailable, LabThemeToggle, CATALOG_NAV, CATALOG_ROUTE } from "../components";
import { useSurface } from "../lib/useSurface";
import { ReferenceLayout } from "../../design-system/lab";

export function NotFoundPage() {
  useSurface("not-found");
  return (
    <ReferenceLayout
      commandStrip={{
        homeTo: CATALOG_ROUTE,
        homeLabel: "Channel index",
        nav: [CATALOG_NAV],
        identLeading: <LabThemeToggle />,
      }}
      mainLabel="Unknown channel"
      mainClassName="board"
      footerNote="Synthetic demonstration content — no real participant data."
    >
      <CeremonyUnavailable
        label="Unknown channel"
        title="Channel not found"
        note="That channel is not on this console."
        recovery={{ label: "Return to channel index", to: CATALOG_ROUTE }}
      />
    </ReferenceLayout>
  );
}
