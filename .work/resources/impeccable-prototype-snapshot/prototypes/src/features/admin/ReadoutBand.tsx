import type { ReactNode } from "react";
import { ActivationMark } from "../../components/state/ActivationMark";

export { ActivationMark } from "../../components/state/ActivationMark";

export function ReadoutBand({
  label,
  children,
  className,
}: {
  label: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={`campaigns-readout-band${className ? ` ${className}` : ""}`}
      aria-label={label}
    >
      {children}
    </div>
  );
}

export function ReadoutField({
  term,
  children,
  className,
}: {
  term: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <dl className="campaigns-readout-field">
      <dt>{term}</dt>
      <dd className={className}>{children}</dd>
    </dl>
  );
}

export function ActivationReadout({ frozen }: { frozen: boolean }) {
  return (
    <ReadoutField term="Activation">
      <ActivationMark frozen={frozen} className="campaigns-state" />
    </ReadoutField>
  );
}
