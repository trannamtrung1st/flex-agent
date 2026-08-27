"""Deterministic Impeccable PRODUCT.md / DESIGN.md context adapters."""

from __future__ import annotations

import argparse
import hashlib
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GENERATOR_ID = "flex-agent-impeccable-context/1"

PRODUCT_SOURCES = (
    ROOT / "docs" / "product" / "overview.md",
    ROOT / "docs" / "product" / "concept-model.md",
    ROOT / "docs" / "product" / "mvp-scope.md",
    ROOT / "docs" / "README.md",
)
DESIGN_SOURCES = (
    ROOT / "docs" / "ui-ux" / "design-system" / "README.md",
    ROOT / "docs" / "ui-ux" / "design-system" / "implementation-guide.md",
    ROOT / "docs" / "ui-ux" / "README.md",
)

METADATA_FIELD = re.compile(r"\|\s+\*\*(.+?)\*\*\s+\|\s+(.+?)\s+\|")
SECRET_PATTERN = re.compile(
    r"(?i)(-----BEGIN (?:RSA |OPENSSH |EC )?PRIVATE KEY-----|api[_-]?key\s*[:=]\s*['\"]?[A-Za-z0-9_\-]{16})"
)
PARTICIPANT_EMAIL_PATTERN = re.compile(
    r"(?i)\b[A-Z0-9._%+\-]+@(?!(?:example\.com|flex-agent\.test)\b)[A-Z0-9.\-]+\.[A-Z]{2,}\b"
)
SNAPSHOT_IMPECCABLE = ROOT / ".work" / "resources" / "impeccable-prototype-snapshot"
RUNTIME_DIR_PREFIXES = (
    "shots/",
    "live/",
    "questions/",
    "hooks/",
    "cache/",
    "logs/",
    "critiques/",
    "previews/",
    "annotations/",
    "manual-edits/",
)
RUNTIME_FILENAMES = frozenset(
    {
        "config.local.json",
        "hook.cache.json",
        "hook.pending.json",
        ".impeccable-live.json",
    }
)
RUNTIME_SUFFIXES = (".png", ".webp", ".jpg", ".jpeg", ".gif", ".webm", ".mp4")
ALLOWED_SYNTHETIC_EMAIL_NOTE = "example.com or flex-agent.test"


def metadata(path: Path) -> dict[str, str]:
    text = path.read_text(encoding="utf-8")
    fields: dict[str, str] = {}
    for match in METADATA_FIELD.finditer(text):
        fields[match.group(1).strip()] = match.group(2).strip()
    return fields


def first_prose_paragraph(path: Path) -> str:
    lines = path.read_text(encoding="utf-8").splitlines()
    past_table = False
    buf: list[str] = []
    for line in lines:
        if line.startswith("|"):
            past_table = True
            continue
        if line.startswith("## "):
            if buf:
                break
            continue
        if past_table and line.strip() and not line.startswith("#"):
            buf.append(line.strip())
            if len(buf) >= 6:
                break
    return complete_sentences(" ".join(buf))


def complete_sentences(text: str) -> str:
    text = text.strip()
    if not text or text[-1] in ".!?":
        return text
    last = max(text.rfind(". "), text.rfind("? "), text.rfind("! "))
    if last >= 0:
        return text[: last + 1]
    return text


def fingerprint(paths: tuple[Path, ...]) -> str:
    digest = hashlib.sha256()
    digest.update(GENERATOR_ID.encode("utf-8"))
    for path in paths:
        digest.update(path.as_posix().encode("utf-8"))
        digest.update(path.read_bytes())
    return digest.hexdigest()


def render_product() -> str:
    versions = []
    for path in PRODUCT_SOURCES[:3]:
        meta = metadata(path)
        versions.append(
            f"- `{path.relative_to(ROOT)}` — {meta.get('Status', 'unknown')} (version {meta.get('Version', 'unknown')})"
        )
    fp = fingerprint(PRODUCT_SOURCES)
    overview = first_prose_paragraph(PRODUCT_SOURCES[0])
    return "\n".join(
        [
            "<!-- impeccable-context: product -->",
            "# Flex Agent (Impeccable product context)",
            "",
            "> **Not authoritative.** This file is a generated adapter for Impeccable.",
            "> Product meaning, scope, actors, and invariants live in approved documents under `docs/product/`.",
            "> If this file disagrees with those documents, the documents win.",
            "",
            f"Generator: `{GENERATOR_ID}`",
            f"Content fingerprint: `{fp}`",
            "",
            "## Canonical sources",
            "",
            *versions,
            "- `docs/README.md` — authority by concern",
            "",
            "## Projection",
            "",
            overview,
            "",
            "Keep Organization, Agent, Harness, Activity, Session, Evaluation, human revision,",
            "review decision, Result, and Release as distinct objects. Isolation, frozen session",
            "configuration, audit history, and Result/Release separation are non-negotiable.",
            "",
        ]
    )


def render_design() -> str:
    ds = metadata(DESIGN_SOURCES[0])
    fp = fingerprint(DESIGN_SOURCES)
    direction = first_prose_paragraph(DESIGN_SOURCES[0])
    return "\n".join(
        [
            "<!-- impeccable-context: design -->",
            "# Flex Agent (Impeccable design context)",
            "",
            "> **Not authoritative.** This file is a generated adapter for Impeccable.",
            "> Shared visual language lives in `docs/ui-ux/design-system/`. Feature journeys live in",
            "> approved UI/UX specifications. Approved product and feature specs still govern behavior.",
            "",
            f"Generator: `{GENERATOR_ID}`",
            f"Content fingerprint: `{fp}`",
            "",
            "## Canonical sources",
            "",
            f"- `docs/ui-ux/design-system/README.md` — {ds.get('Status', 'unknown')} (version {ds.get('Version', 'unknown')})",
            "- `docs/ui-ux/design-system/implementation-guide.md`",
            "- `docs/ui-ux/README.md`",
            "",
            "## Projection",
            "",
            direction,
            "",
            "Until design-system v1.0 is approved in place, the table above remains the canonical",
            "approved design-system status. This migration's visual identity is Shipboard Terminal",
            "from the recorded prototypes; Phase 3 promotes that identity into v1.0. Do not treat",
            "v0.1 Deep-Space styling as the target look for work under that task.",
            "",
            "Token projection is deferred until design-system v1.0. Do not copy v0.1 palette, type,",
            "spacing, or component values into this adapter; Impeccable must follow approved docs",
            "and the recorded prototype snapshot rather than treating this file as a token sheet.",
            "",
            "Accessibility, semantic HTML, keyboard operation, and non-color state communication",
            "remain repository requirements even when visual identity is Shipboard Terminal.",
            "",
        ]
    )


def write_adapters() -> None:
    (ROOT / "PRODUCT.md").write_text(render_product(), encoding="utf-8")
    (ROOT / "DESIGN.md").write_text(render_design(), encoding="utf-8")


def check_adapters() -> list[str]:
    errors: list[str] = []
    expected = {
        ROOT / "PRODUCT.md": render_product(),
        ROOT / "DESIGN.md": render_design(),
    }
    for path, body in expected.items():
        if not path.exists():
            errors.append(f"{path.name} is missing; run python3 scripts/impeccable_context.py")
            continue
        actual = path.read_text(encoding="utf-8")
        if actual != body:
            errors.append(f"{path.name} drifted from canonical docs; regenerate or restore the adapter")
    return errors


def relative_impeccable_parts(path: Path) -> str | None:
    parts = path.as_posix().split("/.impeccable/")
    if len(parts) < 2:
        name = path.name
        if path.parent.name == ".impeccable":
            return name
        return None
    return parts[-1]


def _is_snapshot_impeccable(path: Path) -> bool:
    try:
        path.resolve().relative_to(SNAPSHOT_IMPECCABLE.resolve())
        return True
    except (OSError, ValueError):
        return ".work/resources/impeccable-prototype-snapshot/" in path.as_posix()


def _is_runtime_artifact(relative: str) -> bool:
    name = relative.rsplit("/", 1)[-1]
    if name in RUNTIME_FILENAMES:
        return True
    if any(relative == prefix[:-1] or relative.startswith(prefix) for prefix in RUNTIME_DIR_PREFIXES):
        return True
    return relative.lower().endswith(RUNTIME_SUFFIXES)


def git_tracked_impeccable_paths() -> list[Path]:
    import subprocess

    completed = subprocess.run(
        ["git", "ls-files", "-z"],
        cwd=ROOT,
        check=False,
        capture_output=True,
    )
    if completed.returncode != 0:
        return []
    paths: list[Path] = []
    for raw in completed.stdout.split(b"\0"):
        if not raw:
            continue
        rel = raw.decode("utf-8", errors="replace")
        if "/.impeccable/" not in f"/{rel}" and not rel.startswith(".impeccable/"):
            continue
        paths.append(ROOT / rel)
    return paths


def check_impeccable_tracked_paths(tracked_paths: list[Path] | None = None) -> list[str]:
    errors: list[str] = []
    if tracked_paths is None:
        tracked_paths = git_tracked_impeccable_paths()
        if not tracked_paths:
            root_impeccable = ROOT / ".impeccable"
            if root_impeccable.exists():
                tracked_paths = [p for p in root_impeccable.rglob("*") if p.is_file()]
    for path in tracked_paths:
        if _is_snapshot_impeccable(path):
            continue
        relative = relative_impeccable_parts(path)
        try:
            label = path.relative_to(ROOT)
        except ValueError:
            label = path
        if relative and _is_runtime_artifact(relative):
            errors.append(f"refusing runtime artifact in tracked Impeccable path {label}")
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except (UnicodeDecodeError, OSError, IsADirectoryError):
            continue
        if SECRET_PATTERN.search(text):
            errors.append(f"refusing secret-like content in {label}")
        if PARTICIPANT_EMAIL_PATTERN.search(text):
            errors.append(
                f"refusing participant-like email content in {label} "
                f"(allow only {ALLOWED_SYNTHETIC_EMAIL_NOTE})"
            )
    return errors


def check_impeccable_tracked_secrets(tracked_paths: list[Path] | None = None) -> list[str]:
    return check_impeccable_tracked_paths(tracked_paths)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=("generate", "check"), default="check", nargs="?")
    args = parser.parse_args(argv)
    if args.mode == "generate":
        write_adapters()
        print("Wrote PRODUCT.md and DESIGN.md")
        return 0
    errors = check_adapters() + check_impeccable_tracked_paths()
    if errors:
        print("Impeccable context validation failed:")
        for error in errors:
            print(f"  - {error}")
        return 1
    print("Impeccable context adapters are current.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
