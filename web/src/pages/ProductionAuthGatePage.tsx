import { Key, OperateArea } from "../design-system";
import { useProductionApi } from "../api/production-api";
import { UnauthenticatedChrome } from "../components/shell/SessionChrome";

export function ProductionAuthGatePage() {
  const { login } = useProductionApi();

  return (
    <UnauthenticatedChrome>
      <OperateArea
        className="workspace-area"
        label="Sign in required"
        title="Sign in required"
        description="Assessment Campaign setup uses the production application session. Sign in through the organization identity provider. Browser-visible MFA flags are not authorization."
      >
        <Key variant="transmit" onClick={() => { login(); }}>
          Continue to sign in
        </Key>
      </OperateArea>
    </UnauthenticatedChrome>
  );
}
