#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TOOLCHAIN="$ROOT/build/toolchain.json"
INSTALL_DIR="${INSTALL_DIR:-$ROOT/.tools/supply-chain/bin}"
TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

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

verify_sha256() {
  local file="$1"
  local expected="$2"
  local actual
  if command -v sha256sum >/dev/null 2>&1; then
    actual="$(sha256sum "$file" | awk '{print $1}')"
  else
    actual="$(shasum -a 256 "$file" | awk '{print $1}')"
  fi
  if [[ "$actual" != "$expected" ]]; then
    echo "Checksum mismatch for $(basename "$file")" >&2
    echo "expected: $expected" >&2
    echo "actual:   $actual" >&2
    exit 1
  fi
}

install_archive_tool() {
  local tool="$1"
  local platform_key="$2"
  local version archive checksum binary_checksum url extracted binary
  version="$(read_toolchain "supplyChain.${tool}.version")"
  archive="$(read_toolchain "supplyChain.${tool}.artifacts.${platform_key}.archive")"
  checksum="$(read_toolchain "supplyChain.${tool}.artifacts.${platform_key}.sha256")"
  binary_checksum="$(read_toolchain "supplyChain.${tool}.artifacts.${platform_key}.binarySha256")"

  case "$tool" in
    syft) url="https://github.com/anchore/syft/releases/download/v${version}/${archive}" ;;
    grype) url="https://github.com/anchore/grype/releases/download/v${version}/${archive}" ;;
    gitleaks) url="https://github.com/gitleaks/gitleaks/releases/download/v${version}/${archive}" ;;
    *) echo "Unknown tool: $tool" >&2; exit 1 ;;
  esac

  echo "==> Installing ${tool} ${version} (${platform_key})"
  curl -fsSL "$url" -o "$TMP_DIR/$archive"
  verify_sha256 "$TMP_DIR/$archive" "$checksum"

  case "$tool" in
    syft|grype)
      tar -xzf "$TMP_DIR/$archive" -C "$TMP_DIR"
      binary="$TMP_DIR/${tool}"
      ;;
    gitleaks)
      tar -xzf "$TMP_DIR/$archive" -C "$TMP_DIR" gitleaks
      binary="$TMP_DIR/gitleaks"
      ;;
  esac

  install -m 0755 "$binary" "$INSTALL_DIR/${tool}"
  verify_sha256 "$INSTALL_DIR/${tool}" "$binary_checksum"
  "$INSTALL_DIR/${tool}" version 2>/dev/null || "$INSTALL_DIR/${tool}" --version
}

PLATFORM_KEY="$(detect_platform_key)"
mkdir -p "$INSTALL_DIR"
install_archive_tool syft "$PLATFORM_KEY"
install_archive_tool grype "$PLATFORM_KEY"
install_archive_tool gitleaks "$PLATFORM_KEY"

echo "==> Verified supply-chain tools installed to ${INSTALL_DIR}"
