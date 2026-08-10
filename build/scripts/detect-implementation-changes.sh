#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=matches-implementation-path.sh
source "$SCRIPT_DIR/matches-implementation-path.sh"

bash "$SCRIPT_DIR/detect-implementation-changes.test.sh" >/dev/null

# Emits implementation=true|false to $GITHUB_OUTPUT when set; otherwise prints to stdout.
emit() {
  if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
    echo "implementation=$1" >>"$GITHUB_OUTPUT"
  else
    echo "$1"
  fi
}

BASE_SHA="${BASE_SHA:-${GITHUB_EVENT_BEFORE:-}}"
HEAD_SHA="${HEAD_SHA:-${GITHUB_SHA:-HEAD}}"
EVENT_NAME="${EVENT_NAME:-${GITHUB_EVENT_NAME:-local}}"

if [[ "$EVENT_NAME" == "pull_request" ]]; then
  BASE_SHA="${BASE_SHA:-${GITHUB_BASE_SHA:-}}"
fi

if [[ -z "$BASE_SHA" || "$BASE_SHA" == "0000000000000000000000000000000000000000" ]]; then
  emit true
  exit 0
fi

if ! git cat-file -e "$BASE_SHA^{commit}" 2>/dev/null; then
  emit true
  exit 0
fi

while IFS= read -r path; do
  [[ -z "$path" ]] && continue
  if matches_implementation_path "$path"; then
    emit true
    exit 0
  fi
done < <(git diff --name-only "$BASE_SHA" "$HEAD_SHA")

emit false
