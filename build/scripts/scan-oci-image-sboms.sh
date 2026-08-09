#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ARTIFACTS="${ARTIFACTS:-$ROOT/artifacts/supply-chain}"
INSTALL_DIR="${INSTALL_DIR:-$ROOT/.tools/supply-chain/bin}"
mkdir -p "$ARTIFACTS"

INSTALL_DIR="$INSTALL_DIR" bash "$ROOT/build/scripts/ensure-supply-chain-tools.sh"

scan_image() {
  local image="$1"
  local output="$2"
  if ! docker image inspect "$image" >/dev/null 2>&1; then
    echo "OCI image not found: $image" >&2
    return 1
  fi
  echo "==> SBOM for ${image}"
  "$INSTALL_DIR/syft" "$image" -o cyclonedx-json >"$output"
  "$INSTALL_DIR/grype" "sbom:${output}" -o table >"${output%.cdx.json}.grype.txt"
  # OCI images include pinned base OS packages; gate critical findings while recording all severities.
  "$INSTALL_DIR/grype" "sbom:${output}" --fail-on critical
}

scan_image flex-agent-oci-api:local "$ARTIFACTS/sbom-oci-api.cdx.json"
scan_image flex-agent-oci-worker:local "$ARTIFACTS/sbom-oci-worker.cdx.json"
scan_image flex-agent-oci-spa:local "$ARTIFACTS/sbom-oci-spa.cdx.json"

echo "==> OCI image SBOM and vulnerability scan complete"
