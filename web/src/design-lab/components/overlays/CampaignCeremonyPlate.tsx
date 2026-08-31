import type { ComponentProps, ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { Stack } from "../../../design-system/components/layout/Stack";
import { Grid } from "../../../design-system/components/layout/Grid";
import { KeyGroup } from "../../../design-system/components/keys/KeyGroup";
import {
  DialogPlate,
  DialogPlateBody,
  DialogPlateHead,
} from "../../../design-system/components/overlays/DialogPlate";

export function CampaignCeremonyPlate({
  frozen,
  className,
  children,
}: {
  frozen?: boolean;
  className?: string;
  children: ReactNode;
}) {
  return (
    <DialogPlate width="wide" className={cx("ceremony-plate", frozen && "is-frozen", className)}>
      {children}
    </DialogPlate>
  );
}

export function CampaignCeremonyHead({
  title,
  titleId,
}: {
  title: ReactNode;
  titleId: string;
}) {
  return (
    <DialogPlateHead
      title={title}
      titleId={titleId}
      marker={false}
      className="ceremony-head"
      titleClassName="ceremony-title"
    >
      <span className="ceremony-trace" aria-hidden="true">
        <span className="ceremony-trace-node" />
      </span>
    </DialogPlateHead>
  );
}

export function CampaignCeremonyBody({ children }: { children: ReactNode }) {
  return <DialogPlateBody className="ceremony-body">{children}</DialogPlateBody>;
}

export function CampaignCeremonyNote({
  children,
  role,
}: {
  children: ReactNode;
  role?: "status";
}) {
  return (
    <p className="ceremony-note" role={role}>
      {children}
    </p>
  );
}

export function CampaignCeremonyFootActions({ children }: { children: ReactNode }) {
  return (
    <Stack gap="3" className="ceremony-foot-actions">
      {children}
    </Stack>
  );
}

export function CampaignCeremonyFootRow({
  children,
  "aria-label": ariaLabel,
}: {
  children: ReactNode;
  "aria-label"?: string;
}) {
  return (
    <KeyGroup className="ceremony-foot-row" aria-label={ariaLabel}>
      {children}
    </KeyGroup>
  );
}

export function CampaignCeremonyFooter({ children }: { children: ReactNode }) {
  return <footer className="ceremony-foot">{children}</footer>;
}

export function CampaignCeremonyConfigGrid({
  className,
  gap = "4",
  ...rest
}: ComponentProps<typeof Grid>) {
  return (
    <Grid
      gap={gap}
      className={cx("ceremony-config-grid", typeof className === "string" ? className : undefined)}
      {...rest}
    />
  );
}
