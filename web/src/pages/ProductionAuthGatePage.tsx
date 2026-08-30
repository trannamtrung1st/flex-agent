import { useSearchParams } from "react-router-dom";
import { CeremonyUnavailable } from "../design-system";
import { isSignInDeniedSearch, SignInDeniedCopy } from "../api/signin-completion";
import { useProductionApi } from "../api/production-api";
import { UnauthenticatedChrome } from "../components/shell/SessionChrome";

export function ProductionAuthGatePage() {
  const { login } = useProductionApi();
  const [searchParams] = useSearchParams();
  const denied = isSignInDeniedSearch(searchParams.toString());
  const title = denied ? "Sign-in could not be completed" : "Sign in required";

  return (
    <UnauthenticatedChrome>
      <CeremonyUnavailable
        label={title}
        title={title}
        danger={denied}
        alert={denied}
        note={denied
          ? SignInDeniedCopy
          : "Sign in through the organization identity provider. Assessment Campaign work uses this production session. Browser-visible MFA flags are not authorization."}
        recovery={{
          label: "Continue to sign in",
          variant: "transmit",
          onClick: () => { login(); },
        }}
      />
    </UnauthenticatedChrome>
  );
}
