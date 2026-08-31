import type { ComponentProps } from "react";
import { OperateHead } from "../../../design-system/components/chrome/OperateHead";
import { OperateAreaHost } from "../../../design-system/components/plates/OperateArea";

type ReviewerLedgerOperateAreaProps = Omit<
  ComponentProps<typeof OperateAreaHost>,
  "bay" | "hostClassName" | "head" | "framed" | "gap"
>;

export function ReviewerLedgerOperateArea({
  title,
  description,
  back,
  titleTabIndex,
  headExtra,
  headClassName,
  headed = true,
  ...rest
}: ReviewerLedgerOperateAreaProps) {
  return (
    <OperateAreaHost
      {...rest}
      hostClassName="workspace-area record-view"
      framed={false}
      gap="none"
      headed={false}
      head={
        headed && title ? (
          <OperateHead
            arrangement="plaque"
            className={headClassName}
            title={title}
            description={description}
            back={back}
            titleTabIndex={titleTabIndex}
            headExtra={headExtra}
          />
        ) : null
      }
    />
  );
}
