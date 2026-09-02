#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
API_IMAGE="${FLEXAGENT_OIDC_API_IMAGE:-flex-agent-oidc-api:ci}"
SPA_IMAGE="${FLEXAGENT_OIDC_SPA_IMAGE:-flex-agent-oidc-spa:ci}"

if docker image inspect "${API_IMAGE}" >/dev/null 2>&1 \
  && docker image inspect "${SPA_IMAGE}" >/dev/null 2>&1; then
  echo "==> OIDC CI images already present (${API_IMAGE}, ${SPA_IMAGE})"
  exit 0
fi

echo "==> Build OIDC CI images"
export DOCKER_BUILDKIT="${DOCKER_BUILDKIT:-1}"
docker build -f "${ROOT}/deploy/docker/api.Dockerfile" -t "${API_IMAGE}" "${ROOT}"
docker build -f "${ROOT}/deploy/docker/spa.Dockerfile" -t "${SPA_IMAGE}" "${ROOT}"
