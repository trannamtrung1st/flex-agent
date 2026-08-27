#!/usr/bin/env python3
"""Assert a Playwright JSON report contains the required OIDC case IDs without skips."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def walk(node: object, found: dict[str, str]) -> None:
    if isinstance(node, dict):
        title = str(node.get("title") or "")
        specs = node.get("specs")
        tests = node.get("tests")
        if isinstance(specs, list):
            for spec in specs:
                walk(spec, found)
        if isinstance(tests, list):
            for test in tests:
                status = str(test.get("status") or "")
                results = test.get("results") or []
                result_status = ""
                if results and isinstance(results, list) and isinstance(results[0], dict):
                    result_status = str(results[0].get("status") or "")
                combined = f"{status} {result_status}"
                if title:
                    found[title] = combined
        for value in node.values():
            walk(value, found)
    elif isinstance(node, list):
        for item in node:
            walk(item, found)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--report", required=True)
    parser.add_argument("--require", nargs="+", required=True)
    args = parser.parse_args()
    report = json.loads(Path(args.report).read_text(encoding="utf-8"))
    found: dict[str, str] = {}
    walk(report, found)
    missing = []
    skipped = []
    for case_id in args.require:
        matches = [(title, status) for title, status in found.items() if case_id in title]
        if not matches:
            missing.append(case_id)
            continue
        if any("skipped" in status.lower() or "unexpected" in status.lower() for _, status in matches):
            skipped.append(case_id)
        if not any("expected" in status.lower() or "passed" in status.lower() or status.strip() == "" for _, status in matches):
            if case_id not in skipped:
                skipped.append(case_id)
    if missing or skipped:
        print(f"missing={missing} skipped_or_failed={skipped}", file=sys.stderr)
        raise SystemExit(1)
    print("oidc case manifest ok")


if __name__ == "__main__":
    main()
