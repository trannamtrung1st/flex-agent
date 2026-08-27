import { useEffect, useRef } from "react";
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
  const railRef = useRef<HTMLElement>(null);

  useEffect(() => {
    if (!activeId) return;
    const rail = railRef.current;
    const scrollport = rail?.querySelector<HTMLElement>(".nav-rail");
    if (!scrollport || scrollport.scrollHeight <= scrollport.clientHeight + 1) return;

    const current = scrollport.querySelector<HTMLElement>(`a[href="#${CSS.escape(activeId)}"]`);
    current?.scrollIntoView({ block: "nearest" });
  }, [activeId]);

  return (
    <nav ref={railRef} className="deck-rail" aria-label={ariaLabel}>
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
