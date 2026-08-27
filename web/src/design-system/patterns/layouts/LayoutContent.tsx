import type { ReactNode, Ref } from "react";
import { Inset } from "../../components/layout/Inset";
import { SHELL_MAIN_INSET_BLOCK, SHELL_MAIN_INSET_CLASS, SHELL_MAIN_INSET_INLINE } from "./shellInset";

type LayoutContentProps = {
  nested?: boolean;
  className: string;
  children: ReactNode;
  label?: string;
  contentRef?: Ref<HTMLElement>;
  contain?: boolean;
};

export function LayoutContent({
  nested,
  className,
  children,
  label,
  contentRef,
  contain = false,
}: LayoutContentProps) {
  const body = contain ? (
    <Inset
      className={SHELL_MAIN_INSET_CLASS}
      inline={SHELL_MAIN_INSET_INLINE}
      block={SHELL_MAIN_INSET_BLOCK}
    >
      {children}
    </Inset>
  ) : (
    children
  );

  if (nested) {
    return (
      <div className={className} aria-label={label}>
        {body}
      </div>
    );
  }

  return (
    <main id="main-content" ref={contentRef} className={className} aria-label={label}>
      {body}
    </main>
  );
}
