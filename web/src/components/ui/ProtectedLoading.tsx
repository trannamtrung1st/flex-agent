export function ProtectedLoading({
  label = "Loading protected content…",
  announceOnly = false,
}: {
  label?: string;
  announceOnly?: boolean;
}) {
  return (
    <div className="loading-panel" role="status" aria-live="polite" aria-busy="true">
      <span className="wait-mark" aria-hidden="true" />
      <span className={announceOnly ? "visually-hidden" : undefined}>{label}</span>
    </div>
  );
}
