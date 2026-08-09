#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

API_PORT="$(python3 -c 'import socket; s=socket.socket(); s.bind(("", 0)); print(s.getsockname()[1]); s.close()')"
WORKER_PORT="$((API_PORT + 1))"
SPA_PORT="$((API_PORT + 2))"

cleanup() {
  docker rm -f flex-agent-oci-api flex-agent-oci-worker flex-agent-oci-spa >/dev/null 2>&1 || true
}
trap cleanup EXIT

echo "==> Build OCI images"
docker build -f deploy/docker/api.Dockerfile -t flex-agent-oci-api:local "$ROOT" >/dev/null
docker build -f deploy/docker/worker.Dockerfile -t flex-agent-oci-worker:local "$ROOT" >/dev/null
docker build -f deploy/docker/spa.Dockerfile -t flex-agent-oci-spa:local "$ROOT" >/dev/null

echo "==> Start containers"
docker run -d --name flex-agent-oci-api -p "${API_PORT}:8080" flex-agent-oci-api:local >/dev/null
docker run -d --name flex-agent-oci-worker -p "${WORKER_PORT}:8080" flex-agent-oci-worker:local >/dev/null
docker run -d --name flex-agent-oci-spa -p "${SPA_PORT}:8080" flex-agent-oci-spa:local >/dev/null

wait_for_healthy() {
  local name="$1"
  for _ in $(seq 1 30); do
    local status
    status="$(docker inspect --format='{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$name")"
    if [[ "$status" == "healthy" ]]; then
      return 0
    fi
    sleep 1
  done
  docker logs "$name" || true
  return 1
}

echo "==> Wait for health checks"
wait_for_healthy flex-agent-oci-api
wait_for_healthy flex-agent-oci-worker
wait_for_healthy flex-agent-oci-spa

echo "==> Probe endpoints"
curl -fsS "http://localhost:${API_PORT}/health/live" >/dev/null
curl -fsS "http://localhost:${API_PORT}/health/ready" >/dev/null
curl -fsS "http://localhost:${WORKER_PORT}/health/live" >/dev/null
curl -fsS "http://localhost:${WORKER_PORT}/health/ready" >/dev/null
curl -fsS "http://localhost:${SPA_PORT}/" >/dev/null

echo "==> Verify runtime users"
[[ "$(docker exec flex-agent-oci-api whoami)" == "appuser" ]]
[[ "$(docker exec flex-agent-oci-worker whoami)" == "appuser" ]]
[[ "$(docker exec flex-agent-oci-spa whoami)" == "nginx" ]]

echo "==> Verify publish output excludes development settings"
docker exec flex-agent-oci-api sh -c 'test ! -f /app/appsettings.Development.json'
docker exec flex-agent-oci-worker sh -c 'test ! -f /app/appsettings.Development.json'

echo "==> Verify SPA image excludes source maps"
docker exec flex-agent-oci-spa sh -c '! find /usr/share/nginx/html -name "*.map" | grep -q .'

echo "==> OCI verification complete"
