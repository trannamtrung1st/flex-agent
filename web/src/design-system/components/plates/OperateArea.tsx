import type { ReactNode } from "react";
import { Advisory, OperateHead } from "../chrome/OperateHead";
import { Stack } from "../layout/Stack";
import { EmptyPlate, EtchedFrame, type EtchedFrameInset } from "./EtchedFrame";

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
}) {
  const hasEmpty = Boolean(empty);
  const hasChildren = Boolean(children);
  const hasBody = hasChildren || hasEmpty;

  return (
    <Stack as="section" className={className} aria-label={label} hidden={hidden}>
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
      {context}
      {advisory ? <Advisory label={advisory.label} copy={advisory.copy} attention={advisory.attention} /> : null}
      {hasBody ? (
        framed ? (
          <EtchedFrame className={frameClassName} inset={frameInset} revealing={revealing} sealing={sealing}>
            {children}
            {empty ? (
              <EmptyPlate
                className={empty.separated ? "empty-plate--inset empty-plate--separated" : "empty-plate--inset"}
                label={empty.label}
                note={empty.note}
              />
            ) : null}
          </EtchedFrame>
        ) : (
          <>
            {children}
            {empty ? (
              <EmptyPlate
                className={empty.separated ? "empty-plate--inset empty-plate--separated" : "empty-plate--inset"}
                label={empty.label}
                note={empty.note}
              />
            ) : null}
          </>
        )
      ) : null}
    </Stack>
  );
}
