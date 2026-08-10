import type { ReactNode } from "react";

interface SafeContentProps {
  children: ReactNode;
  className?: string;
}

export function SafeContent({ children, className = "" }: SafeContentProps) {
  return <div className={className}>{children}</div>;
}
