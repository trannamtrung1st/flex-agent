#!/usr/bin/env python3
"""Validate Flex Agent documentation."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
FEATURES = DOCS / "requirements" / "features"

P0_SPEC_FILES = [
    "auth-resource-isolation.md",
    "resolved-session-configuration.md",
    "assessment-setup.md",
    "submission-attempts.md",
    "session-text-lifecycle.md",
    "evidence-evaluation.md",
    "review-result-release.md",
]

DEPRECATED_TERMS = [
    "heard-likely",
    "source-of-truth order",
    "cross-campaign",
    "MVP feature-spec catalog",
    "canonical product model",
    "Accepted baseline",
]

LINK_PATTERN = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
ID_PATTERN = re.compile(r"\b(?:REQ|AC)-[A-Z]+-\d+\b")


def iter_markdown_files() -> list[Path]:
    return sorted(DOCS.rglob("*.md"))


def is_external(target: str) -> bool:
    return bool(re.match(r"^[a-zA-Z][a-zA-Z0-9+.-]*:", target)) or target.startswith("#")


def check_links(files: list[Path]) -> list[str]:
    errors: list[str] = []
    for path in files:
        text = path.read_text(encoding="utf-8")
        for match in LINK_PATTERN.finditer(text):
            target = match.group(1).strip()
            if not target or is_external(target):
                continue
            fragment = ""
            if "#" in target:
                target, fragment = target.split("#", 1)
            if not target:
                continue
            resolved = (path.parent / target).resolve()
            if not resolved.exists():
                if resolved.name in P0_SPEC_FILES:
                    continue
                errors.append(f"{path.relative_to(ROOT)}: broken link -> {target}")
    return errors


def check_deprecated_terms(files: list[Path]) -> list[str]:
    errors: list[str] = []
    for path in files:
        text = path.read_text(encoding="utf-8")
        for term in DEPRECATED_TERMS:
            if term.lower() in text.lower():
                errors.append(f"{path.relative_to(ROOT)}: deprecated term '{term}'")
    return errors


def check_duplicate_ids() -> list[str]:
    errors: list[str] = []
    seen: dict[str, str] = {}
    for path in sorted(FEATURES.glob("*.md")):
        if path.name == "README.md":
            continue
        for match in ID_PATTERN.finditer(path.read_text(encoding="utf-8")):
            ident = match.group(0)
            prior = seen.get(ident)
            if prior:
                errors.append(f"duplicate ID {ident} in {path.name} and {prior}")
            else:
                seen[ident] = path.name
    return errors


def check_mermaid(files: list[Path]) -> list[str]:
    errors: list[str] = []
    for path in files:
        parts = path.read_text(encoding="utf-8").split("```")
        if len(parts) % 2 == 0:
            errors.append(f"{path.relative_to(ROOT)}: unbalanced code fences")
    return errors


def check_p0_catalog() -> list[str]:
    errors: list[str] = []
    features_readme = (FEATURES / "README.md").read_text(encoding="utf-8")
    for filename in P0_SPEC_FILES:
        if filename not in features_readme:
            errors.append(f"features/README.md missing catalog entry for {filename}")
    requirements_readme = (DOCS / "requirements" / "README.md").read_text(encoding="utf-8")
    for filename in P0_SPEC_FILES:
        if filename not in requirements_readme:
            errors.append(f"requirements/README.md missing catalog entry for {filename}")
    return errors


def report_p0_status() -> None:
    print("P0 specification file status:")
    for filename in P0_SPEC_FILES:
        path = FEATURES / filename
        state = "present" if path.exists() else "missing"
        print(f"  - {filename}: {state}")


def main() -> int:
    files = iter_markdown_files()
    errors: list[str] = []
    errors.extend(check_links(files))
    errors.extend(check_deprecated_terms(files))
    errors.extend(check_duplicate_ids())
    errors.extend(check_mermaid(files))
    errors.extend(check_p0_catalog())
    report_p0_status()

    if errors:
        print("\nDocumentation validation failed:")
        for error in errors:
            print(f"  - {error}")
        return 1

    print("\nDocumentation validation passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
