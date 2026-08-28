import { Key } from "../design-system";
import { useProductionApi } from "../api/production-api";
import { CeremonyArea, UnauthenticatedChrome } from "../components/shell/SessionChrome";

export function ProductionAuthGatePage() {
  const { login } = useProductionApi();

  return (
    <UnauthenticatedChrome>
      <CeremonyArea
        label="Sign in required"
        title="Sign in required"
        description="Sign in through the organization identity provider. Assessment Campaign work uses this production session. Browser-visible MFA flags are not authorization."
      >
        <Key variant="transmit" onClick={() => { login(); }}>
          Continue to sign in
        </Key>
      </CeremonyArea>
    </UnauthenticatedChrome>
  );
}
