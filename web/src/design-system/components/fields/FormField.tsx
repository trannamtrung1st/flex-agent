import type { ReactNode } from "react";

export type FormFieldControlProps = {
  id: string;
  "aria-invalid": true | undefined;
  "aria-describedby": string | undefined;
};

export type FormFieldControlMeta = {
  labelId: string;
};

export type FormFieldLayout = "row" | "stack";

function formFieldLayoutClass(layout: FormFieldLayout) {
  if (layout === "stack") return "field-stack";
  return "form-row";
}

function resolveFormFieldClassName(
  layout: FormFieldLayout,
  className?: string,
  hostClassName?: string,
) {
  const host = hostClassName ?? formFieldLayoutClass(layout);
  if (!className) return host;
  if (
    !hostClassName &&
    (className === host ||
      className.startsWith(`${host} `))
  ) {
    return className;
  }
  return `${host} ${className}`;
}

export function FormField({
  id,
  label,
  error,
  hint,
  describedBy,
  invalid,
  layout = "row",
  className,
  hostClassName,
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
  /** Horizontal label row (default) or stacked label/control (dialogs, bulkheads). Compact pair cells replace the host class; they are not a layout. */
  layout?: FormFieldLayout;
  className?: string;
  /** Lab specimens only. Replaces the layout class bundle. Production pages must not pass this. */
  hostClassName?: string;
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
  const rootClassName = resolveFormFieldClassName(layout, className, hostClassName);

  return (
    <div className={rootClassName}>
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
