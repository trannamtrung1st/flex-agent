import type { ReactNode } from "react";
import { DropdownSelect } from "../select/listboxMenus";

export function DemoPlate({
  id,
  value,
  options,
  onChange,
  describedBy,
  note,
  plateLabel,
}: {
  id: string;
  value: string;
  options: { value: string; label: string }[];
  onChange: (value: string) => void;
  describedBy?: string;
  note?: ReactNode;
  plateLabel?: string;
}) {
  const labelId = `${id}Label`;

  return (
    <div className="demo-plate" aria-label={plateLabel}>
      <label className="demo-label" id={labelId} htmlFor={id}>
        Demo state
      </label>
      <DropdownSelect
        id={id}
        labelId={labelId}
        value={value}
        options={options}
        describedBy={describedBy}
        onChange={onChange}
      />
      {note}
    </div>
  );
}
