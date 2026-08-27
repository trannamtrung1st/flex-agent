import type { ReactNode } from "react";

export type FormFieldControlProps = {
  id: string;
  "aria-invalid": true | undefined;
  "aria-describedby": string | undefined;
};

export type FormFieldControlMeta = {
  labelId: string;
};

export function FormField({
  id,
  label,
  error,
  hint,
  describedBy,
  invalid,
  className = "form-row",
  labelClassName = "field-label",
  errorClassName = "field-error",
  hintClassName = "field-hint",
  labelAssociatesControl = true,
  children,
}: {
  id: string;
  label: ReactNode;
  error?: ReactNode;
  hint?: ReactNode;
  describedBy?: string;
  invalid?: boolean;
  className?: string;
  labelClassName?: string;
  errorClassName?: string;
  hintClassName?: string;
  /** Set false for disclosure controls (select, date picker) — htmlFor toggles the trigger and fights outside-dismiss. */
  labelAssociatesControl?: boolean;
  children: (props: FormFieldControlProps, meta: FormFieldControlMeta) => ReactNode;
}) {
  const labelId = `${id}Label`;
  const hintId = `${id}Hint`;
  const errorId = `${id}Error`;
  const isInvalid = invalid ?? Boolean(error);
  const ariaDescribedBy = [describedBy, hint ? hintId : null, error ? errorId : null].filter(Boolean).join(" ") || undefined;

  return (
    <div className={className}>
      {labelAssociatesControl ? (
        <label className={labelClassName} id={labelId} htmlFor={id}>
          {label}
        </label>
      ) : (
        <span className={labelClassName} id={labelId}>
          {label}
        </span>
      )}
      {children(
        {
          id,
          "aria-invalid": isInvalid || undefined,
          "aria-describedby": ariaDescribedBy,
        },
        { labelId },
      )}
      {hint ? (
        <p className={hintClassName} id={hintId}>
          {hint}
        </p>
      ) : null}
      {error ? (
        <p className={errorClassName} id={errorId}>
          {error}
        </p>
      ) : null}
    </div>
  );
}
