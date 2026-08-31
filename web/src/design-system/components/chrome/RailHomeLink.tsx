import type { ReactNode } from "react";
import { Link, type To } from "react-router-dom";

export function RailHomeLink({ to, children }: { to: To; children: ReactNode }) {
  return (
    <Link className="rail-home-link" to={to}>
      <svg viewBox="0 0 10 10" aria-hidden="true" focusable="false">
        <path
          d="M6.5 1.5 L3 5 L6.5 8.5"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.1"
          strokeLinecap="square"
        />
      </svg>
      {children}
    </Link>
  );
}
