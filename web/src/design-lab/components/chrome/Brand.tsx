import type { To } from "react-router-dom";
import type { ReactNode } from "react";
import {
  BrandHomeLink as SharedBrandHomeLink,
  BrandMark,
  PRODUCT_NAME,
  RailBrand as SharedRailBrand,
  StripBrand as SharedStripBrand,
} from "../../../design-system/components/chrome/Brand";
import { CATALOG_ROUTE } from "./operator";

export { BrandMark, PRODUCT_NAME };

export function BrandHomeLink({ className }: { className?: string }) {
  return <SharedBrandHomeLink to={CATALOG_ROUTE} className={className} label="Channel index" />;
}

export function StripBrand({
  suffix,
  origin,
  homeTo = CATALOG_ROUTE,
  homeLabel = "Channel index",
}: {
  suffix?: string;
  origin?: boolean;
  homeTo?: To;
  homeLabel?: string;
}) {
  return <SharedStripBrand homeTo={homeTo} homeLabel={homeLabel} suffix={suffix} origin={origin} />;
}

export function RailBrand({
  suffix,
  children,
  homeTo = CATALOG_ROUTE,
  homeLabel = "Channel index",
}: {
  suffix: string;
  children?: ReactNode;
  homeTo?: To;
  homeLabel?: string;
}) {
  return (
    <SharedRailBrand homeTo={homeTo} homeLabel={homeLabel} suffix={suffix}>
      {children}
    </SharedRailBrand>
  );
}
