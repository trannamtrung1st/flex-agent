type BadgeVariant = "default" | "brand" | "success" | "warning" | "danger" | "info" | "tier";

interface BadgeProps {
  children: React.ReactNode;
  variant?: BadgeVariant;
  className?: string;
}

export function Badge({ children, variant = "default", className = "" }: BadgeProps) {
  const classes = ["badge", `badge-${variant}`, className].filter(Boolean).join(" ");
  return <span className={classes}>{children}</span>;
}
