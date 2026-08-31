import type { ReactNode } from "react";
import {
  Alert,
  GuidedTaskLayout,
  ProfileMenu,
  RailHomeLink,
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
      homeTo={identity.home}
      homeLabel="Home"
      railLabel="Assignment instruments"
      brandSuffix="Assignment Station"
      brandExtras={(
        <>
          <RailHomeLink to="/my-work">My work</RailHomeLink>
          <ProfileMenu
            identity={identity}
            actions={operatorAccountActions(theme, toggleTheme, () => { void logout(); })}
            placement="rail"
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
