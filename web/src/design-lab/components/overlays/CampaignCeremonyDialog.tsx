import type { ComponentProps } from "react";
import { CeremonyDialog } from "../../../design-system/components/overlays/CeremonyDialog";
import { cx } from "../../../lib/cx";

type CampaignCeremonyDialogProps = ComponentProps<typeof CeremonyDialog>;

/** Campaign fill-grid overlay shell. Owns `.ceremony` and `.ceremony-cut`. */
export function CampaignCeremonyDialog({ className, children, ...rest }: CampaignCeremonyDialogProps) {
  return (
    <CeremonyDialog className={cx("ceremony", className)} {...rest}>
      <div className="ceremony-cut">{children}</div>
    </CeremonyDialog>
  );
}
