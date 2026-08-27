import type { ComponentProps } from "react";
import { Key } from "./Key";

type BackKeyProps = Omit<ComponentProps<typeof Key>, "children" | "variant"> & {
  label: string;
};

export function BackKey({ label, ...props }: BackKeyProps) {
  return (
    <Key variant="back" {...props}>
      <svg viewBox="0 0 12 12" aria-hidden="true">
        <path d="M9 1L3 6l6 5" fill="none" stroke="currentColor" strokeWidth="1.6" />
      </svg>
      <span>{label}</span>
    </Key>
  );
}
