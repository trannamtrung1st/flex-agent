import type { ComponentProps } from "react";
import { OperateArea } from "../../../design-system/components/plates/OperateArea";
import { cx } from "../../../lib/cx";

type FormRecipeOperateAreaProps = Omit<ComponentProps<typeof OperateArea>, "bay">;

export function FormRecipeOperateArea({ className, ...rest }: FormRecipeOperateAreaProps) {
  return <OperateArea {...rest} bay="workspace" className={cx("form-recipe", className)} />;
}
