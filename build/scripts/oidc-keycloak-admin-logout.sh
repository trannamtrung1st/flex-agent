#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="${ROOT}/deploy/compose/authenticated-browser.compose.yaml"
PROJECT_NAME="${FLEXAGENT_COMPOSE_PROJECT:-flex-agent-authenticated-browser}"
USERNAME="${1:?synthetic username required}"

docker compose -f "${COMPOSE_FILE}" --project-name "${PROJECT_NAME}" exec -T \
  -e FLEXAGENT_LOGOUT_USER="${USERNAME}" \
  keycloak bash -lc '
set -euo pipefail
/opt/keycloak/bin/kcadm.sh config credentials \
  --server http://127.0.0.1:8080 \
  --realm master \
  --user "${KC_BOOTSTRAP_ADMIN_USERNAME}" \
  --password "${KC_BOOTSTRAP_ADMIN_PASSWORD}" \
  >/dev/null
ID="$(/opt/keycloak/bin/kcadm.sh get users -r flex-agent -q username="${FLEXAGENT_LOGOUT_USER}" --fields id --format csv --noquotes | tail -n 1)"
test -n "${ID}"
/opt/keycloak/bin/kcadm.sh create users/"${ID}"/logout -r flex-agent >/dev/null
'
