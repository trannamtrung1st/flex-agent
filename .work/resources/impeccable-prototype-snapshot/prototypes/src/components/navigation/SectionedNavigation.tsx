import { useState, type ReactNode } from "react";

export type SectionedNavigationGroup<TItem> = {
  id?: string;
  label: string;
  items: readonly TItem[];
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
};

function SectionGroupLabel({
  as: Tag = "span",
  label,
}: {
  as?: "span" | "summary";
  label: string;
}) {
  return (
    <Tag className="gangway-section-label">
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
}: SectionedNavigationProps<TItem>) {
  const compactIndex = variant === "index" &&
    typeof window !== "undefined" &&
    window.matchMedia?.("(max-width: 900px)").matches;
  const [openIndex, setOpenIndex] = useState<number | null>(compactIndex ? 0 : null);
  const listClass = variant === "gangway" ? "gangway-list" : variant === "index" ? "nav-list deck-index" : "nav-list";
  const sectionClass = variant === "gangway" ? "gangway-section" : "nav-rail-section";

  return (
    <>
      {groups.map((group, groupIndex) => {
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

        if (variant === "index") {
          return (
            <details
              className={sectionClass}
              key={group.id ?? group.label}
              open={!compactIndex || openIndex === groupIndex}
              onToggle={(event) => {
                if (!compactIndex) return;
                if (event.currentTarget.open) setOpenIndex(groupIndex);
                else {
                  setOpenIndex((current) => (
                    current === groupIndex ? null : current
                  ));
                }
              }}
            >
              <SectionGroupLabel as="summary" label={group.label} />
              {items}
            </details>
          );
        }

        return (
          <section className={sectionClass} key={group.id ?? group.label}>
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
