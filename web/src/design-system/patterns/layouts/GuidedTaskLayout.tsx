import type { ComponentProps, ReactNode } from "react";
import type { To } from "react-router-dom";
import { RailBrand } from "../../components/chrome/Brand";
import { PlateFoot } from "../../components/plates/EtchedFrame";
import { cx } from "../../../lib/cx";
import { useAssignedLayoutId } from "./LayoutAssignment";
import { LayoutContent } from "./LayoutContent";
import { SkipLink } from "./SkipLink";

export function GuidedTaskFoot({
  className,
  hairline = false,
  ...props
}: ComponentProps<typeof PlateFoot>) {
  return <PlateFoot className={cx("layout-guided__actions", className)} hairline={hairline} {...props} />;
}

export type GuidedTaskLayoutProps = {
  homeTo?: To;
  homeLabel?: string;
  railLabel: string;
  brandSuffix: string;
  brandExtras?: ReactNode;
  instruments: ReactNode;
  heading: ReactNode;
  children: ReactNode;
  actions?: ReactNode;
  overlays?: ReactNode;
  mainLabel?: string;
  contain?: boolean;
  nested?: boolean;
};

export function GuidedTaskLayout({
  homeTo = "/surfaces",
  homeLabel,
  railLabel,
  brandSuffix,
  brandExtras,
  instruments,
  heading,
  children,
  actions,
  overlays,
  mainLabel,
  contain = false,
  nested,
}: GuidedTaskLayoutProps) {
  useAssignedLayoutId("guided-task");
  return (
    <>
      <div className="layout-guided" data-layout="guided-task" data-nested={nested ? "true" : undefined}>
        {nested ? null : <SkipLink />}
        <div className="frame-traces" aria-hidden="true">
          <span className="trace trace-top" />
          <span className="trace trace-rail" />
        </div>
        <aside className="layout-guided__rail phase-rail" aria-label={railLabel}>
          <RailBrand homeTo={homeTo} homeLabel={homeLabel} suffix={brandSuffix}>{brandExtras}</RailBrand>
          <div className="layout-guided__rail-scroll phase-rail-scroll">{instruments}</div>
        </aside>
        <div className="layout-guided__bay">
          {heading}
          <LayoutContent
            nested={nested}
            contain={contain}
            className="layout-guided__main well-frame pane"
            label={mainLabel}
          >
            {children}
          </LayoutContent>
          {actions}
        </div>
      </div>
      {overlays}
    </>
  );
}
