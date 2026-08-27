import { hashNavigationStrategy, type HashNavigationItem } from "./navigationStrategies";
import {
  SectionedNavigation,
  type SectionedNavigationGroup,
} from "./SectionedNavigation";

export type IndexRailGroup = SectionedNavigationGroup<HashNavigationItem>;

export function IndexRail({
  groups,
  activeId,
  ariaLabel = "Component index",
  onNavigate,
}: {
  groups: readonly IndexRailGroup[];
  activeId?: string;
  ariaLabel?: string;
  onNavigate?: () => void;
}) {
  return (
    <nav className="deck-rail" aria-label={ariaLabel}>
      <div className="nav-rail">
        <SectionedNavigation
          groups={groups}
          strategy={hashNavigationStrategy(activeId)}
          variant="index"
          onNavigate={onNavigate}
        />
      </div>
    </nav>
  );
}
