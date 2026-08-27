import { Link } from "react-router";
import type { ReactNode } from "react";
import { cx } from "../../lib/cx";
import { CATALOG_ROUTE } from "./operator";

export const PRODUCT_NAME = "Flex Agent";

export function BrandMark({ className }: { className?: string }) {
  return <span className={cx("brand-mark", className)}>{PRODUCT_NAME}</span>;
}

export function BrandHomeLink({ className }: { className?: string }) {
  return (
    <Link to={CATALOG_ROUTE} className={cx("brand-home-link", className)} aria-label="Channel index">
      <BrandMark />
    </Link>
  );
}

export function StripBrand({
  suffix,
  origin,
}: {
  suffix?: string;
  origin?: boolean;
}) {
  const isOrigin = origin ?? Boolean(suffix);
  return (
    <span className={cx("strip-brand", isOrigin && "strip-brand--origin")}>
      <BrandHomeLink />
      {suffix ? <span className="strip-mode">{suffix}</span> : null}
    </span>
  );
}

export function RailBrand({
  suffix,
  children,
}: {
  suffix: string;
  children?: ReactNode;
}) {
  return (
    <header className="rail-brand">
      <span className="rail-brand-name">
        <BrandHomeLink />
        <span className="rail-brand-suffix">{suffix}</span>
      </span>
      {children}
    </header>
  );
}
