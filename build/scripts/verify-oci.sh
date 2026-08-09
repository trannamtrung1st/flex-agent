#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

API_PORT="$(python3 -c 'import socket; s=socket.socket(); s.bind(("", 0)); print(s.getsockname()[1]); s.close()')"
WORKER_PORT="$((API_PORT + 1))"
SPA_PORT="$((API_PORT + 2))"

cleanup() {
  docker rm flex-agent-oci-api flex-agent-oci-worker flex-agent-oci-spa >/dev/null 2>&1 || true
}
trap cleanup EXIT

bash "$ROOT/build/scripts/build-oci-images.sh" >/dev/null

echo "==> Start containers"
docker run -d --name flex-agent-oci-api -p "${API_PORT}:8080" flex-agent-oci-api:local >/dev/null
docker run -d --name flex-agent-oci-worker -p "${WORKER_PORT}:8080" flex-agent-oci-worker:local >/dev/null
docker run -d --name flex-agent-oci-spa -p "${SPA_PORT}:8080" flex-agent-oci-spa:local >/dev/null

wait_for_endpoint() {
  local url="$1"
  for _ in $(seq 1 30); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done
  return 1
}

echo "==> Wait for endpoints (external health probes)"
wait_for_endpoint "http://localhost:${API_PORT}/health/live"
wait_for_endpoint "http://localhost:${API_PORT}/health/ready"
wait_for_endpoint "http://localhost:${WORKER_PORT}/health/live"
wait_for_endpoint "http://localhost:${WORKER_PORT}/health/ready"
wait_for_endpoint "http://localhost:${SPA_PORT}/"

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

assert_graceful_exit() {
  local name="$1"
  local code
  code="$(docker inspect --format='{{.State.ExitCode}}' "$name")"
  if [[ "$code" != "0" && "$code" != "143" ]]; then
    echo "Unexpected exit code for ${name}: ${code}" >&2
    docker logs "$name" || true
    return 1
  fi
}

graceful_stop() {
  local name="$1"
  echo "==> Graceful shutdown (SIGTERM): ${name}"
  docker stop -t 10 "$name" >/dev/null
  assert_graceful_exit "$name"
  docker rm "$name" >/dev/null
}

graceful_stop flex-agent-oci-api
graceful_stop flex-agent-oci-worker
graceful_stop flex-agent-oci-spa

trap - EXIT

echo "==> OCI image SBOM and vulnerability scan"
bash "$ROOT/build/scripts/scan-oci-image-sboms.sh"

echo "==> OCI verification complete"
