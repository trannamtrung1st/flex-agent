#!/usr/bin/env bash
# Blocking CI OIDC smoke gate. Target wall time: under 3 minutes with warm caches.
# Full OIDC-E2E coverage remains in verify-oidc.sh (local / release).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "${ROOT}"

if command -v corepack >/dev/null 2>&1; then
  corepack enable
  corepack prepare pnpm@9.6.0 --activate
  hash -r
fi

export FLEXAGENT_ROOT="${ROOT}"
export FLEXAGENT_OIDC_REQUIRED=1
export FLEXAGENT_COMPOSE_PROJECT="${FLEXAGENT_COMPOSE_PROJECT:-flex-agent-oidc-ci-$$}"
export FLEXAGENT_OIDC_ORIGIN="${FLEXAGENT_OIDC_ORIGIN:-http://localhost:18080}"
export FLEXAGENT_SEED_DEMO_WORK="${FLEXAGENT_SEED_DEMO_WORK:-0}"
ARTIFACTS="${ROOT}/artifacts/oidc"
PROFILE="${ROOT}/build/scripts/authenticated-browser-profile.sh"
mkdir -p "${ARTIFACTS}"

run_pnpm() {
  if command -v pnpm >/dev/null 2>&1; then
    command pnpm "$@"
  else
    corepack pnpm "$@"
  fi
}

require_prereqs() {
  local missing=()
  if ! command -v docker >/dev/null 2>&1; then
    missing+=("docker")
  elif ! docker compose version >/dev/null 2>&1; then
    missing+=("docker-compose-v2")
  fi
  if ! command -v python3 >/dev/null 2>&1; then
    missing+=("python3")
  fi
  if ! command -v curl >/dev/null 2>&1; then
    missing+=("curl")
  fi
  if ((${#missing[@]} > 0)); then
    echo "::error::verify:oidc:ci missing prerequisites: ${missing[*]}"
    exit 1
  fi
}

cleanup() {
  bash "${PROFILE}" --project-name "${FLEXAGENT_COMPOSE_PROJECT}" down || true
}

trap cleanup EXIT

require_prereqs

if [[ "${FLEXAGENT_OIDC_SKIP_STATIC:-0}" != "1" ]]; then
  echo "==> OIDC-E2E-07 static negatives"
  python3 "${ROOT}/scripts/test_authenticated_browser_compose.py"
  printf '{"title":"OIDC-E2E-07 CI smoke static gate [OIDC-E2E-07]","ok":true}\n' > "${ARTIFACTS}/oidc-e2e-07-ci.json"

  echo "==> Rendered compose validation"
  bash "${PROFILE}" --project-name "${FLEXAGENT_COMPOSE_PROJECT}" validate
fi

if [[ "${FLEXAGENT_OIDC_SKIP_LIVE:-0}" == "1" ]]; then
  echo "verify:oidc:ci static complete"
  exit 0
fi

echo "==> Prepare CI application images"
bash "${ROOT}/build/scripts/prepare-oidc-ci-images.sh"

echo "==> Authenticated-browser CI smoke stack"
bash "${PROFILE}" \
  --project-name "${FLEXAGENT_COMPOSE_PROJECT}" \
  --prebuilt-images \
  up-smoke

echo "==> Playwright PKCE smoke (OIDC-E2E-01)"
if [[ "${CI:-}" == "true" ]]; then
  run_pnpm --filter @flex-agent/oidc-playwright exec playwright install --with-deps chromium
else
  run_pnpm --filter @flex-agent/oidc-playwright exec playwright install chromium
fi
FLEXAGENT_OIDC_REPORT="${ARTIFACTS}/ci-smoke-playwright.json" \
  run_pnpm --filter @flex-agent/oidc-playwright exec playwright test \
    --project=canonical \
    --grep 'OIDC-E2E-01'
python3 "${ROOT}/build/scripts/assert-oidc-case-manifest.py" \
  --report "${ARTIFACTS}/ci-smoke-playwright.json" \
  --require OIDC-E2E-01

echo "verify:oidc:ci complete"
