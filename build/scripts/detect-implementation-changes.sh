#!/usr/bin/env bash
set -euo pipefail

# Emits implementation=true|false to $GITHUB_OUTPUT when set; otherwise prints to stdout.
emit() {
  if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
    echo "implementation=$1" >>"$GITHUB_OUTPUT"
  else
    echo "$1"
  fi
}

matches_implementation_path() {
  local path="$1"
  case "$path" in
    src/*|web/*|tests/*|build/*|deploy/*) return 0 ;;
    .github/workflows/implementation.yml) return 0 ;;
    global.json|Directory.Build.props|Directory.Build.targets|Directory.Packages.props|FlexAgent.slnx|nuget.config) return 0 ;;
    package.json|pnpm-lock.yaml|pnpm-workspace.yaml|.nvmrc) return 0 ;;
    *) return 1 ;;
  esac
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
