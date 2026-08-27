import type { ReactNode } from "react";
import { ChevronGlyph } from "../glyphs";
import { routeNavigationStrategy, type RouteNavigationItem } from "./navigationStrategies";
import { SectionedNavigation } from "./SectionedNavigation";

export type GangwayItem = RouteNavigationItem;
export type GangwayGroup = {
  id?: string;
  label: string;
  items: GangwayItem[];
};

export function AreaGroupList({
  groups,
  variant,
  onNavigate,
}: {
  groups: readonly GangwayGroup[];
  variant: "gangway" | "rail";
  onNavigate?: () => void;
}) {
  return (
    <SectionedNavigation
      groups={groups}
      strategy={routeNavigationStrategy}
      variant={variant}
      onNavigate={onNavigate}
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
}: {
  title: string;
  groups: readonly GangwayGroup[];
  collapsed: boolean;
  onCollapsedChange: (collapsed: boolean) => void;
  footer?: ReactNode;
  ariaLabel?: string;
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
        <AreaGroupList groups={groups} variant="gangway" />
      </div>

      {footer ? <footer className="gangway-foot">{footer}</footer> : null}
    </nav>
  );
}
