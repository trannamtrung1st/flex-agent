#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

echo "==> .NET locked restore"
dotnet restore FlexAgent.slnx --locked-mode

echo "==> .NET build"
dotnet build FlexAgent.slnx --no-restore -c Release

echo "==> .NET tests"
dotnet test --solution FlexAgent.slnx --no-build -c Release

echo "==> .NET publish"
dotnet publish src/Hosts/FlexAgent.Api/FlexAgent.Api.csproj -c Release -o artifacts/publish/api --no-restore /p:UseAppHost=false
dotnet publish src/Hosts/FlexAgent.Worker/FlexAgent.Worker.csproj -c Release -o artifacts/publish/worker --no-restore /p:UseAppHost=false

test ! -f artifacts/publish/api/appsettings.Development.json
test ! -f artifacts/publish/worker/appsettings.Development.json

echo "==> .NET verification complete"
