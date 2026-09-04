#!/usr/bin/env bash
set -euo pipefail

AUDIT_LEVEL="${1:-high}"
MAX_ATTEMPTS="${MAX_ATTEMPTS:-4}"
INITIAL_DELAY_SECONDS="${INITIAL_DELAY_SECONDS:-15}"

attempt=1
while true; do
  output=""
  if output="$(pnpm audit --audit-level="$AUDIT_LEVEL" 2>&1)"; then
    printf '%s\n' "$output"
    exit 0
  fi

  exit_code=$?
  printf '%s\n' "$output" >&2

  if (( attempt >= MAX_ATTEMPTS )); then
    exit "$exit_code"
  fi

  if ! printf '%s\n' "$output" | grep -qE 'ERR_SOCKET_TIMEOUT|ETIMEDOUT|ECONNRESET|FetchError|socket hang up'; then
    exit "$exit_code"
  fi

  delay=$(( INITIAL_DELAY_SECONDS * attempt ))
  echo "pnpm audit hit a transient registry error (attempt ${attempt}/${MAX_ATTEMPTS}); retrying in ${delay}s..." >&2
  sleep "$delay"
  attempt=$(( attempt + 1 ))
done
