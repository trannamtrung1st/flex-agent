#!/usr/bin/env python3
"""Validate Flex Agent documentation under the snapshot-first current catalog."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parent))
DOCS = ROOT / "docs"
FEATURES = DOCS / "requirements" / "features"
WORK = ROOT / ".work"

# Current observable-behavior catalog.
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
}

HISTORICAL_PLACEHOLDER_SPECS: set[str] = set()

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

STALE_AUTHORITY_PATTERNS = [
    "all 19 feature",
    "nineteen feature-spec files",
    "binding until phase 4",
    "still-binding until phase 4",
    "not the phase 4 authority cutover",
    "catalog membership, order, and tier counts are unchanged",
    "adr catalog (binding until phase 4)",
    "technical realization: approved adr",
    "remaining on disk until phase 5",
]

# Live workflow must not recreate historical-record surfaces. Historical
# mentions such as "Historical ADR files are recoverable from Git" are allowed.
PROHIBITED_POLICY_PATTERNS = [
    "record consequential choices as adrs",
    "publish approved architecture, adrs",
    "approved adrs within their area of authority",
    "adrs under docs/architecture",
    "governing specs, adrs",
    "docs/, adrs, specs",
    "explicit decision record",
    "version control and decision records",
    "supersession links",
    "adr with `proposed",
    "new or superseding adr",
    "technical designs, adrs,",
    "reliability, or adrs",
    "technology decisions, or adrs",
    "retain completed task history",
    "keep the completed task file",
    "keep completed files after completion",
    "do not remove completed task files",
    "maintainers may clean them up",
    "maintainers can retain completed",
    "keep its file after completion",
    "newer document or version; retained for history",
]

LIVE_TASK_STATUSES = {"planned", "in-progress", "completed"}
FORBIDDEN_TASK_STATUSES = {"blocked", "cancelled", "superseded"}
TASK_STATUS_PATTERN = re.compile(r"(?m)^status:\s*([A-Za-z0-9_-]+)")

CURRENT_ARCHITECTURE_OWNERS = [
    "mvp-architecture.md",
    "backend-module-architecture.md",
    "session-runtime-contract.md",
    "evaluation-execution-contract.md",
    "review-result-release-contract.md",
    "frontend-architecture.md",
]

HISTORICAL_CATALOG_PATHS = [
    Path("docs/ui-ux/retired-authority.md"),
    Path("docs/architecture/decisions/README.md"),
]

STALE_AUTHORITY_ALLOWLIST: set[Path] = set()

LINK_PATTERN = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
ID_DEFINITION_PATTERN = re.compile(
    r"(?:^|\n)(?:#{1,6}\s+|[-*]\s+)?`((?:REQ|AC)-[A-Z]+-\d+)`\s*(?:—|-)"
)
HEADING_PATTERN = re.compile(r"^(#{1,6})\s+(.+)$")


def iter_markdown_files() -> list[Path]:
    return sorted([ROOT / "README.md", *DOCS.rglob("*.md")])


def iter_governance_markdown_files() -> list[Path]:
    """Docs plus live harness surfaces that instruct agents."""
    candidates: list[Path] = [
        ROOT / "README.md",
        ROOT / "AGENTS.md",
        *DOCS.rglob("*.md"),
        *(WORK.rglob("*.md") if WORK.exists() else []),
        *(ROOT / ".agents" / "skills").rglob("*.md"),
        *(ROOT / ".cursor" / "skills").rglob("*.md"),
        *(ROOT / ".cursor" / "rules").rglob("*.mdc"),
    ]
    return sorted({path.resolve() for path in candidates if path.is_file()})


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


def _stale_allowlisted(path: Path) -> bool:
    resolved = path.resolve()
    for allowed in STALE_AUTHORITY_ALLOWLIST:
        allowed_resolved = allowed.resolve()
        if resolved == allowed_resolved:
            return True
        if allowed_resolved.is_dir() and allowed_resolved in resolved.parents:
            return True
    return False


def check_stale_authority(files: list[Path]) -> list[str]:
    errors: list[str] = []
    for path in files:
        if _stale_allowlisted(path):
            continue
        text = path.read_text(encoding="utf-8").lower()
        for pattern in STALE_AUTHORITY_PATTERNS:
            if pattern in text:
                errors.append(
                    f"{path.relative_to(ROOT)}: stale historical-authority pattern '{pattern}'"
                )
    return errors


def find_prohibited_policy_hits(text: str) -> list[str]:
    lowered = text.lower()
    return [pattern for pattern in PROHIBITED_POLICY_PATTERNS if pattern in lowered]


def check_prohibited_policy(files: list[Path] | None = None) -> list[str]:
    errors: list[str] = []
    for path in files if files is not None else iter_governance_markdown_files():
        if _stale_allowlisted(path):
            continue
        text = path.read_text(encoding="utf-8")
        for pattern in find_prohibited_policy_hits(text):
            errors.append(
                f"{path.relative_to(ROOT)}: prohibited snapshot-first policy '{pattern}'"
            )
    return errors


def check_duplicate_ids() -> list[str]:
    errors: list[str] = []
    seen: dict[str, str] = {}
    for path in sorted(FEATURES.glob("*.md")):
        if path.name == "README.md" or path.name in HISTORICAL_PLACEHOLDER_SPECS:
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
                errors.append(f"{label} missing {filename} in {tier} catalog order")
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
    catalog = set(ALL_SPEC_FILES) | HISTORICAL_PLACEHOLDER_SPECS
    for path in sorted(FEATURES.glob("*.md")):
        if path.name == "README.md":
            continue
        if path.name not in catalog:
            errors.append(f"unlisted specification file {path.relative_to(ROOT)}")
    return errors


def check_ui_current_catalog() -> list[str]:
    errors: list[str] = []
    ui_readme = (DOCS / "ui-ux" / "README.md").read_text(encoding="utf-8")
    if "Approved v1.0" not in ui_readme:
        errors.append(
            "docs/ui-ux/README.md must identify current P0 journeys or the design system as Approved v1.0"
        )
    return errors


def check_current_state_index() -> list[str]:
    errors: list[str] = []
    path = DOCS / "current-state.md"
    if not path.exists():
        errors.append("missing docs/current-state.md")
        return errors
    text = path.read_text(encoding="utf-8").lower()
    if "non-normative" not in text:
        errors.append("docs/current-state.md must declare that it is non-normative")
    return errors


def check_work_hygiene() -> list[str]:
    errors: list[str] = []
    readme = WORK / "README.md"
    if not readme.exists():
        errors.append("missing .work/README.md")
        return errors
    text = readme.read_text(encoding="utf-8").lower()
    if "snapshot-first" not in text:
        errors.append(".work/README.md must describe snapshot-first retention")
    if "not current authority" not in text and "not authoritative" not in text:
        errors.append(".work/README.md must state that task files are not current authority")
    if "git owns history" not in text:
        errors.append(".work/README.md must state that Git owns history")
    if "planned" not in text or "in-progress" not in text:
        errors.append(".work/README.md must keep planned/in-progress tasks in .work/active")
    if "(and `blocked`)" in text or "`planned`, `in-progress`, `blocked`" in text:
        errors.append(
            ".work/README.md must not treat blocked as a live task-file status"
        )
    if "review" not in text or ("delete" not in text and "deleted" not in text):
        errors.append(
            ".work/README.md must delete completed tasks after required review"
        )
    for phrase in (
        "retain completed task history",
        "do not remove completed task files",
        "keep completed files after completion",
    ):
        if phrase in text:
            errors.append(f".work/README.md must not instruct '{phrase}'")
    return errors


def check_no_adr_directory() -> list[str]:
    path = DOCS / "architecture" / "decisions"
    if path.exists():
        return [f"live ADR directory must not exist: {path.relative_to(ROOT)}"]
    return []


def check_live_task_statuses() -> list[str]:
    errors: list[str] = []
    active = WORK / "active"
    if not active.exists():
        return errors
    for path in sorted(active.glob("*.md")):
        text = path.read_text(encoding="utf-8")
        match = TASK_STATUS_PATTERN.search(text)
        if not match:
            errors.append(f"{path.relative_to(ROOT)}: missing status front matter")
            continue
        status = match.group(1).lower()
        if status not in LIVE_TASK_STATUSES:
            errors.append(
                f"{path.relative_to(ROOT)}: status '{status}' is not a live "
                "planned/in-progress/completed task"
            )
    return errors


def check_architecture_current_catalog() -> list[str]:
    errors: list[str] = []
    arch_dir = DOCS / "architecture"
    readme_path = arch_dir / "README.md"
    if not readme_path.exists():
        return ["missing docs/architecture/README.md"]
    readme = readme_path.read_text(encoding="utf-8")
    for name in CURRENT_ARCHITECTURE_OWNERS:
        owner = arch_dir / name
        if not owner.exists():
            errors.append(f"missing current architecture owner {owner.relative_to(ROOT)}")
        if name not in readme:
            errors.append(f"docs/architecture/README.md must catalog {name}")
    if "architecture/decisions" in readme.replace("\\", "/"):
        errors.append("docs/architecture/README.md must not catalog architecture/decisions")
    extraction_markers = (
        "adr-001 through adr-021",
        "historical adr files are recoverable",
        "after adr extraction",
    )
    owner_files = [readme_path, *[arch_dir / name for name in CURRENT_ARCHITECTURE_OWNERS]]
    for owner in owner_files:
        if not owner.exists():
            continue
        owner_text = owner.read_text(encoding="utf-8").lower()
        for marker in extraction_markers:
            if marker in owner_text:
                errors.append(
                    f"{owner.relative_to(ROOT)} must not narrate '{marker}' as live catalog provenance"
                )
    return errors


def check_no_historical_catalog_files() -> list[str]:
    errors: list[str] = []
    for relative in HISTORICAL_CATALOG_PATHS:
        path = ROOT / relative
        if path.exists():
            errors.append(f"historical catalog file must not remain live: {relative}")
    return errors


def report_spec_status() -> None:
    print("Feature specification file status:")
    for tier, filenames in SPEC_CATALOG.items():
        print(f"  {tier}:")
        for filename in filenames:
            path = FEATURES / filename
            state = "present" if path.exists() else "missing"
            print(f"    - {filename}: {state}")
    print("  historical placeholders (not current catalog):")
    for filename in sorted(HISTORICAL_PLACEHOLDER_SPECS):
        path = FEATURES / filename
        state = "present" if path.exists() else "missing"
        print(f"    - {filename}: {state}")


def main() -> int:
    files = iter_markdown_files()
    governance_files = iter_governance_markdown_files()
    anchor_cache = heading_anchor_cache(governance_files)
    errors: list[str] = []
    errors.extend(check_ui_current_catalog())
    errors.extend(check_current_state_index())
    errors.extend(check_work_hygiene())
    errors.extend(check_no_adr_directory())
    errors.extend(check_live_task_statuses())
    errors.extend(check_architecture_current_catalog())
    errors.extend(check_no_historical_catalog_files())
    errors.extend(check_spec_files_exist())
    errors.extend(check_unlisted_feature_specs())
    errors.extend(check_links(governance_files, anchor_cache))
    errors.extend(check_deprecated_terms(files))
    errors.extend(check_stale_authority(governance_files))
    errors.extend(check_prohibited_policy(governance_files))
    errors.extend(check_duplicate_ids())
    errors.extend(check_mermaid(files))
    errors.extend(check_catalog_membership(FEATURES / "README.md", "features/README.md"))
    errors.extend(check_catalog_membership(DOCS / "requirements" / "README.md", "requirements/README.md"))
    errors.extend(check_catalog_order(FEATURES / "README.md", "features/README.md"))
    errors.extend(check_catalog_order(DOCS / "requirements" / "README.md", "requirements/README.md"))
    errors.extend(check_tier_counts(FEATURES / "README.md", "features/README.md"))
    errors.extend(check_tier_counts(DOCS / "requirements" / "README.md", "requirements/README.md"))
    from impeccable_context import check_adapters, check_impeccable_tracked_paths

    errors.extend(check_adapters())
    errors.extend(check_impeccable_tracked_paths())
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
