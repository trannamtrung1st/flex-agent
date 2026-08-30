import { Link } from "react-router-dom";
import { cx } from "../../../lib/cx";

export type BreadcrumbNavItem = {
  label: string;
  href?: string;
  current?: boolean;
};

export function BreadcrumbNav({
  items,
  homeHref = "/",
  homeLabel = "Home",
  className,
}: {
  items: readonly BreadcrumbNavItem[];
  homeHref?: string;
  homeLabel?: string;
  className?: string;
}) {
  return (
    <nav className={cx("breadcrumb-nav", className)} aria-label="Breadcrumb">
      <ol className="breadcrumb-list">
        <li>
          <Link className="text-link" to={homeHref}>{homeLabel}</Link>
          {items.length > 0 ? <span className="breadcrumb-separator" aria-hidden="true">/</span> : null}
        </li>
        {items.map((item, index) => {
          const isLast = index === items.length - 1;
          return (
            <li key={`${item.label}:${index}`}>
              {item.current ? (
                <span aria-current="page">{item.label}</span>
              ) : item.href ? (
                <Link className="text-link" to={item.href}>{item.label}</Link>
              ) : (
                <span>{item.label}</span>
              )}
              {!isLast ? <span className="breadcrumb-separator" aria-hidden="true">/</span> : null}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
