export function ActionMenuGlyph({ className = "action-menu-glyph" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 18 6" aria-hidden="true">
      <circle cx="3" cy="3" r="1.25" />
      <circle cx="9" cy="3" r="1.25" />
      <circle cx="15" cy="3" r="1.25" />
    </svg>
  );
}
