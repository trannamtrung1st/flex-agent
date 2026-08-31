import type { ComponentProps } from "react";
import { cx } from "../../lib/cx";
import { StateReadout } from "../../design-system";

type SetupTrackReadoutProps = ComponentProps<typeof StateReadout> & {
  now?: boolean;
};

export function SetupTrackReadout({ now, className, ...rest }: SetupTrackReadoutProps) {
  return <StateReadout {...rest} className={cx(now && "setup-track-now", className)} />;
}
