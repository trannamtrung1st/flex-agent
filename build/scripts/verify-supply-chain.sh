#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"
ARTIFACTS="$ROOT/artifacts/supply-chain"
mkdir -p "$ARTIFACTS"

echo "==> NuGet vulnerability scan"
dotnet list FlexAgent.slnx package --vulnerable --include-transitive > "$ARTIFACTS/nuget-vulnerable.txt"
if grep -q "has the following vulnerable packages" "$ARTIFACTS/nuget-vulnerable.txt"; then
  cat "$ARTIFACTS/nuget-vulnerable.txt"
  exit 1
fi

echo "==> pnpm audit"
pnpm audit --audit-level=high

echo "==> License inventory"
dotnet list FlexAgent.slnx package --include-transitive --format json > "$ARTIFACTS/nuget-packages.json"
pnpm licenses list --json > "$ARTIFACTS/npm-licenses.json" || pnpm licenses ls --json > "$ARTIFACTS/npm-licenses.json"

if command -v syft >/dev/null 2>&1; then
  syft dir:"$ROOT" -o cyclonedx-json > "$ARTIFACTS/sbom-repo.cdx.json"
else
  echo "syft not installed locally; CI will generate SBOM."
fi

if command -v gitleaks >/dev/null 2>&1; then
  gitleaks detect --source "$ROOT" --no-banner --redact > "$ARTIFACTS/gitleaks.txt"
else
  echo "gitleaks not installed locally; CI will run secret scan."
fi

echo "==> supply-chain verification complete"
