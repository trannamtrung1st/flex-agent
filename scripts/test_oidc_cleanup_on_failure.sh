#!/usr/bin/env bash
# OIDC-E2E-07: injected wrapper failure still tears down Compose.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="flex-agent-oidc-e2e07-$$"
PROFILE="${ROOT}/build/scripts/authenticated-browser-profile.sh"

cleanup() {
  bash "${PROFILE}" --project-name "${PROJECT}" down >/dev/null 2>&1 || true
}
trap cleanup EXIT

set +e
(
  trap 'bash "${PROFILE}" --project-name "${PROJECT}" down >/dev/null 2>&1 || true' EXIT
  bash "${PROFILE}" --project-name "${PROJECT}" up >/dev/null
  echo "injected OIDC-E2E-07 failure" >&2
  exit 42
)
status=$?
set -e

if [[ "${status}" -eq 0 ]]; then
  echo "expected injected failure to be non-zero" >&2
  exit 1
fi

remaining="$(docker ps -a --filter "name=${PROJECT}" --format '{{.Names}}' || true)"
if [[ -n "${remaining}" ]]; then
  echo "injected failure left containers: ${remaining}" >&2
  exit 1
fi

echo "OIDC-E2E-07 injected-failure cleanup ok"
