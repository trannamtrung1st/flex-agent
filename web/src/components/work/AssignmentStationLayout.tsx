import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import {
  Alert,
  GuidedTaskLayout,
  ProfileMenu,
  operatorAccountActions,
} from "../../design-system";
import { useProductionApi } from "../../api/production-api";
import { useTheme } from "../../lib/useTheme";
import { availableProductionDestinations } from "../../router/production-navigation";
import { productionOperatorIdentity } from "../shell/production-operator";

export function AssignmentStationLayout({
  instruments,
  heading,
  children,
  actions,
  overlays,
  mainLabel = "Assignment",
}: {
  instruments: ReactNode;
  heading: ReactNode;
  children: ReactNode;
  actions?: ReactNode;
  overlays?: ReactNode;
  mainLabel?: string;
}) {
  const { logout, shell, errorMessage } = useProductionApi();
  const { theme, toggleTheme } = useTheme();
  const destinations = availableProductionDestinations(shell?.navigation);
  const identity = productionOperatorIdentity(
    shell?.relationship,
    destinations.map((item) => item.id),
    shell?.display_name,
  );

  return (
    <GuidedTaskLayout
      homeTo="/"
      homeLabel="Home"
      railLabel="Assignment instruments"
      brandSuffix="Assignment Station"
      brandExtras={(
        <>
          <Link className="rail-home-link" to="/my-work">
            <svg viewBox="0 0 10 10" aria-hidden="true" focusable="false">
              <path d="M6.5 1.5 L3 5 L6.5 8.5" fill="none" stroke="currentColor" strokeWidth="1.1" strokeLinecap="square" />
            </svg>
            My work
          </Link>
          <ProfileMenu
            identity={identity}
            actions={operatorAccountActions(theme, toggleTheme, () => { void logout(); })}
            className="strip-profile--rail"
          />
        </>
      )}
      instruments={instruments}
      heading={heading}
      actions={actions}
      overlays={overlays}
      mainLabel={mainLabel}
    >
      {errorMessage ? <Alert variant="danger" title="Request could not be completed">{errorMessage}</Alert> : null}
      {children}
    </GuidedTaskLayout>
  );
}
