import { useEffect, useRef, type CSSProperties, type Key, type ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { WaitPanel } from "../feedback/WaitPanel";
import { Key as ActionKey } from "../keys";

export type ItemListLoadMoreTrigger = "button" | "end";

export type ItemListLoadMore = {
  /** Canonical next-page request. Used by both `button` and `end` triggers. */
  onLoadMore?: () => void;
  /** Alias for `onLoadMore`. */
  onClick?: () => void;
  waiting?: boolean;
  disabled?: boolean;
  children?: ReactNode;
  /** `button` (default) renders a trailing Load more key. `end` requests when the scrollport reaches its end. */
  trigger?: ItemListLoadMoreTrigger;
};

export type ItemListScroll =
  | boolean
  | {
      maxBlockSize?: string;
      label?: string;
    };

export type ItemListProps<T> = {
  items: readonly T[];
  itemKey: (item: T, index: number) => Key;
  renderItem: (item: T, index: number) => ReactNode;
  label: string;
  loadMore?: ItemListLoadMore | null;
  scroll?: ItemListScroll;
  className?: string;
};

function scrollEnabled(scroll?: ItemListScroll): scroll is Exclude<ItemListScroll, false | undefined> {
  return Boolean(scroll);
}

function requestMore(loadMore: ItemListLoadMore) {
  return loadMore.onLoadMore ?? loadMore.onClick;
}

export function ItemList<T>({
  items,
  itemKey,
  renderItem,
  label,
  loadMore,
  scroll,
  className,
}: ItemListProps<T>) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const sentinelRef = useRef<HTMLLIElement>(null);
  const trigger = loadMore?.trigger ?? "button";
  const load = loadMore ? requestMore(loadMore) : undefined;
  const waiting = Boolean(loadMore?.waiting);
  const disabled = Boolean(loadMore?.disabled);
  const nestedScroll = scrollEnabled(scroll);

  useEffect(() => {
    if (!loadMore || trigger !== "end" || !load || waiting || disabled) return;
    if (typeof IntersectionObserver === "undefined") return;
    const sentinel = sentinelRef.current;
    const root = nestedScroll ? scrollRef.current : null;
    if (!sentinel || (nestedScroll && !root)) return;

    let locked = false;
    const observer = new IntersectionObserver(
      (entries) => {
        if (locked || !entries.some((entry) => entry.isIntersecting)) return;
        locked = true;
        load();
      },
      { root, threshold: 0 },
    );
    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [disabled, items.length, load, loadMore, nestedScroll, trigger, waiting]);

  const list = (
    <ul className={cx("item-list", !nestedScroll && className)} aria-label={label}>
      {items.map((item, index) => (
        <li className="item-list__item" key={itemKey(item, index)}>
          {renderItem(item, index)}
        </li>
      ))}
      {loadMore && trigger === "button" ? (
        <li className="item-list__more">
          <ActionKey
            className="item-list__more-key"
            variant="quiet"
            size="compact"
            waiting={waiting}
            disabled={disabled || waiting}
            onClick={load}
          >
            {loadMore.children ?? "Load more"}
          </ActionKey>
        </li>
      ) : null}
      {loadMore && trigger === "end" ? (
        <li
          className={cx("item-list__end", waiting && "is-waiting")}
          ref={sentinelRef}
          aria-hidden={waiting ? undefined : true}
        >
          {waiting ? <WaitPanel label="Loading more" /> : null}
        </li>
      ) : null}
    </ul>
  );

  if (!nestedScroll) return list;

  const scrollConfig = scroll === true ? {} : scroll;
  const style = scrollConfig.maxBlockSize
    ? ({ "--item-list-scroll-max": scrollConfig.maxBlockSize } as CSSProperties)
    : undefined;

  return (
    <div
      className={cx("item-list-scroll", className)}
      role="region"
      tabIndex={0}
      aria-label={scrollConfig.label ?? `${label}, scrollable`}
      style={style}
      ref={scrollRef}
    >
      {list}
    </div>
  );
}
