import type { ReactNode } from "react";
import { Advisory, OperateHead } from "../chrome/OperateHead";
import { Stack } from "../layout/Stack";
import type { LayoutSpace } from "../layout/types";
import { EmptyPlate, EtchedFrame, type EtchedFrameInset } from "./EtchedFrame";
import { operateAreaClass, type OperateBay, type OperateHug } from "./operateAreaClass";
import { operateFrameClass, resolveOperateFrameInset, type OperateFrame } from "./operateFrameClass";

export type { OperateBay, OperateHug } from "./operateAreaClass";
export type { OperateFrame } from "./operateFrameClass";
export { operateAreaClass, registryTableHug, REGISTRY_TABLE_HUG_MAX_ROWS } from "./operateAreaClass";
export { operateFrameClass, resolveOperateFrameInset } from "./operateFrameClass";

export type OperateHugMeasure = "auto" | "sm" | "md" | "lg";

export type OperateAreaProps = {
  title?: string;
  description?: string;
  label: string;
  bay?: OperateBay;
  /** Production `bay="registry"` only. Lab wrappers add `registry-wall--hug` themselves. */
  hug?: OperateHug;
  danger?: boolean;
  className?: string;
  frame?: OperateFrame;
  frameInset?: EtchedFrameInset;
  hidden?: boolean;
  framed?: boolean;
  back?: ReactNode;
  titleTabIndex?: number;
  advisory?: { label: string; copy: string; attention?: boolean };
  context?: ReactNode;
  headExtra?: ReactNode;
  empty?: { label: string; note: string; separated?: boolean };
  children?: ReactNode;
  headed?: boolean;
  composition?: "fill" | "hug";
  hugMeasure?: OperateHugMeasure;
};

/** Domain/lab wrappers only. Production pages use `OperateArea`. */
export type OperateAreaHostProps = OperateAreaProps & {
  /** Replaces the `bay` host bundle. */
  hostClassName?: string;
  headClassName?: string;
  frameClassName?: string;
  revealing?: boolean;
  sealing?: boolean;
  /** Replaces the default stacked OperateHead. */
  head?: ReactNode;
  /** Default bay gap; hug composition always uses none. */
  gap?: LayoutSpace;
};

export function OperateArea(props: OperateAreaProps) {
  return <OperateAreaHost {...props} />;
}

export function OperateAreaHost({
  title,
  description,
  label,
  bay = "workspace",
  hug,
  danger,
  className,
  hostClassName,
  headClassName,
  frame,
  frameClassName,
  frameInset,
  revealing,
  sealing,
  back,
  titleTabIndex,
  advisory,
  context,
  headExtra,
  empty,
  children,
  hidden,
  framed = true,
  headed = true,
  head,
  gap,
  composition = "fill",
  hugMeasure = "auto",
}: OperateAreaHostProps) {
  const resolvedFrameClassName = operateFrameClass(frame, frameClassName);
  const resolvedFrameInset = resolveOperateFrameInset(frame, frameInset);

  const hasEmpty = Boolean(empty);
  const hasChildren = Boolean(children);
  const hasBody = hasChildren || hasEmpty;
  const emptyPlate = empty ? (
    <EmptyPlate
      inset
      className={empty.separated ? "empty-plate--separated" : undefined}
      label={empty.label}
      note={empty.note}
    />
  ) : null;

  const body = hasBody ? (
    framed ? (
      <EtchedFrame
        className={resolvedFrameClassName}
        inset={resolvedFrameInset}
        revealing={revealing}
        sealing={sealing}
      >
        {children}
        {emptyPlate}
      </EtchedFrame>
    ) : (
      <>
        {children}
        {emptyPlate}
      </>
    )
  ) : null;

  const rest = (
    <>
      {context}
      {advisory ? <Advisory label={advisory.label} copy={advisory.copy} attention={advisory.attention} /> : null}
      {body}
    </>
  );

  const stackGap = composition === "hug" ? "none" : (gap ?? "6");
  const operateHead = head ?? (headed && title ? (
    <OperateHead
      className={headClassName}
      title={title}
      description={description}
      back={back}
      titleTabIndex={titleTabIndex}
      headExtra={headExtra}
    />
  ) : null);

  const plane = (
    <>
      {operateHead}
      {composition === "hug" ? rest : (
        <>
          {context}
          {advisory ? <Advisory label={advisory.label} copy={advisory.copy} attention={advisory.attention} /> : null}
          {hasBody ? <div className="operate-scroll">{body}</div> : null}
        </>
      )}
    </>
  );

  return (
    <Stack
      as="section"
      className={operateAreaClass(bay, { hug, danger, className, hostClassName })}
      aria-label={label}
      hidden={hidden}
      gap={stackGap}
    >
      {composition === "hug" ? (
        <div className="operate-column operate-column--hug" data-hug-measure={hugMeasure}>
          {plane}
        </div>
      ) : (
        plane
      )}
    </Stack>
  );
}
