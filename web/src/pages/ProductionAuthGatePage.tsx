import { Button } from "../components/ui/Button";
import { StatusPanel } from "../components/ui/StatusPanel";
import { useProductionApi } from "../api/production-api";

export function ProductionAuthGatePage() {
  const { login } = useProductionApi();

  return (
    <div className="shell-content" style={{ padding: "2rem 1.25rem", maxWidth: "40rem" }}>
      <StatusPanel title="Sign in required">
        <p>
          Assessment Campaign setup uses the production application session. Sign in through the organization
          identity provider. Browser-visible MFA flags are not authorization.
        </p>
      </StatusPanel>
      <Button onClick={() => { login(); }}>Continue to sign in</Button>
    </div>
  );
}
