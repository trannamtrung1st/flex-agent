import { useEffect, useState, type MouseEvent, type ReactNode } from "react";
import { maxWidthQuery } from "../../../lib/breakpoints";
import { useMediaQuery } from "../../../lib/useMediaQuery";

export type SectionedNavigationGroup<TItem> = {
  id?: string;
  label: string;
  items: readonly TItem[];
  collapsible?: boolean;
};

export type NavigationRenderState = {
  className: string;
  current: boolean;
  ariaCurrent: "page" | "location";
  children: ReactNode;
  onNavigate?: () => void;
};

export type NavigationRenderStrategy<TItem> = {
  getKey: (item: TItem) => string;
  getLabel: (item: TItem) => string;
  getAbbreviation?: (item: TItem) => string | undefined;
  isCurrent: (item: TItem) => boolean;
  ariaCurrent: "page" | "location";
  renderLink: (item: TItem, state: NavigationRenderState) => ReactNode;
};

export type SectionedNavigationProps<TItem> = {
  groups: readonly SectionedNavigationGroup<TItem>[];
  strategy: NavigationRenderStrategy<TItem>;
  variant: "gangway" | "rail" | "index";
  onNavigate?: () => void;
  collapsibleGroups?: boolean;
  forceExpanded?: boolean;
};

function groupKey(group: { id?: string; label: string }) {
  return group.id ?? group.label;
}

function groupIsCollapsible<TItem>(
  group: SectionedNavigationGroup<TItem>,
  collapsibleGroups: boolean | undefined,
  variant: SectionedNavigationProps<TItem>["variant"],
) {
  if (group.collapsible != null) return group.collapsible;
  if (collapsibleGroups != null) return collapsibleGroups;
  return variant === "index";
}

function SectionGroupLabel({
  as: Tag = "span",
  label,
  onClick,
}: {
  as?: "span" | "summary";
  label: string;
  onClick?: (event: MouseEvent<HTMLElement>) => void;
}) {
  return (
    <Tag className="gangway-section-label" onClick={onClick}>
      <span className="gangway-section-node" aria-hidden="true" />
      <span className="gangway-section-label-text">{label}</span>
    </Tag>
  );
}

function itemContent<TItem>({
  item,
  label,
  strategy,
  variant,
}: {
  item: TItem;
  label: string;
  strategy: NavigationRenderStrategy<TItem>;
  variant: SectionedNavigationProps<TItem>["variant"];
}) {
  if (variant === "index") {
    return (
      <>
        <span className="gangway-tick" aria-hidden="true" />
        <span className="gangway-link-text">{label}</span>
      </>
    );
  }

  if (variant !== "gangway") return label;

  return (
    <>
      <span className="gangway-tick" aria-hidden="true" />
      <span className="gangway-abbr" aria-hidden="true">
        {strategy.getAbbreviation?.(item)}
      </span>
      <span className="gangway-label">{label}</span>
    </>
  );
}

export function SectionedNavigation<TItem>({
  groups,
  strategy,
  variant,
  onNavigate,
  collapsibleGroups,
  forceExpanded = false,
}: SectionedNavigationProps<TItem>) {
  const compactCatalog = useMediaQuery(maxWidthQuery("gallery"));
  const accordion = variant === "index" && compactCatalog;
  const currentGroupIndex = groups.findIndex((group) => group.items.some((item) => strategy.isCurrent(item)));
  const currentGroupKey = currentGroupIndex >= 0 ? groupKey(groups[currentGroupIndex]!) : null;
  const [openIndex, setOpenIndex] = useState(() => Math.max(0, currentGroupIndex));
  const [openKeys, setOpenKeys] = useState<Record<string, boolean>>(() =>
    Object.fromEntries(groups.map((group) => [groupKey(group), true])),
  );

  useEffect(() => {
    if (currentGroupIndex < 0 || currentGroupKey == null) return;
    /* eslint-disable react-hooks/set-state-in-effect -- route changes must reopen the active catalog group */
    setOpenIndex(currentGroupIndex);
    setOpenKeys((current) => (
      current[currentGroupKey] ? current : { ...current, [currentGroupKey]: true }
    ));
    /* eslint-enable react-hooks/set-state-in-effect */
  }, [currentGroupIndex, currentGroupKey]);
  const listClass = variant === "gangway" ? "gangway-list" : variant === "index" ? "nav-list deck-index" : "nav-list";

  return (
    <>
      {groups.map((group, groupIndex) => {
        const key = groupKey(group);
        const collapsible = groupIsCollapsible(group, collapsibleGroups, variant);
        const sectionClass = variant === "index"
          ? "nav-rail-section"
          : variant === "gangway" || collapsible
            ? "gangway-section"
            : "nav-rail-section";
        const items = (
          <ul className={listClass}>
            {group.items.map((item) => {
              const label = strategy.getLabel(item);
              const current = strategy.isCurrent(item);
              const className = `${variant === "gangway" ? "gangway-link tip-trailing" : "nav-link"}${current ? " is-current" : ""}`;

              return (
                <li key={strategy.getKey(item)}>
                  {strategy.renderLink(item, {
                    className,
                    current,
                    ariaCurrent: strategy.ariaCurrent,
                    children: itemContent({ item, label, strategy, variant }),
                    onNavigate,
                  })}
                </li>
              );
            })}
          </ul>
        );

        if (collapsible) {
          const open = forceExpanded || (accordion ? openIndex === groupIndex : openKeys[key]);

          return (
            <details
              className={sectionClass}
              key={key}
              open={open}
            >
              <SectionGroupLabel
                as="summary"
                label={group.label}
                onClick={(event) => {
                  event.preventDefault();
                  if (forceExpanded) return;
                  if (accordion) {
                    setOpenIndex((current) => (current === groupIndex ? -1 : groupIndex));
                    return;
                  }
                  setOpenKeys((current) => ({ ...current, [key]: !current[key] }));
                }}
              />
              {items}
            </details>
          );
        }

        return (
          <section className={sectionClass} key={key}>
            {variant === "gangway" ? (
              <SectionGroupLabel label={group.label} />
            ) : (
              <span className="nav-rail-label">{group.label}</span>
            )}
            {items}
          </section>
        );
      })}
    </>
  );
}
