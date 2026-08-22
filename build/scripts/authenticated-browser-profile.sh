#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="${ROOT}/deploy/compose/authenticated-browser.compose.yaml"
NGINX_FILE="${ROOT}/deploy/compose/nginx/authenticated-browser.conf"
SEED_FILE="${ROOT}/deploy/compose/authenticated-browser/seed.sql"
ORIGIN="http://localhost:18080"
COMPOSE=(docker compose -f "${COMPOSE_FILE}" --project-name flex-agent-authenticated-browser)

usage() {
  echo "Usage: $0 [up|down|reset|status|validate|seed]"
}

validate() {
  if [[ ! -f "${COMPOSE_FILE}" || ! -f "${NGINX_FILE}" || ! -f "${SEED_FILE}" ]]; then
    echo "authenticated browser profile files are missing" >&2
    exit 1
  fi

  if grep -q '5432:5432' "${COMPOSE_FILE}"; then
    echo "host database port publication is not permitted" >&2
    exit 1
  fi

  validate_loopback_gateway

  if grep -q '/browser' "${COMPOSE_FILE}" "${NGINX_FILE}"; then
    echo "synthetic browser route is not permitted in this profile" >&2
    exit 1
  fi

  if ! grep -q 'http://localhost:18080/auth/callback' "${COMPOSE_FILE}"; then
    echo "canonical OIDC callback is missing" >&2
    exit 1
  fi
}

validate_loopback_gateway() {
  if ! grep -q '127.0.0.1:18080:80' "${COMPOSE_FILE}"; then
    echo "gateway must bind only to loopback 127.0.0.1:18080" >&2
    exit 1
  fi

  local non_loopback
  non_loopback="$(grep -oE '([0-9.]+|\[::\]):18080:80|"18080:80"|[[:space:]]18080:80' "${COMPOSE_FILE}" | tr -d '"[:space:]' | grep -vx '127.0.0.1:18080:80' || true)"
  if [[ -n "${non_loopback}" ]]; then
    echo "non-loopback gateway publication is not permitted: ${non_loopback}" >&2
    exit 1
  fi

  if grep -q '0.0.0.0:18080:80' "${COMPOSE_FILE}"; then
    echo "non-loopback gateway publication is not permitted: 0.0.0.0:18080:80" >&2
    exit 1
  fi

  if ! command -v docker >/dev/null 2>&1 || ! docker compose version >/dev/null 2>&1; then
    return 0
  fi

  if ! command -v python3 >/dev/null 2>&1; then
    echo "python3 is required to inspect docker compose config" >&2
    exit 1
  fi

  "${COMPOSE[@]}" config --format json | python3 -c '
import json, sys

config = json.load(sys.stdin)
seen = False
for service in config.get("services", {}).values():
    for port in service.get("ports") or []:
        published = str(port.get("published") or "")
        target = str(port.get("target") or "")
        host_ip = port.get("host_ip") or ""
        if published != "18080" and target != "80":
            continue
        if host_ip != "127.0.0.1" or published != "18080" or target != "80":
            print(
                "non-loopback gateway publication is not permitted: "
                f"{host_ip or "*"}:{published}:{target}",
                file=sys.stderr,
            )
            sys.exit(1)
        seen = True
if not seen:
    print("gateway must bind only to loopback 127.0.0.1:18080", file=sys.stderr)
    sys.exit(1)
'
}

wait_ready() {
  local attempts=0
  until curl -sf "${ORIGIN}/realms/flex-agent" >/dev/null \
    && curl -sf "${ORIGIN}/auth/session" >/dev/null; do
    attempts=$((attempts + 1))
    if [[ "${attempts}" -ge 90 ]]; then
      echo "timed out waiting for ${ORIGIN}" >&2
      "${COMPOSE[@]}" ps >&2 || true
      exit 1
    fi
    sleep 2
  done
}

seed() {
  "${COMPOSE[@]}" exec -T postgres \
    psql -U flexagent -d flexagent -v ON_ERROR_STOP=1 -f /seed/seed.sql
}

status() {
  "${COMPOSE[@]}" ps
  curl -sf "${ORIGIN}/auth/session" || true
  echo
}

up() {
  validate
  "${COMPOSE[@]}" up -d --build
  wait_ready
  echo "${ORIGIN}"
}

down() {
  "${COMPOSE[@]}" down --remove-orphans
}

reset() {
  "${COMPOSE[@]}" down --volumes --remove-orphans
  up
}

case "${1:-up}" in
  validate) validate ;;
  up) up ;;
  down) down ;;
  reset) reset ;;
  status) status ;;
  seed) seed ;;
  -h|--help|help)
    usage
    ;;
  *)
    usage >&2
    exit 1
    ;;
esac
