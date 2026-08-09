#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TOOLCHAIN="$ROOT/build/toolchain.json"
OUTPUT="${1:-$ROOT/artifacts/supply-chain/sbom-spa.cdx.json}"

read_toolchain() {
  python3 - "$TOOLCHAIN" "$1" <<'PY'
import json
import sys

toolchain = json.load(open(sys.argv[1]))
path = sys.argv[2].split(".")
value = toolchain
for key in path:
    value = value[key]
print(value)
PY
}

EXPECTED_GENERATOR_VERSION="$(read_toolchain "supplyChain.cyclonedxNpm.version")"
mkdir -p "$(dirname "$OUTPUT")"

echo "==> Generate SPA runtime SBOM from locked dependency graph"
(
  cd "$ROOT"
  pnpm exec cyclonedx-npm \
    --omit dev \
    --output-file "$OUTPUT" \
    web/package.json
)

python3 - "$OUTPUT" "$TOOLCHAIN" "$ROOT/package.json" <<'PY'
import json
import sys

output = sys.argv[1]
toolchain = json.load(open(sys.argv[2]))
package_json = json.load(open(sys.argv[3]))
bom = json.load(open(output))
components = bom.get("components") or []
names = {component.get("name", "").lower() for component in components}

expected_generator = toolchain["supplyChain"]["cyclonedxNpm"]["version"]
installed_generator = (
    package_json.get("devDependencies", {}).get("@cyclonedx/cyclonedx-npm")
)
if installed_generator != expected_generator:
    raise SystemExit(
        f"cyclonedx-npm version mismatch: toolchain {expected_generator}, package.json {installed_generator}"
    )

required = {"react", "react-dom"}
expected_versions = {
    "react": toolchain["runtime"]["react"],
    "react-dom": toolchain["runtime"]["react"],
}

missing = sorted(required - names)
if missing:
    raise SystemExit(f"SPA SBOM missing required runtime components: {', '.join(missing)}")

if not components:
    raise SystemExit("SPA SBOM contains zero components")

version_by_name = {}
for component in components:
    name = component.get("name", "").lower()
    version = component.get("version")
    if name and version:
        version_by_name.setdefault(name, set()).add(version)

for package, expected in expected_versions.items():
    versions = version_by_name.get(package, set())
    if expected not in versions:
        raise SystemExit(
            f"SPA SBOM {package} version mismatch: expected {expected}, found {sorted(versions) or ['missing']}"
        )

print(f"==> SPA SBOM validated ({len(components)} components, react/react-dom present)")
PY
