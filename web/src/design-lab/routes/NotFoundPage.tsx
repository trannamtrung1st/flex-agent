import { EmptyPlate, Key, CATALOG_NAV, CATALOG_ROUTE } from "../components";
import { useSurface } from "../lib/useSurface";
import { ReferenceLayout } from "../../design-system/lab";

export function NotFoundPage() {
  useSurface("not-found");
  return (
    <ReferenceLayout
      commandStrip={{ homeTo: CATALOG_ROUTE, homeLabel: "Channel index", nav: [CATALOG_NAV] }}
      mainLabel="Unknown channel"
      mainClassName="board"
      footerNote="Synthetic demonstration content — no real participant data."
    >
      <div className="board-empty">
        <EmptyPlate
          label="Channel not found"
          note="That channel is not on this console."
        >
          <Key variant="quiet" to={CATALOG_ROUTE}>
            Return to channel index
          </Key>
        </EmptyPlate>
      </div>
    </ReferenceLayout>
  );
}
