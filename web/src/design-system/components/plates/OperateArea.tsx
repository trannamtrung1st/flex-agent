import type { ReactNode } from "react";
import { Advisory, OperateHead } from "../chrome/OperateHead";
import { Stack } from "../layout/Stack";
import { EmptyPlate, EtchedFrame, type EtchedFrameInset } from "./EtchedFrame";

export type OperateHugMeasure = "auto" | "sm" | "md" | "lg";

export function OperateArea({
  title,
  description,
  label,
  className,
  headClassName,
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
  headArrangement = "stack",
  composition = "fill",
  hugMeasure = "auto",
}: {
  title?: string;
  description?: string;
  label: string;
  className: string;
  headClassName?: string;
  frameClassName?: string;
  frameInset?: EtchedFrameInset;
  revealing?: boolean;
  sealing?: boolean;
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
  headArrangement?: "stack" | "plaque";
  composition?: "fill" | "hug";
  hugMeasure?: OperateHugMeasure;
}) {
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
      <EtchedFrame className={frameClassName} inset={frameInset} revealing={revealing} sealing={sealing}>
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

  const plane = (
    <>
      {headed && title ? (
        <OperateHead
          className={headClassName}
          arrangement={headArrangement}
          title={title}
          description={description}
          back={back}
          titleTabIndex={titleTabIndex}
          headExtra={headExtra}
        />
      ) : null}
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
      className={className}
      aria-label={label}
      hidden={hidden}
      gap={headArrangement === "plaque" || composition === "hug" ? "none" : "6"}
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
