import type { ReactNode } from "react";
import { Key, type KeySize, type KeyVariant } from "./Key";

export function EllipsisKey({
  children,
  tooltip,
  variant,
  size,
  className,
  id,
  type,
  disabled,
  waiting,
  onClick,
  ariaLabel,
  disabledReason,
}: {
  children: ReactNode;
  tooltip?: string;
  variant?: KeyVariant;
  size?: KeySize;
  className?: string;
  id?: string;
  type?: "button" | "submit";
  disabled?: boolean;
  waiting?: boolean;
  onClick?: () => void;
  ariaLabel?: string;
  disabledReason?: string;
}) {
  const tip = tooltip ?? (typeof children === "string" ? children : undefined);

  return (
    <Key
      truncate
      tooltip={tip}
      variant={variant}
      size={size}
      className={className}
      id={id}
      type={type}
      disabled={disabled}
      waiting={waiting}
      onClick={onClick}
      ariaLabel={ariaLabel}
      disabledReason={disabledReason}
    >
      {children}
    </Key>
  );
}
