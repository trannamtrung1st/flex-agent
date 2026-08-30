import { Link, type To } from "react-router-dom";
import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";

export const PRODUCT_NAME = "Flex Agent";

export function BrandMark({ className }: { className?: string }) {
  return <span className={cx("brand-mark", className)}>{PRODUCT_NAME}</span>;
}

export function BrandHomeLink({
  to,
  className,
  label = "Home",
}: {
  to: To;
  className?: string;
  label?: string;
}) {
  return (
    <Link to={to} className={cx("brand-home-link", className)} aria-label={label}>
      <BrandMark />
    </Link>
  );
}

export function StripBrand({
  homeTo,
  homeLabel,
  suffix,
}: {
  homeTo: To;
  homeLabel?: string;
  suffix?: string;
}) {
  const isOrigin = Boolean(suffix);
  return (
    <span className={cx("strip-brand", isOrigin && "strip-brand--origin")}>
      <BrandHomeLink to={homeTo} label={homeLabel} />
      {suffix ? <span className="strip-mode">{suffix}</span> : null}
    </span>
  );
}

export function RailBrand({
  homeTo,
  homeLabel,
  suffix,
  children,
}: {
  homeTo: To;
  homeLabel?: string;
  suffix: string;
  children?: ReactNode;
}) {
  return (
    <header className="rail-brand">
      <span className="rail-brand-name">
        <BrandHomeLink to={homeTo} label={homeLabel} />
        <span className="rail-brand-suffix">{suffix}</span>
      </span>
      {children}
    </header>
  );
}
