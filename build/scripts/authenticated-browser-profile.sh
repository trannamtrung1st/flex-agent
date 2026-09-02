#!/usr/bin/env bash
set -euo pipefail

# Cold CI runners pull Keycloak/Postgres over the Docker API. Compose's default
# 60s HTTP timeout aborts that pull and fails OIDC live smoke around one minute.
export COMPOSE_HTTP_TIMEOUT="${COMPOSE_HTTP_TIMEOUT:-300}"
export DOCKER_CLIENT_TIMEOUT="${DOCKER_CLIENT_TIMEOUT:-300}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="${ROOT}/deploy/compose/authenticated-browser.compose.yaml"
PREBUILT_IMAGES_OVERLAY="${ROOT}/deploy/compose/authenticated-browser.prebuilt-images.compose.yaml"
CANDIDATE_OVERLAY="${ROOT}/deploy/compose/authenticated-browser.candidate-dev.compose.yaml"
DEMO_WORK_OVERLAY="${ROOT}/deploy/compose/authenticated-browser.demo-work.compose.yaml"
DEMO_WORK_SEED_FILE="${ROOT}/deploy/compose/authenticated-browser/seed-demo-work.sql"
NGINX_FILE="${ROOT}/deploy/compose/nginx/authenticated-browser.conf"
SEED_FILE="${ROOT}/deploy/compose/authenticated-browser/seed.sql"
SEED_DEMO_WORK="${FLEXAGENT_SEED_DEMO_WORK:-1}"
REALM_TEMPLATE="${ROOT}/deploy/compose/keycloak/flex-agent-realm.json"
GENERATED_DIR="${ROOT}/deploy/compose/authenticated-browser/.generated"
ORIGIN="http://localhost:18080"
PROJECT_NAME="${FLEXAGENT_COMPOSE_PROJECT:-flex-agent-authenticated-browser}"
MODE="canonical"
COMMAND=""
OVERLAYS=()
USE_PREBUILT_IMAGES=0

usage() {
  echo "Usage: $0 [--overlay candidate] [--prebuilt-images] [--mode canonical|candidate] [--project-name NAME] [up|up-smoke|down|reset|status|validate|seed|recreate-api]"
  echo "Set FLEXAGENT_SEED_DEMO_WORK=0 to skip demo-work list fixtures (default: 1)."
  echo "up-smoke stages infra, migrate/seed, then app services without rebuilding when --prebuilt-images is set."
  echo "recreate-api force-recreates only the API (RedirectUri). It does not regenerate secrets or reseed."
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
  if demo_work_enabled; then
    echo -f
    echo "${DEMO_WORK_OVERLAY}"
  fi
  local overlay
  for overlay in "${OVERLAYS[@]+"${OVERLAYS[@]}"}"; do
    echo -f
    echo "${overlay}"
  done
  if [[ "${USE_PREBUILT_IMAGES}" == "1" ]]; then
    echo -f
    echo "${PREBUILT_IMAGES_OVERLAY}"
  fi
  echo --project-name
  echo "${PROJECT_NAME}"
}

demo_work_enabled() {
  [[ "${SEED_DEMO_WORK}" == "1" ]]
}

run_compose() {
  docker compose $(compose_files) "$@"
}

docker_hub_mirror_ref() {
  local image="$1"
  case "${image}" in
    quay.io/*|mcr.microsoft.com/*|mirror.gcr.io/*|ghcr.io/*)
      return 1
      ;;
    */*)
      printf 'mirror.gcr.io/%s\n' "${image}"
      ;;
    *)
      printf 'mirror.gcr.io/library/%s\n' "${image}"
      ;;
  esac
}

pull_image() {
  local image="$1"
  local attempt=1
  local mirror=""
  local pull_timeout="${FLEXAGENT_DOCKER_PULL_TIMEOUT:-120}"
  while [[ "${attempt}" -le 2 ]]; do
    if DOCKER_CLIENT_TIMEOUT="${pull_timeout}" docker pull "${image}"; then
      return 0
    fi
    echo "pull failed for ${image} (attempt ${attempt})" >&2
    attempt=$((attempt + 1))
    sleep $((attempt * 2))
  done
  if mirror="$(docker_hub_mirror_ref "${image}")"; then
    echo "retrying ${image} via ${mirror}" >&2
    if DOCKER_CLIENT_TIMEOUT="${pull_timeout}" docker pull "${mirror}"; then
      docker tag "${mirror}" "${image%%@sha256:*}"
      return 0
    fi
  fi
  echo "unable to pull ${image}" >&2
  return 1
}

ensure_pinned_images() {
  local image
  while read -r image; do
    [[ -n "${image}" ]] || continue
    [[ "${image}" == *@sha256:* ]] || continue
    if docker image inspect "${image}" >/dev/null 2>&1; then
      continue
    fi
    echo "==> Pull ${image}"
    pull_image "${image}"
  done < <(run_compose config --images | awk 'NF && !seen[$0]++')
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
  if demo_work_enabled && [[ ! -f "${DEMO_WORK_OVERLAY}" || ! -f "${DEMO_WORK_SEED_FILE}" ]]; then
    echo "demo-work seed files are missing" >&2
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
  local validate_args=(--compose-json - --nginx "${NGINX_FILE}" --realm "${GENERATED_DIR}/flex-agent-realm.json" --mode "${MODE}" --generated-realm)
  if demo_work_enabled; then
    validate_args+=(--demo-work)
  fi
  run_compose config --format json | python3 "${ROOT}/build/scripts/validate-authenticated-browser-compose.py" "${validate_args[@]}"
}

wait_ready() {
  local attempts=0
  until curl -sf "${ORIGIN}/realms/flex-agent" >/dev/null \
    && curl -sf "${ORIGIN}/auth/session" >/dev/null; do
    attempts=$((attempts + 1))
    if [[ "${attempts}" -ge 90 ]]; then
      echo "timed out waiting for ${ORIGIN}" >&2
      dump_oidc_diagnostics
      exit 1
    fi
    sleep 2
  done
}

seed() {
  run_compose exec -T postgres \
    psql -U flexagent -d flexagent -v ON_ERROR_STOP=1 -f /seed/seed.sql
  if demo_work_enabled; then
    run_compose exec -T postgres \
      psql -U flexagent -d flexagent -v ON_ERROR_STOP=1 -f /seed/seed-demo-work.sql
  fi
}

status() {
  run_compose ps
  curl -sf "${ORIGIN}/auth/session" >/dev/null && echo "session-endpoint:ok" || echo "session-endpoint:down"
  local redirect
  redirect="$(run_compose exec -T api printenv HumanAuthentication__RedirectUri 2>/dev/null || true)"
  if [[ -n "${redirect}" ]]; then
    echo "redirect-uri:${redirect}"
  else
    echo "redirect-uri:unavailable"
  fi
}

recreate_api() {
  require_docker
  if [[ ! -f "${GENERATED_DIR}/secrets/oidc-client-secret" ]]; then
    echo "recreate-api refuses to mint a new OIDC client secret. Generated fixtures are missing; use compose:up only for a fresh stack." >&2
    exit 1
  fi
  run_compose up -d --no-deps --force-recreate api
  wait_ready
  echo "api-recreated mode=${MODE}"
  status
}

compose_up() {
  if [[ "${USE_PREBUILT_IMAGES}" == "1" ]]; then
    run_compose up -d --no-build "$@"
  else
    run_compose up -d --build --renew-anon-volumes "$@"
  fi
}

dump_oidc_diagnostics() {
  echo "==> OIDC compose diagnostics" >&2
  run_compose ps -a >&2 || true
  run_compose logs --tail=200 \
    postgres keycloak-db keycloak seaweedfs api spa nginx >&2 || true

  local keycloak_id
  keycloak_id="$(run_compose ps -aq keycloak 2>/dev/null || true)"
  if [[ -n "${keycloak_id}" ]]; then
    docker inspect \
      --format='status={{.State.Status}} exit={{.State.ExitCode}} oom={{.State.OOMKilled}} error={{.State.Error}} health={{if .State.Health}}{{.State.Health.Status}}{{end}}' \
      "${keycloak_id}" >&2 || true
  fi
}

wait_postgres_healthy() {
  local attempts=0
  until run_compose exec -T postgres pg_isready -U flexagent -d flexagent >/dev/null 2>&1; do
    attempts=$((attempts + 1))
    if [[ "${attempts}" -ge 90 ]]; then
      echo "timed out waiting for postgres" >&2
      dump_oidc_diagnostics
      exit 1
    fi
    sleep 1
  done
}

wait_keycloak_healthy() {
  local attempts=0
  local keycloak_id=""
  local status=""
  local health=""
  while true; do
    keycloak_id="$(run_compose ps -aq keycloak 2>/dev/null || true)"
    if [[ -n "${keycloak_id}" ]]; then
      status="$(docker inspect --format='{{.State.Status}}' "${keycloak_id}" 2>/dev/null || true)"
      health="$(docker inspect --format='{{if .State.Health}}{{.State.Health.Status}}{{end}}' "${keycloak_id}" 2>/dev/null || true)"
      if [[ "${health}" == "healthy" ]]; then
        return 0
      fi
      if [[ "${status}" != "running" ]]; then
        echo "keycloak is not running (status=${status:-missing})" >&2
        dump_oidc_diagnostics
        exit 1
      fi
    fi
    attempts=$((attempts + 1))
    if [[ "${attempts}" -ge 90 ]]; then
      echo "timed out waiting for keycloak health" >&2
      dump_oidc_diagnostics
      exit 1
    fi
    sleep 2
  done
}

up_smoke() {
  cleanup_generated
  validate
  ensure_pinned_images
  if ! compose_up postgres keycloak-db seaweedfs keycloak; then
    echo "infra tier failed" >&2
    dump_oidc_diagnostics
    exit 1
  fi
  wait_postgres_healthy
  wait_keycloak_healthy
  run_compose run --rm --no-deps migrate
  run_compose run --rm --no-deps seed
  if demo_work_enabled; then
    run_compose run --rm --no-deps seed-demo-work
  fi
  if ! compose_up api spa nginx; then
    echo "app tier failed" >&2
    dump_oidc_diagnostics
    exit 1
  fi
  wait_ready
  echo "${ORIGIN}"
}

up() {
  cleanup_generated
  validate
  ensure_pinned_images
  compose_up
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
    --prebuilt-images)
      USE_PREBUILT_IMAGES=1
      shift
      ;;
    -h|--help|help)
      usage
      exit 0
      ;;
    up|up-smoke|down|reset|status|validate|seed|recreate-api)
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
  up-smoke) up_smoke ;;
  down) down ;;
  reset) reset ;;
  status) status ;;
  seed) seed ;;
  recreate-api) recreate_api ;;
esac
