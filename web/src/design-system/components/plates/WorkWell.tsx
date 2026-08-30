import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { Stack } from "../layout/Stack";
import type { LayoutSpace } from "../layout/types";
import { PlateFoot } from "./EtchedFrame";

export function WorkWell({
  head,
  foot,
  children,
  className,
  revealing,
  label,
  live = true,
}: {
  head?: ReactNode;
  foot?: ReactNode;
  children: ReactNode;
  className?: string;
  revealing?: boolean;
  label?: string;
  live?: boolean;
}) {
  return (
    <Stack
      as="article"
      className={cx("work-well", revealing && "is-revealing", className)}
      gap="none"
      aria-label={label}
      aria-live={live ? "polite" : undefined}
      aria-atomic={live ? "true" : undefined}
    >
      {head}
      <div className="work-well__body">{children}</div>
      {foot ? <PlateFoot className="work-well__foot" arrangement="start">{foot}</PlateFoot> : null}
    </Stack>
  );
}

export function WorkWellHead({
  title,
  ident,
  children,
  gap = "2.5",
}: {
  title?: string;
  ident?: string;
  children?: ReactNode;
  gap?: LayoutSpace;
}) {
  return (
    <Stack as="header" className="work-well__head" gap={gap}>
      {children ?? (
        <>
          {title ? <h2 className="work-well__title">{title}</h2> : null}
          {ident ? <p className="work-well__ident">{ident}</p> : null}
        </>
      )}
    </Stack>
  );
}

export function WorkWellSection({ children, className }: { children: ReactNode; className?: string }) {
  return <section className={cx("work-well__section", className)}>{children}</section>;
}

export function PlateStatusMark({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <p className={cx("plate-status-mark", className)} role="status">
      {children}
    </p>
  );
}
