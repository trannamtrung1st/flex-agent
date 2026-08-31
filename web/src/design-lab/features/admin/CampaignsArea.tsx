import { useMemo, useState } from "react";
import {
  ActivationMark,
  BackKey,
  EmptyPlate,
  FrozenLine,
  InPlateHost,
  Key,
  registryTableHug,
  PlateFoot,
  ReadoutGrid,
  ReadoutGridField,
  ReadoutGridRow,
  useTableActionRunner,
} from "../../components";
import { CampaignsOperateArea, CampaignsUnavailableWell } from "../../components/operate";
import { EMPTY_SELECTION, removeIds } from "../../../design-system/patterns/tableSelection";
import type { Campaign, CampaignRegistryState } from "../../data/types";
import type { CampaignForm } from "./campaignSchema";
import { useAdminContext } from "./adminContext";
import { createCampaignActions } from "./campaignActions";
import { CampaignConfigDialog } from "./CampaignConfigDialog";
import { CampaignRegistry } from "./CampaignRegistry";
import { campaignQueryKey, matchingCampaignIds, toRegistryRow } from "./campaignRegistryLogic";

const EMPTY_REGISTRY: CampaignRegistryState = {
  search: "",
  activationFilter: "all",
  sorts: [{ key: "campaign", dir: "asc" }],
  page: 0,
  pageSize: 16,
  selection: EMPTY_SELECTION,
};

export function CampaignsArea() {
  const { campaigns, campaign, campaignId, setCampaigns, setCampaignId, announce, pushToast } = useAdminContext();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [registry, setRegistry] = useState<CampaignRegistryState>(EMPTY_REGISTRY);
  const runner = useTableActionRunner<Campaign>("campaignSelectAll");

  const actions = useMemo(
    () => {
      const base = createCampaignActions({
        configure: (next) => {
          setCampaignId(next.id);
          setDialogOpen(true);
        },
        deleteCampaigns: (removed) => {
          const ids = new Set(removed.map((item) => item.id));
          setCampaigns((list) => list.filter((item) => !ids.has(item.id)));
          setRegistry((prev) => {
            const remaining = campaigns.filter((item) => !ids.has(item.id)).map(toRegistryRow);
            return {
              ...prev,
              selection: removeIds(
                prev.selection,
                [...ids],
                matchingCampaignIds(remaining, prev),
                campaignQueryKey(prev),
              ),
            };
          });
          if (campaignId && ids.has(campaignId)) setCampaignId(null);
        },
      });
      return base.map((action) => ({
        ...action,
        run: async (records: Campaign[]) => {
          const result = await action.run(records);
          if (result.ok) {
            if (result.message) {
              pushToast({ label: result.label ?? action.label, copy: result.message });
              announce(result.message);
            }
          } else {
            pushToast({ label: action.label, copy: result.message, attention: true });
            announce(result.message);
          }
          return result;
        },
      }));
    },
    [announce, campaignId, campaigns, pushToast, setCampaignId, setCampaigns],
  );

  const applyRegistry = (patch: Partial<CampaignRegistryState> | ((prev: CampaignRegistryState) => CampaignRegistryState)) => {
    setRegistry((prev) => (typeof patch === "function" ? patch(prev) : { ...prev, ...patch }));
  };

  const openCampaign = (id: string) => {
    setCampaignId(id);
    const next = campaigns.find((c) => c.id === id);
    announce(next ? `Opened campaign record ${next.id} / ${next.name}.` : `Opened campaign ${id}.`);
  };

  const backToRegistry = () => {
    setCampaignId(null);
    announce("Returned to campaign registry.");
  };

  if (!campaignId) {
    return (
      <CampaignsOperateArea
        variant="registry"
        label="Campaign registry"
        title="Campaign Registry"
        description="Find a campaign, then open its record to inspect or configure."
        titleTabIndex={-1}
        revealing
        hug={registryTableHug(matchingCampaignIds(campaigns.map(toRegistryRow), registry).length)}
      >
        <CampaignRegistry
            rows={campaigns.map(toRegistryRow)}
            campaigns={campaigns}
            state={registry}
            setState={applyRegistry}
            announce={announce}
            onOpen={openCampaign}
            actions={actions}
            onChoose={(action, records, trigger) => runner.choose(action, records, trigger)}
            busyActionId={runner.busyActionId}
            confirm={{
              open: Boolean(runner.pending),
              confirmation: runner.confirmation,
              error: runner.error,
              waiting: runner.running,
              onCancel: runner.cancel,
              onConfirm: runner.confirm,
            }}
          />
      </CampaignsOperateArea>
    );
  }

  if (!campaign) {
    return (
      <CampaignsOperateArea
        variant="plain"
        label="Campaign unavailable"
        title="Campaign Record"
        description="No campaign matches this address."
        back={<BackKey label="Campaigns" onClick={backToRegistry} />}
      >
        <CampaignsUnavailableWell>
          <EmptyPlate
            inset
            label="Campaign not found"
            note="This campaign is not available. Return to the registry and open a listed campaign."
          >
            <Key onClick={backToRegistry}>Back to campaigns</Key>
          </EmptyPlate>
        </CampaignsUnavailableWell>
      </CampaignsOperateArea>
    );
  }

  const { config } = campaign;

  const activate = (values: CampaignForm) => {
    setCampaigns((list) =>
      list.map((c) => (c.id === campaign.id ? { ...c, frozen: true, config: values } : c)),
    );
    announce(`Campaign ${campaign.id} activated in this design lab. Local freeze is not server authority.`);
  };

  return (
    <>
      <CampaignsOperateArea
        variant="record"
        label="Campaign configuration"
        title="Campaign Record"
        description={`Configuration and activation for ${campaign.id} / ${campaign.name}.`}
        revealing
        back={<BackKey label="Campaigns" onClick={backToRegistry} />}
      >
          <InPlateHost>
          <ReadoutGrid label="Campaign record">
            <ReadoutGridRow label="Campaign summary">
              <ReadoutGridField term="Campaign" span={3}>
                {campaign.id} / {campaign.name}
              </ReadoutGridField>
              <ReadoutGridField term="Enrollments">{campaign.rows.length}</ReadoutGridField>
              <ReadoutGridField term="Activation" span={2}>
                <ActivationMark frozen={campaign.frozen} placement="grid" />
              </ReadoutGridField>
            </ReadoutGridRow>
            <ReadoutGridRow label="Campaign configuration">
              <ReadoutGridField term="Harness">{config.harness}</ReadoutGridField>
              <ReadoutGridField term="Agent identity">{config.agent}</ReadoutGridField>
              <ReadoutGridField term="Session limit">{config.sessionLimit}</ReadoutGridField>
              <ReadoutGridField term="Time warning">{config.timeWarning}</ReadoutGridField>
              <ReadoutGridField term="Max attempts">{config.maxAttempts}</ReadoutGridField>
              <ReadoutGridField term="Cooldown">{config.cooldown}</ReadoutGridField>
            </ReadoutGridRow>
          </ReadoutGrid>
          {campaign.frozen ? (
            <PlateFoot arrangement="start">
              <FrozenLine>Configuration frozen at activation</FrozenLine>
            </PlateFoot>
          ) : (
            <PlateFoot>
              <Key onClick={() => setDialogOpen(true)}>Configure campaign</Key>
            </PlateFoot>
          )}
          </InPlateHost>
      </CampaignsOperateArea>
      <CampaignConfigDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        campaign={campaign}
        onSaveDraft={(values) => {
          setCampaigns((list) =>
            list.map((c) => (c.id === campaign.id ? { ...c, config: values } : c)),
          );
        }}
        onActivate={activate}
      />
    </>
  );
}
