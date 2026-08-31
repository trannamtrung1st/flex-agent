import { createContext, useContext, type ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { Stack } from "../layout/Stack";
import type { LayoutSpace } from "../layout/types";
import { PlateFoot } from "./EtchedFrame";

export type WorkWellSeat = "stack" | "pane";
export type WorkWellInset = "frame" | "flush";
export type WorkWellHeadMark = "span" | "title";
export type WorkWellHeadTitleRole = "plate" | "task";

const WorkWellSeatContext = createContext<WorkWellSeat>("pane");

export function WorkWell({
  head,
  foot,
  children,
  className,
  revealing,
  label,
  live = true,
  seat = "pane",
  inset,
}: {
  head?: ReactNode;
  foot?: ReactNode;
  children: ReactNode;
  className?: string;
  revealing?: boolean;
  label?: string;
  live?: boolean;
  seat?: WorkWellSeat;
  inset?: WorkWellInset;
}) {
  const resolvedInset = inset ?? (seat === "stack" ? "flush" : "frame");
  return (
    <WorkWellSeatContext.Provider value={seat}>
      <Stack
        as="article"
        className={cx("work-well", revealing && "is-revealing", className)}
        gap="none"
        data-seat={seat}
        data-inset={resolvedInset}
        aria-label={label}
        aria-live={live ? "polite" : undefined}
        aria-atomic={live ? "true" : undefined}
      >
        {head}
        <div className="work-well__body">{children}</div>
        {foot ? <PlateFoot className="work-well__foot" arrangement="start">{foot}</PlateFoot> : null}
      </Stack>
    </WorkWellSeatContext.Provider>
  );
}

export function WorkWellHead({
  title,
  ident,
  seal,
  children,
  gap = "2.5",
  mark: markProp,
  titleRole: titleRoleProp,
}: {
  title?: string;
  ident?: string;
  /** Optional mark ahead of title copy (callers pass domain chrome such as a released-result seal). */
  seal?: ReactNode;
  children?: ReactNode;
  gap?: LayoutSpace;
  mark?: WorkWellHeadMark;
  titleRole?: WorkWellHeadTitleRole;
}) {
  const seat = useContext(WorkWellSeatContext);
  const mark = markProp ?? (seat === "stack" ? "title" : "span");
  const titleRole = titleRoleProp ?? (seat === "stack" ? "plate" : "task");

  if (children) {
    return (
      <Stack
        as="header"
        className="work-well__head"
        gap={gap}
        align="start"
        data-mark={mark}
        data-title-role={titleRole}
      >
        {children}
      </Stack>
    );
  }

  if (seal) {
    return (
      <Stack
        as="header"
        className="work-well__head"
        gap="none"
        align="start"
        data-mark={mark}
        data-title-role={titleRole}
      >
        {seal}
        <Stack className="work-well__copy" gap="2" align="start">
          {title ? <h2 className="work-well__title" data-title-role={titleRole}>{title}</h2> : null}
          {ident ? <p className="work-well__ident">{ident}</p> : null}
        </Stack>
      </Stack>
    );
  }

  return (
    <Stack as="header" className="work-well__head" gap="none" align="start" data-mark={mark} data-title-role={titleRole}>
      <Stack className="work-well__copy" gap={gap} align="start">
        {title ? <h2 className="work-well__title" data-title-role={titleRole}>{title}</h2> : null}
        {ident ? <p className="work-well__ident">{ident}</p> : null}
      </Stack>
    </Stack>
  );
}

export function WorkWellHint({ children }: { children: ReactNode }) {
  return <p className="work-well__hint">{children}</p>;
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
