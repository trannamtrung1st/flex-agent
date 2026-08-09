#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"
ARTIFACTS="$ROOT/artifacts/supply-chain"
PUBLISH="$ROOT/artifacts/publish"
INSTALL_DIR="${INSTALL_DIR:-$ROOT/.tools/supply-chain/bin}"
mkdir -p "$ARTIFACTS" "$PUBLISH/api" "$PUBLISH/worker"

corepack enable

echo "==> Restore locked dependencies"
dotnet restore FlexAgent.slnx --locked-mode
pnpm install --frozen-lockfile

echo "==> NuGet vulnerability scan"
dotnet list FlexAgent.slnx package --vulnerable --include-transitive > "$ARTIFACTS/nuget-vulnerable.txt"
if grep -q "has the following vulnerable packages" "$ARTIFACTS/nuget-vulnerable.txt"; then
  cat "$ARTIFACTS/nuget-vulnerable.txt"
  exit 1
fi

echo "==> pnpm audit"
pnpm audit --audit-level=high

echo "==> License inventory"
pnpm licenses list --json > "$ARTIFACTS/npm-licenses.json" || pnpm licenses ls --json > "$ARTIFACTS/npm-licenses.json"

echo "==> Publish application artifacts for SBOM scan"
dotnet publish src/Hosts/FlexAgent.Api/FlexAgent.Api.csproj \
  -c Release \
  -o "$PUBLISH/api" \
  /p:UseAppHost=false
dotnet publish src/Hosts/FlexAgent.Worker/FlexAgent.Worker.csproj \
  -c Release \
  -o "$PUBLISH/worker" \
  /p:UseAppHost=false
pnpm build

echo "==> Ensure pinned supply-chain tools"
INSTALL_DIR="$INSTALL_DIR" bash "$ROOT/build/scripts/ensure-supply-chain-tools.sh"

echo "==> SBOM generation (shipped artifacts)"
"$INSTALL_DIR/syft" dir:"$PUBLISH" -o cyclonedx-json > "$ARTIFACTS/sbom-publish.cdx.json"
bash "$ROOT/build/scripts/generate-spa-sbom.sh" "$ARTIFACTS/sbom-spa.cdx.json"

echo "==> Vulnerability scan"
"$INSTALL_DIR/grype" sbom:"$ARTIFACTS/sbom-publish.cdx.json" --fail-on high
"$INSTALL_DIR/grype" sbom:"$ARTIFACTS/sbom-spa.cdx.json" --fail-on high

bash "$ROOT/build/scripts/generate-nuget-licenses.sh" "$ARTIFACTS/nuget-licenses.json"

echo "==> Secret scan"
"$INSTALL_DIR/gitleaks" detect --source "$ROOT" --no-banner --redact > "$ARTIFACTS/gitleaks.txt"

echo "==> OCI image SBOM and vulnerability scan"
bash "$ROOT/build/scripts/build-oci-images.sh" >/dev/null
bash "$ROOT/build/scripts/scan-oci-image-sboms.sh"

echo "==> supply-chain verification complete"
