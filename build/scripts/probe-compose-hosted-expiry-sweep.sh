#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
# shellcheck source=build/scripts/authenticated-browser-profile.sh
source "${ROOT}/build/scripts/authenticated-browser-profile.sh" status >/dev/null 2>&1 || true

COMPOSE=(docker compose
  -f "${ROOT}/deploy/compose/authenticated-browser.compose.yaml"
  -f "${ROOT}/deploy/compose/authenticated-browser.demo-work.compose.yaml"
  --project-name flex-agent-authenticated-browser)

echo "==> Applying migrations (0069 session_frozen_timing when needed)"
"${COMPOSE[@]}" run --rm --no-deps migrate >/dev/null

echo "==> Rebuilding API and Worker from current source"
"${COMPOSE[@]}" up -d --build --no-deps --force-recreate api worker

echo "==> Restarting gateway so nginx picks up recreated API upstream"
"${COMPOSE[@]}" restart nginx

echo "==> Waiting for API and Worker health"
for _ in $(seq 1 60); do
  if bash "${ROOT}/build/scripts/authenticated-browser-profile.sh" status 2>&1 | grep -q "session-endpoint:ok"; then
    break
  fi
  sleep 2
done

echo "==> Verifying session_frozen_timing exists"
"${COMPOSE[@]}" exec -T postgres psql -U flexagent -d flexagent -c "\dt session_frozen_timing" | grep -q session_frozen_timing

echo "==> Running HostedSessionTimingFairnessTests (Postgres integration)"
dotnet test --project "${ROOT}/tests/Integration/FlexAgent.Postgres.Integration.Tests/FlexAgent.Postgres.Integration.Tests.csproj" -c Release -- \
  --filter-class "FlexAgent.Postgres.Integration.Tests.HostedSessionTimingFairnessTests"

NETWORK="flex-agent-authenticated-browser_default"
PROBE_CONNECTION="Host=postgres;Port=5432;Username=flexagent;Password=flexagent_test_password;Database=flexagent"

SDK_IMAGE="mcr.microsoft.com/dotnet/sdk:10.0.100-noble@sha256:c7445f141c04f1a6b454181bd098dcfa606c61ba0bd213d0a702489e5bd4cd71"

echo "==> Live Compose probe: running Worker loop must expire inserted due Session (test does not call ExpireDueAsync)"
docker run --rm \
  --network "${NETWORK}" \
  -v "${ROOT}:/workspace" -w /workspace \
  -e FLEXAGENT_COMPOSE_PROBE=1 \
  -e FLEXAGENT_COMPOSE_PROBE_CONNECTION="${PROBE_CONNECTION}" \
  "${SDK_IMAGE}" \
  bash -lc "dotnet test --project tests/Integration/FlexAgent.Postgres.Integration.Tests/FlexAgent.Postgres.Integration.Tests.csproj -c Release -- --filter-class FlexAgent.Postgres.Integration.Tests.ComposeStackHostedExpiryProbeTests"

echo "==> Compose hosted expiry sweep probe succeeded"
