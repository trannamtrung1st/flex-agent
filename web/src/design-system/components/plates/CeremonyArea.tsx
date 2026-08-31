import type { ReactNode } from "react";
import type { To } from "react-router-dom";
import { WaitPlate } from "../feedback/WaitPlate";
import { Key } from "../keys";
import { EmptyPlate } from "./EtchedFrame";
import { OperateArea, type OperateHugMeasure } from "./OperateArea";

export type CeremonyRecovery = {
  label: string;
  to?: To;
  onClick?: () => void;
  disabled?: boolean;
  /** Quiet Return/Reload by default. `transmit` is the auth commit (`Continue to sign in`). */
  variant?: "quiet" | "transmit";
};

export function CeremonyWait({
  label,
  note,
}: {
  label: string;
  note?: string;
}) {
  return <WaitPlate className="ceremony-wait" inset label={label} note={note} />;
}

export function CeremonyEmpty({
  note,
  children,
  alert,
}: {
  note: string;
  children?: ReactNode;
  alert?: boolean;
}) {
  return (
    <EmptyPlate className="ceremony-empty" inset note={note} noteRole={alert ? "alert" : undefined}>
      {children}
    </EmptyPlate>
  );
}

export function CeremonyArea({
  title,
  description,
  label,
  danger,
  hugMeasure = "auto",
  children,
}: {
  title: string;
  description?: string;
  label: string;
  danger?: boolean;
  hugMeasure?: OperateHugMeasure;
  children?: ReactNode;
}) {
  return (
    <OperateArea
      composition="hug"
      hugMeasure={hugMeasure}
      bay="ceremony"
      danger={danger}
      frame="ceremony"
      label={label}
      title={title}
      description={description}
    >
      {children}
    </OperateArea>
  );
}

export function CeremonyUnavailable({
  title,
  description,
  label,
  note,
  recovery,
  danger,
  hugMeasure,
  alert,
}: {
  title: string;
  description?: string;
  label?: string;
  note: string;
  recovery?: CeremonyRecovery;
  danger?: boolean;
  hugMeasure?: OperateHugMeasure;
  alert?: boolean;
}) {
  return (
    <CeremonyArea
      label={label ?? title}
      title={title}
      description={description}
      danger={danger}
      hugMeasure={hugMeasure}
    >
      <CeremonyEmpty note={note} alert={alert}>
        {recovery ? (
          <Key
            variant={recovery.variant ?? "quiet"}
            size={recovery.variant === "transmit" ? "large" : "standard"}
            to={recovery.to}
            onClick={recovery.onClick}
            disabled={recovery.disabled}
          >
            {recovery.label}
          </Key>
        ) : null}
      </CeremonyEmpty>
    </CeremonyArea>
  );
}
