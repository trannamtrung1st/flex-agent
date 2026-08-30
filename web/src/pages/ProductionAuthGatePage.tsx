import { useSearchParams } from "react-router-dom";
import { CeremonyEmpty, Key } from "../design-system";
import { isSignInDeniedSearch, SignInDeniedCopy } from "../api/signin-completion";
import { useProductionApi } from "../api/production-api";
import { CeremonyArea, UnauthenticatedChrome } from "../components/shell/SessionChrome";

export function ProductionAuthGatePage() {
  const { login } = useProductionApi();
  const [searchParams] = useSearchParams();
  const denied = isSignInDeniedSearch(searchParams.toString());

  return (
    <UnauthenticatedChrome>
      <CeremonyArea
        label={denied ? "Sign-in could not be completed" : "Sign in required"}
        title={denied ? "Sign-in could not be completed" : "Sign in required"}
        danger={denied}
      >
        <CeremonyEmpty
          alert={denied}
          note={denied
            ? SignInDeniedCopy
            : "Sign in through the organization identity provider. Assessment Campaign work uses this production session. Browser-visible MFA flags are not authorization."}
        >
          <Key variant="transmit" onClick={() => { login(); }}>
            Continue to sign in
          </Key>
        </CeremonyEmpty>
      </CeremonyArea>
    </UnauthenticatedChrome>
  );
}
