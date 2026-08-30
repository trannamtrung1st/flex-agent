export function LayoutSlot({
  label,
  variant = "bay",
}: {
  label: string;
  variant?: "bay" | "rail" | "heading" | "foot" | "examiner";
}) {
  return (
    <div className={`layout-slot layout-slot--${variant}`}>
      <span className="layout-slot__name">{label}</span>
    </div>
  );
}
