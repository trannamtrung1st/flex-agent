#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

if [[ -z "${FLEXAGENT_DATABASE_URL:-}" ]]; then
  echo "FLEXAGENT_DATABASE_URL is required" >&2
  exit 1
fi

export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-LatestPatch}"

MIGRATIONS_DIRECTORY="${FLEXAGENT_MIGRATIONS_DIRECTORY:-${ROOT}/database/migrations}"

RESTORE_LOCK_DIR="${ROOT}/artifacts/.grate-tool-restore.lock.d"
mkdir -p "$(dirname "${RESTORE_LOCK_DIR}")"
restore_lock_attempts=0
until mkdir "${RESTORE_LOCK_DIR}" 2>/dev/null; do
  restore_lock_attempts=$((restore_lock_attempts + 1))
  if [ "${restore_lock_attempts}" -ge 1200 ]; then
    echo "Timed out waiting for grate tool restore lock" >&2
    exit 1
  fi
  sleep 0.1
done
trap 'rmdir "${RESTORE_LOCK_DIR}" 2>/dev/null || true' EXIT
dotnet tool restore >/dev/null

GRATE_ARGS=(
  --connectionstring="${FLEXAGENT_DATABASE_URL}"
  --sqlfilesdirectory="${MIGRATIONS_DIRECTORY}"
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
