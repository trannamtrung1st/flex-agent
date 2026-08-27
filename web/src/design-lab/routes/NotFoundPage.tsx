import { CommandStrip, ConsoleFoot, EmptyPlate, Key, CATALOG_NAV, CATALOG_ROUTE } from "../components";
import { useSurface } from "../lib/useSurface";

export function NotFoundPage() {
  useSurface("not-found");
  return (
    <>
      <CommandStrip nav={[CATALOG_NAV]} />
      <main className="board" aria-label="Unknown channel">
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
      </main>
      <ConsoleFoot note="Synthetic demonstration content — no real participant data." />
    </>
  );
}
