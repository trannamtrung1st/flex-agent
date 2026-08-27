import { useCallback, useEffect, useMemo, useState } from "react";
import { Outlet, useLocation, useSearchParams } from "react-router";
import {
  ADMINISTRATOR_IDENTITY,
  Announcer,
  AreaGroupList,
  Bulkhead,
  CommandStrip,
  ConsoleFoot,
  Gangway,
  Key,
  SignOutCeremony,
  ToastDock,
  administratorHome,
  usePrototypeSignOut,
  useToasts,
} from "../components";
import { createCampaigns } from "../data/fixtures/campaigns";
import type { AdminOutletContext } from "../features/admin/adminContext";
import {
  adminAreaLabel,
  adminNavGroups,
  assessmentCampaignQuery,
  defaultsCampaignSelection,
  operationalCampaignId,
} from "../features/admin/adminNav";
import { useAnnouncer } from "../lib/useAnnouncer";
import { maxWidthQuery } from "../lib/breakpoints";
import { useMediaQuery } from "../lib/useMediaQuery";
import { useSurface } from "../lib/useSurface";

export function AdminPage() {
  useSurface("admin-console");
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();
  const [campaigns, setCampaigns] = useState(() => createCampaigns());
  const [gangwayCollapsed, setGangwayCollapsed] = useState(false);
  const [navOpen, setNavOpen] = useState(false);
  const [sealing, setSealing] = useState(false);
  const isDrawerLayout = useMediaQuery(maxWidthQuery("adminDrawer"));
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
    <>
      <CommandStrip
        brandSuffix="Admin"
        nav={[{ to: administratorHome(matched?.id ?? workingCampaignId ?? campaign?.id ?? undefined), label: "Home" }]}
        profile={ADMINISTRATOR_IDENTITY}
        actions={actions}
      />
      <div className="admin-shell">
        {!isDrawerLayout ? (
          <Gangway
            title="Administrator"
            groups={navGroups}
            collapsed={gangwayCollapsed}
            onCollapsedChange={setGangwayCollapsed}
            ariaLabel="Administrator areas"
          />
        ) : null}
        <div className="admin-content">
          {isDrawerLayout ? (
            <div className="admin-drawer-bar" aria-label="Administrator areas">
              <span className="admin-drawer-label">{areaLabel}</span>
              <Key
                size="compact"
                ariaExpanded={navOpen}
                ariaControls="adminNavBulkhead"
                onClick={() => setNavOpen(true)}
              >
                Menu
              </Key>
            </div>
          ) : null}
          <Outlet context={outletContext} />
        </div>
      </div>
      <ConsoleFoot note="Synthetic demonstration content — no real participant data." />
      <Bulkhead
        id="adminNavBulkhead"
        open={isDrawerLayout && navOpen}
        onClose={() => setNavOpen(false)}
        title="Administrator"
        titleId="adminNavBulkheadTitle"
      >
        <nav className="nav-rail" aria-label="Administrator areas">
          <AreaGroupList groups={navGroups} variant="rail" onNavigate={() => setNavOpen(false)} />
        </nav>
      </Bulkhead>
      <Announcer message={message} />
      <ToastDock toasts={toasts} />
      <SignOutCeremony open={signOutOpen} onClose={() => setSignOutOpen(false)} />
    </>
  );
}
