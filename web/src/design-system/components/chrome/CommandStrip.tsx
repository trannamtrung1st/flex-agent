import { NavLink, useLocation, type Location, type To } from "react-router-dom";
import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";
import type { OperatorAction, OperatorIdentity } from "./operator";
import { StripBrand } from "./Brand";
import { ProfileMenu } from "./ProfileMenu";

export type CommandStripNavItem = {
  to: To;
  label: string;
  current?: boolean;
  inactive?: boolean;
};

function stripNavTarget(to: To) {
  if (typeof to === "string") {
    const hashIndex = to.indexOf("#");
    if (hashIndex === -1) return { pathname: to, hash: null as string | null };
    return {
      pathname: to.slice(0, hashIndex),
      hash: to.slice(hashIndex),
    };
  }

  const hash = to.hash ?? null;
  return {
    pathname: to.pathname ?? "",
    hash: hash ? (hash.startsWith("#") ? hash : `#${hash}`) : null,
  };
}

function resolveStripNavCurrent(
  item: CommandStripNavItem,
  location: Location,
  routeActive: boolean,
) {
  if (item.current !== undefined) return item.current;
  const { pathname, hash } = stripNavTarget(item.to);
  if (hash) {
    const pathMatch = pathname ? location.pathname === pathname : true;
    return pathMatch && location.hash === hash;
  }
  return routeActive;
}

function StripNavToken({ item }: { item: CommandStripNavItem }) {
  const location = useLocation();

  if (item.inactive) {
    return <span className="strip-token">{item.label}</span>;
  }

  return (
    <NavLink
      className={({ isActive }) => {
        const current = resolveStripNavCurrent(item, location, isActive);
        return cx("strip-token", current && "is-current");
      }}
      to={item.to}
    >
      {item.label}
    </NavLink>
  );
}

export type CommandStripProps = {
  homeTo: To;
  homeLabel?: string;
  brandSuffix?: string;
  nav?: CommandStripNavItem[];
  tabs?: ReactNode;
  mode?: string;
  readout?: string;
  profile?: OperatorIdentity;
  actions?: OperatorAction[];
  identLeading?: ReactNode;
  className?: string;
};

export function CommandStrip({
  homeTo,
  homeLabel,
  brandSuffix,
  nav,
  tabs,
  mode,
  readout,
  profile,
  actions,
  identLeading,
  className,
}: CommandStripProps) {
  const hasCenter = Boolean(nav?.length || tabs);
  return (
    <header className={cx("command-strip", className)}>
      <StripBrand homeTo={homeTo} homeLabel={homeLabel} suffix={brandSuffix} />
      {hasCenter ? (
        <div className={cx("strip-center", Boolean(tabs) && "strip-center--tabs")}>
          {nav?.length ? (
            <nav className="strip-nav strip-nav--role" aria-label="Primary">
              {nav.map((item) => (
                <StripNavToken item={item} key={item.label} />
              ))}
            </nav>
          ) : null}
          {tabs}
        </div>
      ) : null}
      <div className="strip-ident">
        {identLeading}
        {mode ? (
          <span className="strip-mode" aria-hidden="true">
            {mode}
          </span>
        ) : null}
        {readout ? <span className="strip-readout">{readout}</span> : null}
        {profile ? <ProfileMenu identity={profile} actions={actions ?? []} /> : null}
      </div>
    </header>
  );
}
