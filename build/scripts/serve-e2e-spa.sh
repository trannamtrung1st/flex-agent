#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

cd "$ROOT/web"
npm run build

docker rm -f flex-agent-e2e-spa >/dev/null 2>&1 || true
docker rm -f flex-agent-e2e-spa-test >/dev/null 2>&1 || true

exec docker run --rm --name flex-agent-e2e-spa \
  --add-host=host.docker.internal:host-gateway \
  -p 5173:5173 \
  -v "$ROOT/web/dist:/usr/share/nginx/html:ro" \
  -v "$ROOT/deploy/nginx/e2e.conf:/etc/nginx/conf.d/default.conf:ro" \
  nginx:1.30.4-alpine
