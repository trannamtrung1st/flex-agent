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

echo "==> design-lab import specifier regression"
node --test build/scripts/frontend-isolation-lib.test.mjs

echo "==> production web lint"
pnpm --filter @flex-agent/web lint

echo "==> design-lab lint"
pnpm --filter @flex-agent/web lint:design-lab

echo "==> production web typecheck"
pnpm --filter @flex-agent/web typecheck

echo "==> design-lab typecheck"
pnpm --filter @flex-agent/web typecheck:design-lab

echo "==> production web unit tests"
pnpm --filter @flex-agent/web test

echo "==> design-lab unit tests"
pnpm --filter @flex-agent/web test:design-lab

echo "==> production web build"
pnpm --filter @flex-agent/web build

echo "==> candidate production bundle isolation"
node build/scripts/check-candidate-bundle.mjs

echo "==> design-lab build"
pnpm --filter @flex-agent/web build:design-lab

echo "==> design-lab Playwright"
pnpm --filter @flex-agent/web exec playwright install chromium
pnpm --filter @flex-agent/web test:e2e:design-lab

echo "==> web verification complete"
