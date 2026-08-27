import { forwardRef, type InputHTMLAttributes, type TextareaHTMLAttributes } from "react";

export type FieldWidth = "standard" | "narrow" | "wide";
export type FieldTextareaResize = "none" | "vertical" | "both";

function fieldWidthClass(width?: FieldWidth) {
  if (width === "narrow") return "field-input--narrow";
  if (width === "wide") return "field-input--wide";
  return undefined;
}

function isAriaInvalid(value: InputHTMLAttributes<HTMLInputElement>["aria-invalid"]) {
  return value === true || value === "true";
}

function fieldControlClass({
  base,
  width,
  invalid,
  frozen,
  className,
  ariaInvalid,
}: {
  base: string;
  width?: FieldWidth;
  invalid?: boolean;
  frozen?: boolean;
  className?: string;
  ariaInvalid?: InputHTMLAttributes<HTMLInputElement>["aria-invalid"];
}) {
  return [
    base,
    fieldWidthClass(width),
    invalid || isAriaInvalid(ariaInvalid) ? "is-invalid" : undefined,
    frozen ? "is-frozen" : undefined,
    className,
  ]
    .filter(Boolean)
    .join(" ");
}

export const FieldInput = forwardRef<
  HTMLInputElement,
  InputHTMLAttributes<HTMLInputElement> & {
    width?: FieldWidth;
    invalid?: boolean;
    frozen?: boolean;
  }
>(function FieldInput({ width = "standard", invalid, frozen, className, readOnly, ...props }, ref) {
  return (
    <input
      {...props}
      ref={ref}
      readOnly={frozen || readOnly}
      className={fieldControlClass({
        base: "field-input",
        width,
        invalid,
        frozen,
        className,
        ariaInvalid: props["aria-invalid"],
      })}
    />
  );
});

export const FieldTextarea = forwardRef<
  HTMLTextAreaElement,
  TextareaHTMLAttributes<HTMLTextAreaElement> & {
    invalid?: boolean;
    frozen?: boolean;
    resize?: FieldTextareaResize;
  }
>(function FieldTextarea({ invalid, frozen, resize = "none", className, readOnly, ...props }, ref) {
  const resizeClass =
    resize === "vertical" ? "field-textarea--resize-y" : resize === "both" ? "field-textarea--resize-both" : undefined;

  return (
    <textarea
      {...props}
      ref={ref}
      readOnly={frozen || readOnly}
      className={fieldControlClass({
        base: "field-textarea",
        invalid,
        frozen,
        className: [resizeClass, className].filter(Boolean).join(" ") || undefined,
        ariaInvalid: props["aria-invalid"],
      })}
    />
  );
});
