export function RecordSeal() {
  return (
    <svg className="record-seal" viewBox="0 0 22 22" aria-hidden="true" focusable="false">
      <circle cx="11" cy="11" r="10" />
      <circle cx="11" cy="11" r="7" />
      <path d="M7.6 11.2 L10 13.6 L14.6 8.4" />
    </svg>
  );
}

export function StageBars({ stage, total, complete }: { stage: number; total: number; complete?: boolean }) {
  return (
    <div className="stage-bars" aria-hidden="true">
      {Array.from({ length: total }, (_, i) => (
        <span
          key={i}
          className={complete || i < stage - 1 ? "is-done" : !complete && i === stage - 1 ? "is-now" : undefined}
        />
      ))}
    </div>
  );
}
