import { forwardRef, useRef, type InputHTMLAttributes, type Ref } from "react";
import { cx } from "../../../lib/cx";
import { ChevronGlyph } from "../glyphs";
import { FieldInput, type FieldWidth } from "./FieldControls";
import { SCORE_PLACEHOLDER } from "./fieldFormat";
import { stepNumberFieldValue, toFiniteNumber } from "./numberFieldValue";

export type FieldNumberProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  width?: FieldWidth;
  invalid?: boolean;
  frozen?: boolean;
  /** Distinguishes Increase/Decrease when several number fields share a view. */
  stepperLabel?: string;
};

function assignRef<T>(ref: Ref<T> | undefined, node: T | null) {
  if (typeof ref === "function") {
    ref(node);
    return;
  }
  if (ref) {
    ref.current = node;
  }
}

function setNativeInputValue(input: HTMLInputElement, value: string) {
  input.value = value;
  input.dispatchEvent(new Event("input", { bubbles: true }));
  input.dispatchEvent(new Event("change", { bubbles: true }));
}

export const FieldNumber = forwardRef<HTMLInputElement, FieldNumberProps>(function FieldNumber(
  {
    width = "standard",
    invalid,
    frozen,
    className,
    disabled,
    onChange,
    stepperLabel,
    size,
    placeholder = SCORE_PLACEHOLDER,
    ...props
  },
  ref,
) {
  const inputRef = useRef<HTMLInputElement>(null);
  const min = toFiniteNumber(props.min);
  const max = toFiniteNumber(props.max);
  const step = toFiniteNumber(props.step) ?? 1;
  const increaseLabel = stepperLabel ? `Increase ${stepperLabel}` : "Increase";
  const decreaseLabel = stepperLabel ? `Decrease ${stepperLabel}` : "Decrease";

  const stepBy = (direction: 1 | -1) => {
    const input = inputRef.current;
    if (!input || input.disabled || input.readOnly) return;
    const next = stepNumberFieldValue(input.value, direction, { min, max, step });
    setNativeInputValue(input, next);
  };

  const widthClass = width === "narrow" ? "field-input--narrow" : width === "wide" ? "field-input--wide" : undefined;

  return (
    <div
      className={cx(
        "field-number",
        widthClass,
        invalid || props["aria-invalid"] === true || props["aria-invalid"] === "true" ? "is-invalid" : undefined,
        frozen ? "is-frozen" : undefined,
        className,
      )}
    >
      <FieldInput
        {...props}
        ref={(node) => {
          inputRef.current = node;
          assignRef(ref, node);
        }}
        type="number"
        size={size ?? 4}
        placeholder={placeholder}
        invalid={invalid}
        frozen={frozen}
        disabled={disabled}
        onChange={onChange}
        className="field-number-input"
      />
      {frozen ? null : (
        <div className="field-number-steps">
          <button
            type="button"
            className="field-number-step field-number-step--inc"
            tabIndex={-1}
            disabled={disabled}
            aria-label={increaseLabel}
            onClick={() => stepBy(1)}
          >
            <ChevronGlyph />
          </button>
          <button
            type="button"
            className="field-number-step field-number-step--dec"
            tabIndex={-1}
            disabled={disabled}
            aria-label={decreaseLabel}
            onClick={() => stepBy(-1)}
          >
            <ChevronGlyph />
          </button>
        </div>
      )}
    </div>
  );
});
