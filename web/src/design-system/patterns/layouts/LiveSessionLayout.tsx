import type { ReactNode, Ref } from "react";
import type { To } from "react-router-dom";
import { cx } from "../../../lib/cx";
import { RailBrand } from "../../components/chrome/Brand";
import { useAssignedLayoutId } from "./LayoutAssignment";
import { LayoutContent } from "./LayoutContent";
import { SkipLink } from "./SkipLink";

export type LiveSessionLayoutProps = {
  homeTo?: To;
  homeLabel?: string;
  railLabel: string;
  brandSuffix: string;
  brandExtras?: ReactNode;
  instruments: ReactNode;
  children: ReactNode;
  composer?: ReactNode;
  examiner: ReactNode;
  overlays?: ReactNode;
  warned?: boolean;
  complete?: boolean;
  mainLabel?: string;
  examinerLabel?: string;
  mainRef?: Ref<HTMLElement>;
  contain?: boolean;
  nested?: boolean;
};

export function LiveSessionLayout({
  homeTo = "/",
  homeLabel,
  railLabel,
  brandSuffix,
  brandExtras,
  instruments,
  children,
  composer,
  examiner,
  overlays,
  warned,
  complete,
  mainLabel = "Examination transcript",
  examinerLabel = "Examiner station",
  mainRef,
  contain = false,
  nested,
}: LiveSessionLayoutProps) {
  useAssignedLayoutId("live-session");
  return (
    <>
      <div
        className={cx("layout-session", warned && !complete && "is-warned", complete && "is-complete")}
        data-layout="live-session"
      >
        {nested ? null : <SkipLink />}
        <div className="frame-traces" aria-hidden="true">
          <span className="trace trace-top" />
          <span className="trace trace-chrono" />
          <span className="trace trace-foot" />
        </div>
        <aside className="layout-session__rail rail" aria-label={railLabel}>
          <RailBrand homeTo={homeTo} homeLabel={homeLabel} suffix={brandSuffix}>{brandExtras}</RailBrand>
          <div className="layout-session__rail-scroll rail-scroll">{instruments}</div>
        </aside>
        <div className="layout-session__bay">
          <LayoutContent
            nested={nested}
            contain={contain}
            contentRef={mainRef}
            className="layout-session__main ledger-frame pane pane--tl"
            label={mainLabel}
          >
            {children}
          </LayoutContent>
          {composer != null ? (
            <footer className="layout-session__composer composer-row">{composer}</footer>
          ) : null}
        </div>
        <aside className="layout-session__examiner agent-panel pane" aria-label={examinerLabel}>
          {examiner}
        </aside>
      </div>
      {overlays}
    </>
  );
}
