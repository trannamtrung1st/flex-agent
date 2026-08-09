#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TOOLCHAIN="$ROOT/build/toolchain.json"
INSTALL_DIR="${INSTALL_DIR:-$ROOT/.tools/supply-chain/bin}"

detect_platform_key() {
  local os arch
  os="$(uname -s | tr '[:upper:]' '[:lower:]')"
  arch="$(uname -m)"

  case "$os" in
    linux)
      case "$arch" in
        x86_64|amd64) echo "linux_amd64" ;;
        *) echo "Unsupported Linux architecture: $arch" >&2; exit 1 ;;
      esac
      ;;
    darwin)
      case "$arch" in
        x86_64) echo "darwin_amd64" ;;
        arm64) echo "darwin_arm64" ;;
        *) echo "Unsupported macOS architecture: $arch" >&2; exit 1 ;;
      esac
      ;;
    *)
      echo "Unsupported OS: $os" >&2
      exit 1
      ;;
  esac
}

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

file_sha256() {
  local file="$1"
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$file" | awk '{print $1}'
  else
    shasum -a 256 "$file" | awk '{print $1}'
  fi
}

binary_is_verified() {
  local tool="$1"
  local platform_key="$2"
  local binary="$INSTALL_DIR/$tool"
  local expected

  if [[ ! -f "$binary" ]]; then
    return 1
  fi

  expected="$(read_toolchain "supplyChain.${tool}.artifacts.${platform_key}.binarySha256")"
  if [[ "$(file_sha256 "$binary")" != "$expected" ]]; then
    echo "Checksum mismatch for ${tool} binary in ${INSTALL_DIR}" >&2
    return 1
  fi

  if [[ ! -x "$binary" ]]; then
    return 1
  fi

  return 0
}

PLATFORM_KEY="$(detect_platform_key)"
needs_install=false
for tool in syft grype gitleaks; do
  if ! binary_is_verified "$tool" "$PLATFORM_KEY"; then
    needs_install=true
    break
  fi
done

if [[ "$needs_install" == true ]]; then
  INSTALL_DIR="$INSTALL_DIR" bash "$ROOT/build/scripts/install-supply-chain-tools.sh"
fi

for tool in syft grype gitleaks; do
  if ! binary_is_verified "$tool" "$PLATFORM_KEY"; then
    echo "Failed to provision checksum-verified ${tool} in ${INSTALL_DIR}" >&2
    exit 1
  fi
done

echo "==> Checksum-verified supply-chain tools ready in ${INSTALL_DIR}"
