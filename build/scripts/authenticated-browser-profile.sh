#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="${ROOT}/deploy/compose/authenticated-browser.compose.yaml"
CANDIDATE_OVERLAY="${ROOT}/deploy/compose/authenticated-browser.candidate-dev.compose.yaml"
NGINX_FILE="${ROOT}/deploy/compose/nginx/authenticated-browser.conf"
SEED_FILE="${ROOT}/deploy/compose/authenticated-browser/seed.sql"
REALM_TEMPLATE="${ROOT}/deploy/compose/keycloak/flex-agent-realm.json"
GENERATED_DIR="${ROOT}/deploy/compose/authenticated-browser/.generated"
ORIGIN="http://localhost:18080"
PROJECT_NAME="${FLEXAGENT_COMPOSE_PROJECT:-flex-agent-authenticated-browser}"
MODE="canonical"
COMMAND=""
OVERLAYS=()

usage() {
  echo "Usage: $0 [--overlay candidate] [--mode canonical|candidate] [--project-name NAME] [up|down|reset|status|validate|seed]"
}

require_docker() {
  if ! command -v docker >/dev/null 2>&1 || ! docker compose version >/dev/null 2>&1; then
    echo "Docker Compose is required for the authenticated browser profile" >&2
    exit 1
  fi
  if ! command -v python3 >/dev/null 2>&1; then
    echo "python3 is required to inspect docker compose config" >&2
    exit 1
  fi
}

compose_files() {
  echo -f
  echo "${COMPOSE_FILE}"
  local overlay
  for overlay in "${OVERLAYS[@]+"${OVERLAYS[@]}"}"; do
    echo -f
    echo "${overlay}"
  done
  echo --project-name
  echo "${PROJECT_NAME}"
}

run_compose() {
  docker compose $(compose_files) "$@"
}

ensure_generated_fixtures() {
  mkdir -p "${GENERATED_DIR}/secrets"
  chmod 700 "${GENERATED_DIR}" "${GENERATED_DIR}/secrets"
  if [[ ! -f "${GENERATED_DIR}/secrets/oidc-client-secret" ]]; then
    openssl rand -base64 32 | tr -d '\n' > "${GENERATED_DIR}/secrets/oidc-client-secret"
    chmod 600 "${GENERATED_DIR}/secrets/oidc-client-secret"
  fi
  if [[ ! -f "${GENERATED_DIR}/keycloak.env" ]]; then
    local admin_password
    admin_password="$(openssl rand -base64 24 | tr -d '\n')"
    umask 077
    printf 'KC_BOOTSTRAP_ADMIN_USERNAME=admin\nKC_BOOTSTRAP_ADMIN_PASSWORD=%s\n' "${admin_password}" > "${GENERATED_DIR}/keycloak.env"
    chmod 600 "${GENERATED_DIR}/keycloak.env"
  fi
  python3 "${ROOT}/build/scripts/render-oidc-realm.py" \
    --template "${REALM_TEMPLATE}" \
    --secret-file "${GENERATED_DIR}/secrets/oidc-client-secret" \
    --output "${GENERATED_DIR}/flex-agent-realm.json"
}

cleanup_generated() {
  rm -rf "${GENERATED_DIR}"
}

validate() {
  require_docker
  if [[ ! -f "${COMPOSE_FILE}" || ! -f "${NGINX_FILE}" || ! -f "${SEED_FILE}" || ! -f "${REALM_TEMPLATE}" ]]; then
    echo "authenticated browser profile files are missing" >&2
    exit 1
  fi
  if grep -q '5432:5432' "${COMPOSE_FILE}"; then
    echo "host database port publication is not permitted" >&2
    exit 1
  fi
  if grep -q '/browser' "${COMPOSE_FILE}"; then
    echo "synthetic browser route is not permitted in this profile" >&2
    exit 1
  fi
  if grep -q 'proxy_pass .*/browser' "${NGINX_FILE}"; then
    echo "synthetic browser route is not permitted in this profile" >&2
    exit 1
  fi
  ensure_generated_fixtures
  run_compose config --format json | python3 "${ROOT}/build/scripts/validate-authenticated-browser-compose.py" \
    --compose-json - \
    --nginx "${NGINX_FILE}" \
    --realm "${GENERATED_DIR}/flex-agent-realm.json" \
    --mode "${MODE}" \
    --generated-realm
}

wait_ready() {
  local attempts=0
  until curl -sf "${ORIGIN}/realms/flex-agent" >/dev/null \
    && curl -sf "${ORIGIN}/auth/session" >/dev/null; do
    attempts=$((attempts + 1))
    if [[ "${attempts}" -ge 90 ]]; then
      echo "timed out waiting for ${ORIGIN}" >&2
      run_compose ps >&2 || true
      run_compose logs --tail=80 nginx api keycloak >&2 || true
      exit 1
    fi
    sleep 2
  done
}

seed() {
  run_compose exec -T postgres \
    psql -U flexagent -d flexagent -v ON_ERROR_STOP=1 -f /seed/seed.sql
}

status() {
  run_compose ps
  curl -sf "${ORIGIN}/auth/session" >/dev/null && echo "session-endpoint:ok" || echo "session-endpoint:down"
}

up() {
  validate
  run_compose up -d --build
  wait_ready
  echo "${ORIGIN}"
}

down() {
  if command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1; then
    run_compose down --remove-orphans --volumes || true
  fi
  cleanup_generated
}

reset() {
  down
  up
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --overlay)
      if [[ "${2:-}" == "candidate" ]]; then
        OVERLAYS+=("${CANDIDATE_OVERLAY}")
        MODE="candidate"
      else
        OVERLAYS+=("${2:?overlay path required}")
      fi
      shift 2
      ;;
    --mode)
      MODE="${2:?mode required}"
      shift 2
      ;;
    --project-name)
      PROJECT_NAME="${2:?project name required}"
      shift 2
      ;;
    -h|--help|help)
      usage
      exit 0
      ;;
    up|down|reset|status|validate|seed)
      COMMAND="$1"
      shift
      ;;
    *)
      usage >&2
      exit 1
      ;;
  esac
done

case "${COMMAND:-up}" in
  validate) validate ;;
  up) up ;;
  down) down ;;
  reset) reset ;;
  status) status ;;
  seed) seed ;;
esac
