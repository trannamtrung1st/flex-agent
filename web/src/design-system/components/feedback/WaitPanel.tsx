import { Inline } from "../layout/Inline";

export function WaitPanel({
  label = "Loading protected content…",
  announceOnly = false,
}: {
  label?: string;
  announceOnly?: boolean;
}) {
  return (
    <Inline className="loading-panel" gap="3" wrap={false} role="status" aria-live="polite" aria-busy="true">
      <span className="wait-mark" aria-hidden="true" />
      <span className={announceOnly ? "visually-hidden" : undefined}>{label}</span>
    </Inline>
  );
}
