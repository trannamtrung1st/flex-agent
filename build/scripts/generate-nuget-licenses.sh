#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUTPUT="${1:-$ROOT/artifacts/supply-chain/nuget-licenses.json}"

mkdir -p "$(dirname "$OUTPUT")"

echo "==> Generate NuGet license inventory from packages.lock.json files"
python3 - "$ROOT" "$OUTPUT" <<'PY'
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

root = Path(sys.argv[1])
output = Path(sys.argv[2])

nuget_packages = Path(
    __import__("os").environ.get("NUGET_PACKAGES", root / ".nuget" / "packages")
)

packages: dict[tuple[str, str], dict] = {}


def local_tag(element: ET.Element) -> str:
    return element.tag.rsplit("}", 1)[-1]


def metadata_from_nuspec(nuspec_path: Path) -> tuple[list[str], list[str]]:
    if not nuspec_path.is_file():
        return [], []
    tree = ET.parse(nuspec_path)
    metadata = next(
        (child for child in tree.getroot() if local_tag(child) == "metadata"),
        None,
    )
    if metadata is None:
        return [], []

    licenses: list[str] = []
    references: list[str] = []

    for child in metadata:
        tag = local_tag(child)
        text = (child.text or "").strip()
        if tag in {"license", "licenseExpression"} and text:
            licenses.append(text)
        elif tag in {"licenseUrl", "projectUrl"} and text:
            references.append(text)
        elif tag == "repository":
            repo_url = (child.attrib.get("url") or "").strip()
            if repo_url:
                references.append(repo_url)

    return sorted(set(licenses)), sorted(set(references))


def nuspec_path(name: str, version: str) -> Path:
    return nuget_packages / name.lower() / version / f"{name.lower()}.nuspec"


def ingest_lock(lock_path: Path) -> None:
    project = lock_path.parent.name
    data = json.loads(lock_path.read_text())
    deps = data.get("dependencies", {}).get("net10.0", {})
    for name, entry in deps.items():
        version = entry.get("resolved")
        if not version:
            continue
        key = (name.lower(), version)
        licenses, references = metadata_from_nuspec(nuspec_path(name, version))
        existing = packages.get(key)
        if existing:
            existing["projects"] = sorted(set(existing["projects"] + [project]))
            existing["licenses"] = sorted(set(existing["licenses"] + licenses))
            existing["licenseReferences"] = sorted(
                set(existing["licenseReferences"] + references)
            )
            continue
        packages[key] = {
            "name": name,
            "version": version,
            "projects": [project],
            "licenses": licenses,
            "licenseReferences": references,
        }


for lock_path in sorted(root.rglob("packages.lock.json")):
    ingest_lock(lock_path)

missing = [
    p
    for p in packages.values()
    if not p["licenses"] and not p["licenseReferences"]
]
if missing:
    names = ", ".join(f"{p['name']}@{p['version']}" for p in missing[:10])
    raise SystemExit(f"NuGet packages missing license metadata: {names}")

payload = {
    "generatedBy": "packages.lock.json+nuspec",
    "packageCount": len(packages),
    "packages": sorted(packages.values(), key=lambda p: (p["name"].lower(), p["version"])),
}
output.write_text(json.dumps(payload, indent=2) + "\n")
print(f"==> NuGet license inventory validated ({len(packages)} packages)")
PY
