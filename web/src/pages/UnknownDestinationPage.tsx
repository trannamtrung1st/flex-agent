import { CeremonyUnavailable } from "../design-system";
import { useProductionApi } from "../api/production-api";
import { productionWorkspaceHome } from "../router/production-navigation";

const UNKNOWN_NOTE = "The current authorized relationship cannot use this locator.";

export function UnknownDestinationPage() {
  const { shell } = useProductionApi();
  return (
    <CeremonyUnavailable
      title="This destination is not available"
      note={UNKNOWN_NOTE}
      recovery={{ label: "Return to Home", to: productionWorkspaceHome(shell?.navigation) }}
    />
  );
}
