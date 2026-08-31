import { forwardRef, type ComponentPropsWithoutRef, type ElementType, type Ref } from "react";
import { cx } from "../../lib/cx";
import { Stack } from "../../design-system/components/layout/Stack";
import type { LayoutSpace } from "../../design-system/components/layout/types";
import type { PolymorphicProps } from "../../design-system/components/layout/polymorphic";
import { PlateFoot } from "../../design-system/components/plates/EtchedFrame";

type SetupCeremonyOwn = {
  /** Post-activation cohort; locks the ceremony shell. */
  frozen?: boolean;
  className?: string;
  gap?: LayoutSpace;
};

export const SetupCeremony = forwardRef(function SetupCeremony(
  { frozen, className, as, gap = "none", ...rest }: PolymorphicProps<ElementType, SetupCeremonyOwn>,
  ref: Ref<unknown>,
) {
  return (
    <Stack
      ref={ref}
      as={as}
      gap={gap}
      className={cx(
        "setup-ceremony",
        "plate-bleed",
        as === "form" && "workspace-form",
        frozen && "is-frozen",
        typeof className === "string" ? className : undefined,
      )}
      {...rest}
    />
  );
});

export function SetupCeremonyScroll({
  className,
  gap = "6",
  ...rest
}: Omit<ComponentPropsWithoutRef<typeof Stack>, "className" | "gap"> & {
  className?: string;
  gap?: LayoutSpace;
}) {
  return <Stack gap={gap} className={cx("create-ceremony__scroll", typeof className === "string" ? className : undefined)} {...rest} />;
}

export function SetupCeremonyFoot({ className, ...rest }: ComponentPropsWithoutRef<typeof PlateFoot>) {
  return <PlateFoot className={cx("setup-ceremony__foot", className)} {...rest} />;
}
