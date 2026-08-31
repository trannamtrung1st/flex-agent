import type { ReactNode } from "react";

export type IntakeItemRow = {
  id: string;
  label: ReactNode;
  detail: ReactNode;
};

export function IntakeItemList({
  items,
  label,
}: {
  items: readonly IntakeItemRow[];
  label: string;
}) {
  return (
    <ul className="intake-item-list" aria-label={label}>
      {items.map((item) => (
        <li className="intake-item-row" key={item.id}>
          <span>{item.label}</span>
          <span>{item.detail}</span>
        </li>
      ))}
    </ul>
  );
}
