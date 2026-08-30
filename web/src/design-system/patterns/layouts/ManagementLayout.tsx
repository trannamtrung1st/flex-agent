import { useState, type ReactNode } from "react";
import { maxWidthQuery } from "../../../lib/breakpoints";
import { useMediaQuery } from "../../../lib/useMediaQuery";
import { CommandStrip, type CommandStripProps } from "../../components/chrome/CommandStrip";
import { ConsoleFoot } from "../../components/chrome/OperateHead";
import { Key } from "../../components/keys/Key";
import { AreaGroupList, Gangway, type GangwayGroup } from "../../components/navigation";
import { Bulkhead } from "../../components/overlays/Bulkhead";
import { useAssignedLayoutId } from "./LayoutAssignment";
import { LayoutContent } from "./LayoutContent";
import { SkipLink } from "./SkipLink";

export type ManagementNavigation = {
  title: string;
  groups: readonly GangwayGroup[];
  currentLabel: string;
  ariaLabel?: string;
  bulkheadId?: string;
  collapsibleGroups?: boolean;
};

export type ManagementLayoutProps = {
  commandStrip: CommandStripProps;
  navigation?: ManagementNavigation;
  breadcrumbs?: ReactNode;
  banner?: ReactNode;
  children: ReactNode;
  footer?: ReactNode;
  footerNote?: string;
  overlays?: ReactNode;
  mainLabel?: string;
  contain?: boolean;
  nested?: boolean;
};

export function ManagementLayout({
  commandStrip,
  navigation,
  breadcrumbs,
  banner,
  children,
  footer,
  footerNote,
  overlays,
  mainLabel,
  contain = true,
  nested,
}: ManagementLayoutProps) {
  useAssignedLayoutId("management");
  const [gangwayCollapsed, setGangwayCollapsed] = useState(false);
  const [navOpen, setNavOpen] = useState(false);
  const isDrawerLayout = useMediaQuery(maxWidthQuery("adminDrawer"));
  const bulkheadId = navigation?.bulkheadId ?? "layoutNavBulkhead";
  const navLabel = navigation?.ariaLabel ?? "Primary navigation";

  return (
    <div className="layout-management" data-layout="management" data-nested={nested ? "true" : undefined}>
      {nested ? null : <SkipLink />}
      <CommandStrip {...commandStrip} />
      {banner}
      {navigation ? (
        <div className="layout-management__shell">
          {!isDrawerLayout ? (
            <Gangway
              title={navigation.title}
              groups={navigation.groups}
              collapsed={gangwayCollapsed}
              onCollapsedChange={setGangwayCollapsed}
              ariaLabel={navLabel}
              collapsibleGroups={navigation.collapsibleGroups}
            />
          ) : null}
          <div className="layout-management__content">
            {isDrawerLayout ? (
              <div className="layout-management__drawer-bar">
                <span className="layout-management__drawer-label">{navigation.currentLabel}</span>
                <Key
                  size="compact"
                  ariaExpanded={navOpen}
                  ariaControls={bulkheadId}
                  onClick={() => setNavOpen(true)}
                >
                  Menu
                </Key>
              </div>
            ) : null}
            <LayoutContent nested={nested} contain={contain} className="layout-management__main" label={mainLabel}>
              {breadcrumbs}
              {children}
            </LayoutContent>
          </div>
        </div>
      ) : (
        <LayoutContent nested={nested} contain={contain} className="layout-management__main" label={mainLabel}>
          {breadcrumbs}
          {children}
        </LayoutContent>
      )}
      {footerNote || footer ? <ConsoleFoot note={footerNote ?? ""}>{footer}</ConsoleFoot> : null}
      {navigation ? (
        <Bulkhead
          id={bulkheadId}
          open={isDrawerLayout && navOpen}
          onClose={() => setNavOpen(false)}
          title={navigation.title}
          titleId={`${bulkheadId}Title`}
        >
          <nav className="nav-rail" aria-label={navLabel}>
            <AreaGroupList
              groups={navigation.groups}
              variant="rail"
              collapsibleGroups={navigation.collapsibleGroups}
              onNavigate={() => setNavOpen(false)}
            />
          </nav>
        </Bulkhead>
      ) : null}
      {overlays}
    </div>
  );
}
