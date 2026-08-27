# Impeccable skill provenance

This directory is a reviewed, pinned copy of the official Impeccable skill.
It is a repository capability, not product or design authority.

| Field | Value |
| --- | --- |
| Package version | 4.1.1 |
| Official tag | `skill-v4.1.1` |
| Upstream commit | `5a149f3fdb1b5793f10567233b1dcab98fc305fd` |
| Upstream remote | https://github.com/pbakaus/impeccable |
| License | Apache-2.0 (`LICENSE`, `NOTICE.md` copied from the tagged repository root) |
| Acquisition | `git clone --depth 1 --branch skill-v4.1.1` (no `npx impeccable install/update`) |
| File count from upstream skill tree | 153 |
| Local additions | `LICENSE`, `NOTICE.md`, this file; `agents/openai.yaml` policy patch |

## Local modifications

- `agents/openai.yaml`: set `policy.allow_implicit_invocation: false` so the skill is explicit-invocation only.
- Hooks, live mode, and automatic update/staleness network checks are not enabled. Shared `.impeccable/config.json` sets `stalenessCheck: false` and `updateCheck: false`.

## Experiment copy comparison

The experiment repository at planning time also declared 4.1.1 (153 skill files) but omitted `LICENSE`/`NOTICE.md`. Hash comparison of the official tag against the experiment skill tree found **27 content-different files** (same paths). Flex Agent vendors the **official tag**, not the experiment copy.

## Disabled by default

Do not run hook installers, live mode, `pin.mjs` global shortcuts, or the network bundle installer during this migration. Scripts in `scripts/` may write `.impeccable/` runtime state, spawn subprocesses, or open local HTTP servers when invoked; they are not part of production runtime.
