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

assert_matches .github/workflows/implementation.yml
assert_matches gitleaks.toml
assert_matches .gitleaksignore
assert_matches build/scripts/verify-supply-chain.sh
assert_matches scripts/test_authenticated_browser_compose.py
assert_matches tests/Browser/FlexAgent.Oidc.Playwright/package.json
assert_matches contracts/schemas/v1/example.schema.json
assert_matches src/Hosts/FlexAgent.Api/Program.cs
assert_matches web/package.json
assert_matches web/package.json

assert_no_match README.md
assert_no_match docs/contributing/workspace.md
assert_no_match .github/workflows/documentation.yml

if [[ "$failures" -gt 0 ]]; then
  echo "detect-implementation-changes path classifier failed ($failures)" >&2
  exit 1
fi

echo "detect-implementation-changes path classifier passed."

head_sha="$(git rev-parse HEAD)"
assert_event_output() {
  local event="$1"
  local expected="$2"
  local got
  got="$(
    SKIP_IMPLEMENTATION_CHANGE_SELFTEST=1 \
      EVENT_NAME="$event" \
      BASE_SHA="$head_sha" \
      HEAD_SHA="$head_sha" \
      bash "$SCRIPT_DIR/detect-implementation-changes.sh"
  )"
  if [[ "$got" == "$expected" ]]; then
    return 0
  fi

  echo "expected implementation=$expected for EVENT_NAME=$event with an empty diff, got: $got" >&2
  failures=$((failures + 1))
}

assert_event_output push true
assert_event_output local true
assert_event_output pull_request false

if [[ "$failures" -gt 0 ]]; then
  echo "detect-implementation-changes event policy failed ($failures)" >&2
  exit 1
fi

echo "detect-implementation-changes event policy passed."
