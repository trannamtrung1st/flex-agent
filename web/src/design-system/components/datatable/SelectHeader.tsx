import { forwardRef, type ComponentPropsWithoutRef } from "react";
import { HeaderSelectionControl } from "../../patterns/TableActions";

export const SelectHeader = forwardRef<
  HTMLInputElement,
  ComponentPropsWithoutRef<typeof HeaderSelectionControl>
>(function SelectHeader(props, ref) {
  return (
    <th scope="col" className="col-select">
      <HeaderSelectionControl ref={ref} {...props} />
    </th>
  );
});
