import type { ComponentProps, ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { FormField } from "../../../design-system";

/** Horizontally paired fields inside a titled cluster (`form-row--pair`). */
export function FormPair({ children, className }: { children: ReactNode; className?: string }) {
  return <div className={cx("form-row", "form-row--pair", className)}>{children}</div>;
}

type FormPairFieldProps = Omit<ComponentProps<typeof FormField>, "hostClassName" | "layout">;

/** Compact pair cell (`.field-pair`). Use inside `FormPair`, not `FormField` `layout`. */
export function FormPairField(props: FormPairFieldProps) {
  return <FormField {...props} hostClassName="field-pair" />;
}
