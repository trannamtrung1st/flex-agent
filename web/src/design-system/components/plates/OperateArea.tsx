import type { ReactNode } from "react";
import { Advisory, OperateHead } from "../chrome/OperateHead";
import { EmptyPlate, EtchedFrame } from "./EtchedFrame";

export function OperateArea({
  title,
  description,
  label,
  className,
  headClassName,
  frameClassName,
  revealing,
  sealing,
  back,
  titleTabIndex,
  advisory,
  context,
  empty,
  children,
}: {
  title: string;
  description?: string;
  label: string;
  className: string;
  headClassName?: string;
  frameClassName?: string;
  revealing?: boolean;
  sealing?: boolean;
  back?: ReactNode;
  titleTabIndex?: number;
  advisory?: { label: string; copy: string; attention?: boolean };
  context?: ReactNode;
  empty?: { label: string; note: string; separated?: boolean };
  children?: ReactNode;
}) {
  const hasEmpty = Boolean(empty);
  const hasChildren = Boolean(children);
  const hasBody = hasChildren || hasEmpty;

  return (
    <main className={className} aria-label={label}>
      <OperateHead
        className={headClassName}
        title={title}
        description={description}
        back={back}
        titleTabIndex={titleTabIndex}
      />
      {context}
      {advisory ? <Advisory label={advisory.label} copy={advisory.copy} attention={advisory.attention} /> : null}
      {hasBody ? (
        <EtchedFrame className={frameClassName} revealing={revealing} sealing={sealing}>
          {children}
          {empty ? (
            <EmptyPlate
              className={empty.separated ? "empty-plate--inset empty-plate--separated" : "empty-plate--inset"}
              label={empty.label}
              note={empty.note}
            />
          ) : null}
        </EtchedFrame>
      ) : null}
    </main>
  );
}
