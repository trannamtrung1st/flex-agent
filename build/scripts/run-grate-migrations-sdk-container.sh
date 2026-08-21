#!/usr/bin/env bash
set -euo pipefail

# Runs Grate from the pinned SDK container. grate 2.1.6 asks for
# Microsoft.NETCore.App 10.0.10; the pinned SDK image ships 10.0.0, so the
# tool is executed against that framework instead of failing closed on a
# missing patch.

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

if [[ -z "${FLEXAGENT_DATABASE_URL:-}" ]]; then
  echo "FLEXAGENT_DATABASE_URL is required" >&2
  exit 1
fi

export DOTNET_CLI_TELEMETRY_OPTOUT="${DOTNET_CLI_TELEMETRY_OPTOUT:-1}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/tmp/nuget-packages}"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp}"

dotnet tool restore >/dev/null

GRATE_DLL="$(find "${NUGET_PACKAGES}/grate" -name grate.dll | head -n 1)"
if [[ -z "${GRATE_DLL}" ]]; then
  echo "grate.dll was not restored" >&2
  exit 1
fi

MIGRATIONS_DIRECTORY="${FLEXAGENT_MIGRATIONS_DIRECTORY:-${ROOT}/database/migrations}"

exec dotnet exec --fx-version 10.0.0 "${GRATE_DLL}" \
  --connectionstring="${FLEXAGENT_DATABASE_URL}" \
  --sqlfilesdirectory="${MIGRATIONS_DIRECTORY}" \
  --databasetype=postgresql \
  --transaction \
  --disabletokenreplacement \
  --noninteractive \
  --verbosity=Information
