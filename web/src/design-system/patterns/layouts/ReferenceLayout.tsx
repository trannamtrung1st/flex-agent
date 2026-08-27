import type { MouseEvent, ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { CommandStrip, type CommandStripProps } from "../../components/chrome/CommandStrip";
import { ConsoleFoot } from "../../components/chrome/OperateHead";
import { IndexRail, type IndexRailGroup } from "../../components/navigation";
import { LayoutContent } from "./LayoutContent";
import { SkipLink } from "./SkipLink";
import { useAssignedLayoutId } from "./LayoutAssignment";

export type ReferenceIndex = {
  groups: readonly IndexRailGroup[];
  activeId?: string;
  onNavigate?: () => void;
  onDeckClick?: (event: MouseEvent<HTMLDivElement>) => void;
};

export type ReferenceLayoutProps = {
  commandStrip: CommandStripProps;
  index?: ReferenceIndex;
  children: ReactNode;
  footer?: ReactNode;
  footerNote?: string;
  overlays?: ReactNode;
  mainLabel?: string;
  mainClassName?: string;
  contain?: boolean;
  nested?: boolean;
};

export function ReferenceLayout({
  commandStrip,
  index,
  children,
  footer,
  footerNote,
  overlays,
  mainLabel,
  mainClassName,
  contain,
  nested,
}: ReferenceLayoutProps) {
  useAssignedLayoutId("reference");
  const deck = Boolean(index);
  const wrapMain = contain ?? !deck;

  return (
    <div className={cx("layout-reference", deck && "layout-reference--deck")} data-layout="reference">
      {nested ? null : <SkipLink />}
      <CommandStrip {...commandStrip} className={cx(commandStrip.className, deck && "page-strip")} />
      {index ? (
        <div className="layout-reference__deck deck" onClick={index.onDeckClick}>
          <IndexRail groups={index.groups} activeId={index.activeId} onNavigate={index.onNavigate} />
          <LayoutContent
            nested={nested}
            contain={wrapMain}
            className={cx("layout-reference__main deck-main", mainClassName)}
            label={mainLabel}
          >
            {children}
            {footerNote || footer ? <ConsoleFoot note={footerNote ?? ""}>{footer}</ConsoleFoot> : null}
          </LayoutContent>
        </div>
      ) : (
        <>
          <LayoutContent
            nested={nested}
            contain={wrapMain}
            className={cx("layout-reference__main", mainClassName)}
            label={mainLabel}
          >
            {children}
          </LayoutContent>
          {footerNote || footer ? <ConsoleFoot note={footerNote ?? ""}>{footer}</ConsoleFoot> : null}
        </>
      )}
      {overlays}
    </div>
  );
}
