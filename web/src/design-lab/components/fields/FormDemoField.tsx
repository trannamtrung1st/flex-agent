import type { ComponentProps } from "react";
import { cx } from "../../../lib/cx";
import { FormField } from "../../../design-system";

type FormDemoFieldProps = Omit<ComponentProps<typeof FormField>, "hostClassName" | "layout"> & {
  fit?: boolean;
};

export function FormDemoField({ className, fit, ...rest }: FormDemoFieldProps) {
  return (
    <FormField
      {...rest}
      layout="row"
      hostClassName={cx("form-demo-row", fit && "form-demo-row--fit")}
      className={className}
    />
  );
}
