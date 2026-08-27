export function DateGlyph({ className = "datetime-glyph" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 14 14" aria-hidden="true">
      <rect x="1.5" y="2.5" width="11" height="10" fill="none" stroke="currentColor" strokeWidth="1.1" />
      <path d="M1.5 5.2h11" fill="none" stroke="currentColor" strokeWidth="1.1" />
      <path d="M4.2 1.4v2.2M9.8 1.4v2.2" fill="none" stroke="currentColor" strokeWidth="1.1" strokeLinecap="square" />
    </svg>
  );
}

export function TimeGlyph({ className = "datetime-glyph" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 14 14" aria-hidden="true">
      <circle cx="7" cy="7" r="5.2" fill="none" stroke="currentColor" strokeWidth="1.1" />
      <path d="M7 3.8v3.2l2.4 1.4" fill="none" stroke="currentColor" strokeWidth="1.1" strokeLinecap="square" />
    </svg>
  );
}
