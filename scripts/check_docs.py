#!/usr/bin/env python3
"""Validate Flex Agent documentation."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parent))
DOCS = ROOT / "docs"
FEATURES = DOCS / "requirements" / "features"

SPEC_CATALOG: dict[str, list[str]] = {
    "P0": [
        "auth-resource-isolation.md",
        "resolved-session-configuration.md",
        "assessment-setup.md",
        "submission-attempts.md",
        "session-text-lifecycle.md",
        "evidence-evaluation.md",
        "review-result-release.md",
    ],
    "P1": [
        "agent-library-configuration.md",
        "harness-library-configuration.md",
    ],
    "P2": [
        "voice-interaction-interruption.md",
        "tool-execution-permissions.md",
        "workflow-stage-configuration.md",
        "harness-snapshots-comparison-restoration.md",
        "memory-governance-dynamic-mode.md",
    ],
    "P3": [
        "memory-candidates-learning-approval.md",
        "harness-improvement-proposals.md",
        "shared-multi-participant-sessions.md",
        "calibration-analytics.md",
        "activity-deployment-forms.md",
    ],
}

ALL_SPEC_FILES = [filename for tier in SPEC_CATALOG.values() for filename in tier]
EXPECTED_TIER_COUNTS = {tier: len(files) for tier, files in SPEC_CATALOG.items()}

DEPRECATED_TERMS = [
    "heard-likely",
    "source-of-truth order",
    "cross-campaign",
    "MVP feature-spec catalog",
    "canonical product model",
    "Accepted baseline",
]

LINK_PATTERN = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
# Definitions appear as `REQ-…` / `AC-…` bullets or AC headings, not later references.
ID_DEFINITION_PATTERN = re.compile(
    r"(?:^|\n)(?:#{1,6}\s+|[-*]\s+)?`((?:REQ|AC)-[A-Z]+-\d+)`\s*(?:—|-)"
)
HEADING_PATTERN = re.compile(r"^(#{1,6})\s+(.+)$")


def iter_markdown_files() -> list[Path]:
    return sorted([ROOT / "README.md", *DOCS.rglob("*.md")])


def is_external_scheme(target: str) -> bool:
    return bool(re.match(r"^[a-zA-Z][a-zA-Z0-9+.-]*:", target))


def github_heading_anchor(heading_text: str) -> str:
    text = heading_text.strip()
    text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", text)
    text = re.sub(r"<[^>]+>", "", text)
    text = re.sub(r"[*_`]", "", text)
    text = text.strip().lower()
    text = re.sub(r"[^\w\u00c0-\u024f]+", "-", text, flags=re.UNICODE)
    return text.strip("-")


def collect_heading_anchors(text: str) -> set[str]:
    anchors: set[str] = set()
    seen: dict[str, int] = {}
    for line in text.splitlines():
        match = HEADING_PATTERN.match(line)
        if not match:
            continue
        base = github_heading_anchor(match.group(2))
        if not base:
            continue
        if base not in seen:
            seen[base] = 0
            anchors.add(base)
        else:
            seen[base] += 1
            anchors.add(f"{base}-{seen[base]}")
    return anchors


def heading_anchor_cache(files: list[Path]) -> dict[Path, set[str]]:
    cache: dict[Path, set[str]] = {}
    for path in files:
        cache[path.resolve()] = collect_heading_anchors(path.read_text(encoding="utf-8"))
    return cache


def resolve_link_target(source: Path, target: str) -> tuple[Path | None, str]:
    fragment = ""
    if "#" in target:
        target, fragment = target.split("#", 1)
        fragment = fragment.strip()
    target = target.strip()
    if not target:
        return source.resolve(), fragment
    return (source.parent / target).resolve(), fragment


def check_links(files: list[Path], anchor_cache: dict[Path, set[str]]) -> list[str]:
    errors: list[str] = []
    for path in files:
        text = path.read_text(encoding="utf-8")
        for match in LINK_PATTERN.finditer(text):
            raw_target = match.group(1).strip()
            if not raw_target or is_external_scheme(raw_target):
                continue

            resolved, fragment = resolve_link_target(path, raw_target)
            if not resolved.exists():
                errors.append(f"{path.relative_to(ROOT)}: broken link -> {raw_target}")
                continue

            if fragment:
                anchors = anchor_cache.get(resolved.resolve(), set())
                if fragment not in anchors:
                    errors.append(
                        f"{path.relative_to(ROOT)}: broken fragment #{fragment} in {resolved.relative_to(ROOT)}"
                    )
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
        text = path.read_text(encoding="utf-8")
        for match in ID_DEFINITION_PATTERN.finditer(text):
            ident = match.group(1)
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


def check_catalog_membership(readme_path: Path, label: str) -> list[str]:
    errors: list[str] = []
    text = readme_path.read_text(encoding="utf-8")
    for filename in ALL_SPEC_FILES:
        if filename not in text:
            errors.append(f"{label} missing catalog entry for {filename}")
    return errors


def check_catalog_order(readme_path: Path, label: str) -> list[str]:
    errors: list[str] = []
    text = readme_path.read_text(encoding="utf-8")
    for tier, filenames in SPEC_CATALOG.items():
        section_match = re.search(rf"^##+ {re.escape(tier)}[^\n]*$", text, re.MULTILINE)
        if not section_match:
            errors.append(f"{label} missing section heading for {tier}")
            continue
        section_start = section_match.end()
        next_section = re.search(r"^##+ ", text[section_start:], re.MULTILINE)
        section_end = section_start + next_section.start() if next_section else len(text)
        section_text = text[section_start:section_end]
        positions = [(section_text.find(filename), filename) for filename in filenames]
        for _, filename in positions:
            if section_text.find(filename) == -1:
                errors.append(f"{label} missing {filename} in {tier} section")
        ordered = [(pos, filename) for pos, filename in positions if pos != -1]
        for index in range(len(ordered) - 1):
            current_pos, current_name = ordered[index]
            next_pos, next_name = ordered[index + 1]
            if current_pos >= next_pos:
                errors.append(
                    f"{label}: {next_name} appears before {current_name} in {tier} catalog order"
                )
    return errors


def check_tier_counts(readme_path: Path, label: str) -> list[str]:
    errors: list[str] = []
    text = readme_path.read_text(encoding="utf-8")
    for tier, expected in EXPECTED_TIER_COUNTS.items():
        pattern = re.compile(rf"\|\s*(?:\*\*)?{re.escape(tier)}(?:\*\*)?[^\|]*\|\s*(\d+)\s*\|")
        match = pattern.search(text)
        if not match:
            errors.append(f"{label} missing tier count row for {tier}")
            continue
        actual = int(match.group(1))
        if actual != expected:
            errors.append(f"{label} lists {tier} count as {actual}; expected {expected}")
    return errors


def check_spec_files_exist() -> list[str]:
    errors: list[str] = []
    for filename in ALL_SPEC_FILES:
        path = FEATURES / filename
        if not path.exists():
            errors.append(f"missing specification file {path.relative_to(ROOT)}")
    return errors


def check_unlisted_feature_specs() -> list[str]:
    errors: list[str] = []
    catalog = set(ALL_SPEC_FILES)
    for path in sorted(FEATURES.glob("*.md")):
        if path.name == "README.md":
            continue
        if path.name not in catalog:
            errors.append(f"unlisted specification file {path.relative_to(ROOT)}")
    return errors


def report_spec_status() -> None:
    print("Feature specification file status:")
    for tier, filenames in SPEC_CATALOG.items():
        print(f"  {tier}:")
        for filename in filenames:
            path = FEATURES / filename
            state = "present" if path.exists() else "missing"
            print(f"    - {filename}: {state}")


def main() -> int:
    files = iter_markdown_files()
    anchor_cache = heading_anchor_cache(files)
    errors: list[str] = []
    errors.extend(check_spec_files_exist())
    errors.extend(check_unlisted_feature_specs())
    errors.extend(check_links(files, anchor_cache))
    errors.extend(check_deprecated_terms(files))
    errors.extend(check_duplicate_ids())
    errors.extend(check_mermaid(files))
    errors.extend(check_catalog_membership(FEATURES / "README.md", "features/README.md"))
    errors.extend(check_catalog_membership(DOCS / "requirements" / "README.md", "requirements/README.md"))
    errors.extend(check_catalog_order(FEATURES / "README.md", "features/README.md"))
    errors.extend(check_catalog_order(DOCS / "requirements" / "README.md", "requirements/README.md"))
    errors.extend(check_tier_counts(FEATURES / "README.md", "features/README.md"))
    errors.extend(check_tier_counts(DOCS / "requirements" / "README.md", "requirements/README.md"))
    from impeccable_context import check_adapters, check_impeccable_tracked_secrets

    errors.extend(check_adapters())
    errors.extend(check_impeccable_tracked_secrets())
    report_spec_status()

    if errors:
        print("\nDocumentation validation failed:")
        for error in errors:
            print(f"  - {error}")
        return 1

    print("\nDocumentation validation passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
