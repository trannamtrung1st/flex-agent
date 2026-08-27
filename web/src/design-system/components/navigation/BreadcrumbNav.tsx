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
          <Link to={homeHref}>{homeLabel}</Link>
        </li>
        {items.map((item, index) => (
          <li key={`${item.label}:${index}`}>
            <span className="breadcrumb-separator" aria-hidden="true">/</span>
            {item.current ? (
              <span aria-current="page">{item.label}</span>
            ) : (
              <Link to={item.href ?? homeHref}>{item.label}</Link>
            )}
          </li>
        ))}
      </ol>
    </nav>
  );
}
