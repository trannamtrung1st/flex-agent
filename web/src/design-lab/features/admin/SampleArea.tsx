import type { ReactNode } from "react";
import { OperateArea } from "../../components";
import { useAdminContext } from "./adminContext";
import { CampaignContext } from "./CampaignContext";

export function SampleArea({
  title,
  description,
  advisoryLabel,
  advisoryCopy,
  emptyLabel,
  emptyNote,
  campaignScoped,
  children,
}: {
  title: string;
  description: string;
  advisoryLabel: string;
  advisoryCopy: string;
  emptyLabel?: string;
  emptyNote?: string;
  campaignScoped?: boolean;
  children?: ReactNode;
}) {
  const { campaign } = useAdminContext();
  if (campaignScoped && !campaign) {
    return (
      <OperateArea
        className="campaigns-wall sample-wall"
        label={title}
        title={title}
        description={description}
        headClassName="campaigns-head"
        empty={{
          label: "Campaign not available",
          note: "This campaign is not available. Select an authorized campaign before inspecting this area.",
        }}
      />
    );
  }

  return (
    <OperateArea
      className="campaigns-wall sample-wall"
      label={title}
      title={title}
      description={description}
      headClassName="campaigns-head"
      frameClassName="campaigns-frame sample-frame"
      revealing
      advisory={{ label: advisoryLabel, copy: advisoryCopy }}
      context={campaignScoped ? <CampaignContext /> : undefined}
      empty={emptyLabel && emptyNote ? { label: emptyLabel, note: emptyNote, separated: Boolean(children) } : undefined}
    >
      {children}
    </OperateArea>
  );
}
