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
  const truncated = shown !== value;
  const focusable = truncated && tabbable;

  return (
    <TooltipHost tip={truncated ? value : undefined} tone="value" className={className}>
      <span className="compact-id" tabIndex={focusable ? 0 : undefined}>
        <span aria-hidden={truncated || undefined}>{shown}</span>
        {truncated ? <span className="visually-hidden">{value}</span> : null}
      </span>
    </TooltipHost>
  );
}
