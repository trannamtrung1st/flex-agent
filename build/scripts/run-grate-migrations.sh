#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

if [[ -z "${FLEXAGENT_DATABASE_URL:-}" ]]; then
  echo "FLEXAGENT_DATABASE_URL is required" >&2
  exit 1
fi

export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-LatestPatch}"

dotnet tool restore >/dev/null

GRATE_ARGS=(
  --connectionstring="${FLEXAGENT_DATABASE_URL}"
  --sqlfilesdirectory="${ROOT}/database/migrations"
  --databasetype=postgresql
  --transaction
  --disabletokenreplacement
  --noninteractive
  --verbosity=Information
)

if [[ "${1:-}" == "--dryrun" ]]; then
  GRATE_ARGS+=(--dryrun)
fi

dotnet tool run grate "${GRATE_ARGS[@]}"
