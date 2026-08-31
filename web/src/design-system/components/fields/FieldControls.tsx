import { forwardRef, type InputHTMLAttributes, type TextareaHTMLAttributes } from "react";

export type FieldWidth = "standard" | "narrow" | "wide";
export type FieldCasing = "authored" | "uppercase";
export type FieldTextareaResize = "none" | "vertical" | "both";

function fieldWidthClass(width?: FieldWidth) {
  if (width === "narrow") return "field-input--narrow";
  if (width === "wide") return "field-input--wide";
  return undefined;
}

function fieldCasingClass(casing?: FieldCasing) {
  if (casing === "uppercase") return "field-input--uppercase";
  return undefined;
}

function isAriaInvalid(value: InputHTMLAttributes<HTMLInputElement>["aria-invalid"]) {
  return value === true || value === "true";
}

function fieldControlClass({
  base,
  width,
  casing,
  invalid,
  frozen,
  className,
  ariaInvalid,
}: {
  base: string;
  width?: FieldWidth;
  casing?: FieldCasing;
  invalid?: boolean;
  frozen?: boolean;
  className?: string;
  ariaInvalid?: InputHTMLAttributes<HTMLInputElement>["aria-invalid"];
}) {
  return [
    base,
    fieldWidthClass(width),
    fieldCasingClass(casing),
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
    /** Token slots only. Default preserves authored case (titles, names, captions). */
    casing?: FieldCasing;
    invalid?: boolean;
    frozen?: boolean;
    /** Format example. Required; never a substitute for the visible label. */
    placeholder: string;
  }
>(function FieldInput({ width = "standard", casing = "authored", invalid, frozen, className, readOnly, ...props }, ref) {
  return (
    <input
      {...props}
      ref={ref}
      readOnly={frozen || readOnly}
      className={fieldControlClass({
        base: "field-input",
        width,
        casing,
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
    /** Format example. Required; never a substitute for the visible label. */
    placeholder: string;
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
