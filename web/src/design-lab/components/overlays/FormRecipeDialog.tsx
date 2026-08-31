import type { ComponentPropsWithoutRef, ReactNode } from "react";
import { cx } from "../../../lib/cx";

export function FormRecipeDialogWell({ children, className }: { children: ReactNode; className?: string }) {
  return <div className={cx("form-recipe-dialog-well", className)}>{children}</div>;
}

export function FormRecipeDialog({
  children,
  className,
  ...rest
}: ComponentPropsWithoutRef<"form">) {
  return (
    <form className={cx("form-recipe-dialog", className)} noValidate {...rest}>
      {children}
    </form>
  );
}
