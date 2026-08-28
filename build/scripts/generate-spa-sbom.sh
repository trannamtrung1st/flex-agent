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
  # After `corepack enable`, `pnpm exec cyclonedx-npm` shells `pnpm ls --json
  # --all --omit=dev`, which pnpm 9 rejects, so the SBOM parser gets an error
  # string (exit 254). npm ls still understands those flags.
  npm exec -- cyclonedx-npm \
    --omit dev \
    --ignore-npm-errors \
    --output-file "$OUTPUT" \
    web/package.json
)
if [[ ! -s "$OUTPUT" ]]; then
  echo "SPA SBOM was not written to $OUTPUT" >&2
  exit 1
fi

python3 - "$OUTPUT" "$TOOLCHAIN" "$ROOT/package.json" "$ROOT/web/package.json" <<'PY'
import json
import sys

output = sys.argv[1]
toolchain = json.load(open(sys.argv[2]))
package_json = json.load(open(sys.argv[3]))
web_package_json = json.load(open(sys.argv[4]))
bom = json.load(open(output))
components = bom.get("components") or []

def component_package_name(component: dict) -> str:
    name = (component.get("name") or "").lower()
    group = (component.get("group") or "").lower()
    if group and name:
        return f"{group}/{name}"
    return name

names = {component_package_name(component) for component in components if component_package_name(component)}

version_by_name: dict[str, set[str]] = {}
for component in components:
    package_name = component_package_name(component)
    version = component.get("version")
    if package_name and version:
        version_by_name.setdefault(package_name, set()).add(version)

expected_generator = toolchain["supplyChain"]["cyclonedxNpm"]["version"]
installed_generator = (
    package_json.get("devDependencies", {}).get("@cyclonedx/cyclonedx-npm")
)
if installed_generator != expected_generator:
    raise SystemExit(
        f"cyclonedx-npm version mismatch: toolchain {expected_generator}, package.json {installed_generator}"
    )

expected_dependencies = web_package_json.get("dependencies") or {}
if not expected_dependencies:
    raise SystemExit("SPA package.json declares no runtime dependencies to validate")

missing = sorted(
    name for name in expected_dependencies if name.lower() not in names
)
if missing:
    raise SystemExit(
        f"SPA SBOM missing required runtime components: {', '.join(missing)}"
    )

if not components:
    raise SystemExit("SPA SBOM contains zero components")

for package, expected in expected_dependencies.items():
    versions = version_by_name.get(package.lower(), set())
    if expected not in versions:
        raise SystemExit(
            f"SPA SBOM {package} version mismatch: expected {expected}, found {sorted(versions) or ['missing']}"
        )

validated = ", ".join(sorted(expected_dependencies))
print(
    f"==> SPA SBOM validated ({len(components)} components; direct runtime deps: {validated})"
)
PY
