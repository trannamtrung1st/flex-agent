#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "${ROOT}"
export FLEXAGENT_ROOT="${ROOT}"
export FLEXAGENT_OIDC_REQUIRED=1
export FLEXAGENT_COMPOSE_PROJECT="${FLEXAGENT_COMPOSE_PROJECT:-flex-agent-oidc-$$}"
export FLEXAGENT_OIDC_ORIGIN="${FLEXAGENT_OIDC_ORIGIN:-http://localhost:18080}"
export FLEXAGENT_OIDC_CANDIDATE_ORIGIN="${FLEXAGENT_OIDC_CANDIDATE_ORIGIN:-http://127.0.0.1:5274}"
ARTIFACTS="${ROOT}/artifacts/oidc"
mkdir -p "${ARTIFACTS}"
VITE_PID=""
CLEANED=0

require_prereqs() {
  if ! command -v docker >/dev/null 2>&1 || ! docker compose version >/dev/null 2>&1; then
    echo "verify:oidc requires Docker Compose" >&2
    exit 1
  fi
  if ! command -v python3 >/dev/null 2>&1; then
    echo "verify:oidc requires python3" >&2
    exit 1
  fi
  if ! command -v curl >/dev/null 2>&1; then
    echo "verify:oidc requires curl" >&2
    exit 1
  fi
  if ! command -v pnpm >/dev/null 2>&1; then
    echo "verify:oidc requires pnpm" >&2
    exit 1
  fi
}

cleanup() {
  if [[ "${CLEANED}" == "1" ]]; then
    return
  fi
  CLEANED=1
  if [[ -n "${VITE_PID}" ]] && kill -0 "${VITE_PID}" 2>/dev/null; then
    kill "${VITE_PID}" 2>/dev/null || true
    wait "${VITE_PID}" 2>/dev/null || true
  fi
  bash "${ROOT}/build/scripts/authenticated-browser-profile.sh" --project-name "${FLEXAGENT_COMPOSE_PROJECT}" down || true
}

trap cleanup EXIT

redact_logs() {
  sed -E 's/(access_token|id_token|refresh_token|logout_token|client_secret|password)[=:][^[:space:]&"]+/\1=[redacted]/gi' || true
}

require_prereqs

echo "==> OIDC-E2E-07 rendered-config negative control"
python3 "${ROOT}/scripts/test_authenticated_browser_compose.py"
printf '{"title":"OIDC-E2E-07 required-gate negative control [OIDC-E2E-07]","ok":true}\n' > "${ARTIFACTS}/oidc-e2e-07.json"

echo "==> OIDC-E2E-07 injected-failure cleanup"
bash "${ROOT}/scripts/test_oidc_cleanup_on_failure.sh"

echo "==> Keycloak logout-token compatibility"
dotnet restore "${ROOT}/tests/Integration/FlexAgent.Keycloak.Integration.Tests/FlexAgent.Keycloak.Integration.Tests.csproj" --locked-mode
dotnet test --project "${ROOT}/tests/Integration/FlexAgent.Keycloak.Integration.Tests/FlexAgent.Keycloak.Integration.Tests.csproj" -c Release -- --fail-skips on

echo "==> Canonical authenticated-browser profile"
bash "${ROOT}/build/scripts/authenticated-browser-profile.sh" --project-name "${FLEXAGENT_COMPOSE_PROJECT}" up

echo "==> Canonical Playwright OIDC acceptance"
pnpm --filter @flex-agent/oidc-playwright exec playwright install chromium
FLEXAGENT_OIDC_REPORT="${ARTIFACTS}/canonical-playwright.json" \
  pnpm --filter @flex-agent/oidc-playwright test:canonical
python3 "${ROOT}/build/scripts/assert-oidc-case-manifest.py" \
  --report "${ARTIFACTS}/canonical-playwright.json" \
  --require OIDC-E2E-01 OIDC-E2E-02 OIDC-E2E-03 OIDC-E2E-04 OIDC-E2E-05A OIDC-E2E-05B OIDC-E2E-06

echo "==> Candidate/non-Production transition overlay"
bash "${ROOT}/build/scripts/authenticated-browser-profile.sh" \
  --project-name "${FLEXAGENT_COMPOSE_PROJECT}" \
  --overlay candidate \
  up
VITE_DEV_API_PROXY=http://127.0.0.1:18080 pnpm --filter @flex-agent/web exec -- vite --host 127.0.0.1 --port 5274 >/tmp/flex-agent-oidc-vite.log 2>&1 &
VITE_PID=$!
attempts=0
until curl -sf "${FLEXAGENT_OIDC_CANDIDATE_ORIGIN}" >/dev/null; do
  attempts=$((attempts + 1))
  if [[ "${attempts}" -ge 60 ]]; then
    echo "timed out waiting for candidate Vite server" >&2
    redact_logs </tmp/flex-agent-oidc-vite.log >&2 || true
    exit 1
  fi
  sleep 2
done

FLEXAGENT_OIDC_REPORT="${ARTIFACTS}/candidate-playwright.json" \
  pnpm --filter @flex-agent/oidc-playwright test:candidate
python3 "${ROOT}/build/scripts/assert-oidc-case-manifest.py" \
  --report "${ARTIFACTS}/candidate-playwright.json" \
  --require OIDC-CANDIDATE-01

echo "verify:oidc complete"
