export function DocumentGlyph({
  current = false,
  className,
}: {
  current?: boolean;
  className?: string;
}) {
  return (
    <svg
      className={["doc-glyph", current ? "doc-glyph--current" : undefined, className].filter(Boolean).join(" ")}
      viewBox="0 0 12 14"
      aria-hidden="true"
    >
      <path d="M1 .5h6.5L11 4v9.5H1z" />
      <path d="M7.5 .5V4H11" />
    </svg>
  );
}
