import type { ReactNode } from "react";

export type FormFieldControlProps = {
  id: string;
  "aria-invalid": true | undefined;
  "aria-describedby": string | undefined;
};

export type FormFieldControlMeta = {
  labelId: string;
};

export type FormFieldLayout = "row" | "stack" | "pair";

function formFieldLayoutClass(layout: FormFieldLayout) {
  if (layout === "stack") return "field-stack";
  if (layout === "pair") return "field-pair";
  return "form-row";
}

function resolveFormFieldClassName(layout: FormFieldLayout, className?: string) {
  if (!className) return formFieldLayoutClass(layout);
  if (
    className === "form-row" ||
    className === "field-stack" ||
    className === "field-pair" ||
    className.startsWith("form-row ") ||
    className.startsWith("field-stack ") ||
    className.startsWith("field-pair ") ||
    className === "form-demo-row" ||
    className.startsWith("form-demo-row ")
  ) {
    return className;
  }
  return `${formFieldLayoutClass(layout)} ${className}`;
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
  /** Horizontal label row (default), stacked label/control (dialogs, bulkheads), or compact pair grid. */
  layout?: FormFieldLayout;
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
  const rootClassName = resolveFormFieldClassName(layout, className);

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
