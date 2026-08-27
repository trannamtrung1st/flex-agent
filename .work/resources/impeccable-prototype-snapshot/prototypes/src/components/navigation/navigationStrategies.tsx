import { Link, type To } from "react-router";
import type { NavigationRenderStrategy } from "./SectionedNavigation";

export type RouteNavigationItem = {
  to: To;
  label: string;
  abbr: string;
  current?: boolean;
};

export type HashNavigationItem = {
  id: string;
  label: string;
};

export const routeNavigationStrategy: NavigationRenderStrategy<RouteNavigationItem> = {
  getKey: (item) => `${item.abbr}:${item.label}`,
  getLabel: (item) => item.label,
  getAbbreviation: (item) => item.abbr,
  isCurrent: (item) => Boolean(item.current),
  ariaCurrent: "page",
  renderLink: (item, state) => (
    <Link
      className={state.className}
      to={item.to}
      data-tip={state.className.includes("gangway-link") ? item.label : undefined}
      aria-current={state.current ? state.ariaCurrent : undefined}
      aria-label={item.label}
      onClick={state.onNavigate}
    >
      {state.children}
    </Link>
  ),
};

export function hashNavigationStrategy(
  activeId?: string,
): NavigationRenderStrategy<HashNavigationItem> {
  return {
    getKey: (item) => item.id,
    getLabel: (item) => item.label,
    isCurrent: (item) => item.id === activeId,
    ariaCurrent: "location",
    renderLink: (item, state) => (
      <a
        className={state.className}
        href={`#${item.id}`}
        aria-current={state.current ? state.ariaCurrent : undefined}
        onClick={state.onNavigate}
      >
        {state.children}
      </a>
    ),
  };
}
