#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=matches-implementation-path.sh
source "$SCRIPT_DIR/matches-implementation-path.sh"

failures=0

assert_matches() {
  local path="$1"
  if matches_implementation_path "$path"; then
    return 0
  fi

  echo "expected implementation path match: $path" >&2
  failures=$((failures + 1))
}

assert_no_match() {
  local path="$1"
  if ! matches_implementation_path "$path"; then
    return 0
  fi

  echo "expected implementation path non-match: $path" >&2
  failures=$((failures + 1))
}

assert_matches gitleaks.toml
assert_matches .gitleaksignore
assert_matches build/scripts/verify-supply-chain.sh
assert_matches contracts/schemas/v1/example.schema.json
assert_matches src/Hosts/FlexAgent.Api/Program.cs

assert_no_match README.md
assert_no_match docs/contributing/workspace.md
assert_no_match .github/workflows/documentation.yml

if [[ "$failures" -gt 0 ]]; then
  echo "detect-implementation-changes path classifier failed ($failures)" >&2
  exit 1
fi

echo "detect-implementation-changes path classifier passed."
