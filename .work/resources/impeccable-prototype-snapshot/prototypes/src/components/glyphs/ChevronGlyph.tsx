export function ChevronGlyph({ className = "chevron-glyph" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 10 6" aria-hidden="true">
      <path d="M1 1l4 4 4-4" />
    </svg>
  );
}
