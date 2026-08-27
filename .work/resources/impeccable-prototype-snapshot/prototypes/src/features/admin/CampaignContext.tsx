import { SearchableDisclosureMenu } from "../../components";
import { useAdminContext } from "./adminContext";
import { ActivationReadout, ReadoutBand } from "./ReadoutBand";

export function CampaignContext() {
  const { campaign, campaigns, setCampaignId, announce } = useAdminContext();

  if (!campaign) return null;

  const onSelect = (id: string) => {
    if (id === campaign.id) return;
    const next = campaigns.find((item) => item.id === id);
    setCampaignId(id);
    announce(
      next
        ? `Campaign ${next.id} selected. ${next.name}.`
        : `Campaign ${id} selected.`,
    );
  };

  return (
    <ReadoutBand label="Campaign context" className="campaign-context">
      <SearchableDisclosureMenu
        keyId="campaignKey"
        menuId="campaignMenu"
        valueId="campaignValue"
        label="Campaign"
        value={`${campaign.id} / ${campaign.name}`}
        selectedId={campaign.id}
        ariaLabel="Select campaign"
        searchPlaceholder="Filter campaigns"
        optionNoun="campaign"
        emptyMessage="No campaigns match this filter. Revise the search term."
        variant="context"
        options={campaigns.map((item) => ({
          id: item.id,
          label: `${item.id} / ${item.name}`,
        }))}
        onSelect={onSelect}
      />
      <ActivationReadout frozen={campaign.frozen} />
    </ReadoutBand>
  );
}
