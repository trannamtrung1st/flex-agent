import { useRef } from "react";
import { compactRegistryId } from "./compactRegistryId";
import { TooltipHost } from "../keys/TooltipHost";

export function CompactId({
  value,
  display,
  className,
  tabbable = false,
}: {
  value: string;
  /** Override the default center-truncated registry form. */
  display?: string;
  className?: string;
  /** When true, truncated ids join the tab order so focus-visible opens the plaque. */
  tabbable?: boolean;
}) {
  const shown = display ?? compactRegistryId(value);
  const logicallyTruncated = shown !== value;
  const compactRef = useRef<HTMLSpanElement>(null);
  const focusable = logicallyTruncated && tabbable;

  return (
    <TooltipHost
      tip={value}
      tone="value"
      className={className}
      tipOnlyWhenTruncated={!logicallyTruncated}
      truncationRef={logicallyTruncated ? undefined : compactRef}
    >
      <span ref={compactRef} className="compact-id" tabIndex={focusable ? 0 : undefined}>
        <span aria-hidden={logicallyTruncated || undefined}>{shown}</span>
        {logicallyTruncated ? <span className="visually-hidden">{value}</span> : null}
      </span>
    </TooltipHost>
  );
}
