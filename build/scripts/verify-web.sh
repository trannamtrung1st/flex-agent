#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

corepack enable

echo "==> pnpm frozen install"
pnpm install --frozen-lockfile

echo "==> web boundary check"
node build/scripts/check-web-boundaries.mjs

echo "==> frontend isolation check"
node build/scripts/check-frontend-isolation.mjs

echo "==> contracts JCS conformance"
pnpm --filter @flex-agent/contracts test

echo "==> legacy web lint"
pnpm --filter @flex-agent/web-legacy lint

echo "==> candidate web lint"
pnpm --filter @flex-agent/web lint

echo "==> legacy web typecheck"
pnpm --filter @flex-agent/web-legacy typecheck

echo "==> candidate web typecheck"
pnpm --filter @flex-agent/web typecheck

echo "==> legacy web unit tests"
pnpm --filter @flex-agent/web-legacy test

echo "==> candidate web unit tests"
pnpm --filter @flex-agent/web test

echo "==> production web build (web-legacy)"
pnpm --filter @flex-agent/web-legacy build

echo "==> candidate web build"
pnpm --filter @flex-agent/web build

echo "==> design-lab build"
pnpm --filter @flex-agent/web build:design-lab

echo "==> web verification complete"
