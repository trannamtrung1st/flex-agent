#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

echo "==> Build OCI images"
docker build -f deploy/docker/api.Dockerfile -t flex-agent-oci-api:local "$ROOT"
docker build -f deploy/docker/worker.Dockerfile -t flex-agent-oci-worker:local "$ROOT"
docker build -f deploy/docker/spa.Dockerfile -t flex-agent-oci-spa:local "$ROOT"
