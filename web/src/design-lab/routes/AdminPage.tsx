import { useCallback, useEffect, useMemo, useState } from "react";
import { Outlet, useLocation, useSearchParams } from "react-router-dom";
import {
  ADMINISTRATOR_IDENTITY,
  Announcer,
  CATALOG_ROUTE,
  SignOutCeremony,
  ToastDock,
  usePrototypeSignOut,
  useToasts,
} from "../components";
import { ManagementLayout } from "../../design-system";
import { createCampaigns } from "../data/fixtures/campaigns";
import type { AdminOutletContext } from "../features/admin/adminContext";
import {
  adminAreaLabel,
  adminNavGroups,
  assessmentCampaignQuery,
  defaultsCampaignSelection,
  operationalCampaignId,
} from "../features/admin/adminNav";
import { useAnnouncer } from "../../lib/useAnnouncer";
import { useSurface } from "../lib/useSurface";

export function AdminPage() {
  useSurface("admin-console");
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();
  const [campaigns, setCampaigns] = useState(() => createCampaigns());
  const [sealing, setSealing] = useState(false);
  const { message, announce } = useAnnouncer();
  const { toasts, pushToast } = useToasts();
  const { actions, signOutOpen, setSignOutOpen } = usePrototypeSignOut();

  const campaignId = searchParams.get("campaign");
  const knownIds = useMemo(() => campaigns.map((item) => item.id), [campaigns]);
  const [workingCampaignId, setWorkingCampaignId] = useState<string | null>(() =>
    campaignId && campaigns.some((item) => item.id === campaignId) ? campaignId : null,
  );
  const matched = useMemo(
    () => (campaignId ? campaigns.find((item) => item.id === campaignId) : undefined),
    [campaignId, campaigns],
  );
  const rememberedCampaignId =
    matched?.id ?? (workingCampaignId && knownIds.includes(workingCampaignId) ? workingCampaignId : null);
  const selectedId = operationalCampaignId(location.pathname, campaignId, knownIds, rememberedCampaignId);
  const campaign = useMemo(
    () => (selectedId ? campaigns.find((item) => item.id === selectedId) : undefined),
    [campaigns, selectedId],
  );

  const writeCampaignParam = useCallback((id: string | null) => {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        if (id) next.set("campaign", id);
        else next.delete("campaign");
        return next;
      },
      { replace: true },
    );
  }, [setSearchParams]);
  const setCampaignId = useCallback((id: string | null) => {
    if (id) setWorkingCampaignId(id);
    writeCampaignParam(id);
  }, [writeCampaignParam]);

  useEffect(() => {
    if (!defaultsCampaignSelection(location.pathname) || !campaign) return;
    if (campaignId === campaign.id) return;
    writeCampaignParam(campaign.id);
  }, [campaign, campaignId, location.pathname, writeCampaignParam]);

  const campaignQuery = assessmentCampaignQuery(
    knownIds,
    matched?.id,
    rememberedCampaignId,
    location.pathname,
    campaignId,
  );
  const navGroups = adminNavGroups({
    pathname: location.pathname,
    campaignQuery,
  });
  const areaLabel = adminAreaLabel(location.pathname);
  const outletContext: AdminOutletContext = {
    campaigns,
    campaign,
    campaignId,
    setCampaigns,
    setCampaignId,
    announce,
    pushToast,
    sealing,
    setSealing,
  };

  return (
    <ManagementLayout
      commandStrip={{
        homeTo: CATALOG_ROUTE,
        homeLabel: "Channel index",
        brandSuffix: "Admin",
        profile: ADMINISTRATOR_IDENTITY,
        actions,
      }}
      navigation={{
        title: "Administrator",
        groups: navGroups,
        currentLabel: areaLabel,
        ariaLabel: "Administrator areas",
        bulkheadId: "adminNavBulkhead",
      }}
      footerNote="Synthetic demonstration content — no real participant data."
      overlays={
        <>
          <Announcer message={message} />
          <ToastDock toasts={toasts} />
          <SignOutCeremony open={signOutOpen} onClose={() => setSignOutOpen(false)} />
        </>
      }
    >
      <Outlet context={outletContext} />
    </ManagementLayout>
  );
}
