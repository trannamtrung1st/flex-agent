interface ProtectedLoadingProps {
  label?: string;
}

export function ProtectedLoading({ label = "Loading protected content…" }: ProtectedLoadingProps) {
  return (
    <div className="loading-panel" role="status" aria-live="polite" aria-busy="true">
      <span className="loading-spinner" aria-hidden="true" />
      <span>{label}</span>
    </div>
  );
}
