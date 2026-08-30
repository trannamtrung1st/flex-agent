import type { ReactNode } from "react";
import { ChevronGlyph } from "../glyphs";
import { routeNavigationStrategy, type RouteNavigationItem } from "./navigationStrategies";
import { SectionedNavigation, type SectionedNavigationGroup } from "./SectionedNavigation";

export type GangwayItem = RouteNavigationItem;
export type GangwayGroup = SectionedNavigationGroup<GangwayItem>;

export function AreaGroupList({
  groups,
  variant,
  onNavigate,
  collapsibleGroups,
  forceExpanded,
}: {
  groups: readonly GangwayGroup[];
  variant: "gangway" | "rail";
  onNavigate?: () => void;
  collapsibleGroups?: boolean;
  forceExpanded?: boolean;
}) {
  return (
    <SectionedNavigation
      groups={groups}
      strategy={routeNavigationStrategy}
      variant={variant}
      onNavigate={onNavigate}
      collapsibleGroups={collapsibleGroups}
      forceExpanded={forceExpanded}
    />
  );
}

export function Gangway({
  title,
  groups,
  collapsed,
  onCollapsedChange,
  footer,
  ariaLabel = "Areas",
  collapsibleGroups,
}: {
  title: string;
  groups: readonly GangwayGroup[];
  collapsed: boolean;
  onCollapsedChange: (collapsed: boolean) => void;
  footer?: ReactNode;
  ariaLabel?: string;
  collapsibleGroups?: boolean;
}) {
  const toggleLabel = collapsed ? "Expand menu" : "Collapse menu";

  return (
    <nav
      className={`gangway${collapsed ? " is-collapsed" : ""}`}
      data-gangway
      aria-label={ariaLabel}
    >
      <header className="gangway-head">
        <span className="gangway-title">{title}</span>
        <button
          type="button"
          className="gangway-toggle"
          aria-expanded={!collapsed}
          aria-label={toggleLabel}
          onClick={() => onCollapsedChange(!collapsed)}
        >
          <ChevronGlyph />
        </button>
      </header>

      <div className="gangway-body">
        <AreaGroupList
          groups={groups}
          variant="gangway"
          collapsibleGroups={collapsibleGroups}
          forceExpanded={collapsed}
        />
      </div>

      {footer ? <footer className="gangway-foot">{footer}</footer> : null}
    </nav>
  );
}
