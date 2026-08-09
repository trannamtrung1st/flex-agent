#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

corepack enable

echo "==> pnpm frozen install"
pnpm install --frozen-lockfile

echo "==> web boundary check"
node build/scripts/check-web-boundaries.mjs

echo "==> web lint"
pnpm lint

echo "==> web typecheck"
pnpm typecheck

echo "==> web unit tests"
pnpm test

echo "==> web production build"
pnpm build

echo "==> web verification complete"
