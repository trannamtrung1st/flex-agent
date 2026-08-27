import type { ChangeEvent, ReactNode } from "react";

export function ControlLine({
  id,
  name,
  type = "checkbox",
  role,
  value,
  checked,
  defaultChecked,
  disabled,
  className,
  markClassName,
  onChange,
  children,
}: {
  id?: string;
  name?: string;
  type?: "checkbox" | "radio";
  role?: string;
  value?: string;
  checked?: boolean;
  defaultChecked?: boolean;
  disabled?: boolean;
  className?: string;
  markClassName: string;
  onChange?: (checked: boolean, event: ChangeEvent<HTMLInputElement>) => void;
  children: ReactNode;
}) {
  return (
    <label className={["control-line", className].filter(Boolean).join(" ")}>
      <input
        type={type}
        id={id}
        name={name}
        role={role}
        value={value}
        checked={checked}
        defaultChecked={defaultChecked}
        disabled={disabled}
        onChange={onChange ? (event) => onChange(event.target.checked, event) : undefined}
      />
      <span className={markClassName} aria-hidden="true" />
      <span>{children}</span>
    </label>
  );
}

export function RadioGroup({
  legend,
  name,
  value,
  onChange,
  options,
}: {
  legend: ReactNode;
  name: string;
  value: string;
  onChange: (value: string) => void;
  options: readonly { value: string; label: ReactNode; id?: string }[];
}) {
  return (
    <fieldset className="radio-group">
      <legend className="field-label">{legend}</legend>
      {options.map((option) => (
        <ControlLine
          key={option.value}
          type="radio"
          name={name}
          id={option.id}
          value={option.value}
          checked={value === option.value}
          markClassName="radio-mark"
          onChange={() => onChange(option.value)}
        >
          {option.label}
        </ControlLine>
      ))}
    </fieldset>
  );
}

export function Breaker({
  id,
  checked,
  defaultChecked,
  disabled,
  className,
  onChange,
  children,
}: {
  id?: string;
  checked?: boolean;
  defaultChecked?: boolean;
  disabled?: boolean;
  className?: string;
  onChange?: (checked: boolean) => void;
  children: ReactNode;
}) {
  return (
    <ControlLine
      id={id}
      role="switch"
      checked={checked}
      defaultChecked={defaultChecked}
      disabled={disabled}
      className={className}
      markClassName="breaker"
      onChange={onChange}
    >
      {children}
    </ControlLine>
  );
}
