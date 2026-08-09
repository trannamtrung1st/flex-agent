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
toolchain_path = root / "build" / "toolchain.json"
toolchain = json.loads(toolchain_path.read_text()) if toolchain_path.is_file() else {}
reviewed_overrides = toolchain.get("nugetLicenseReview", {})

nuget_packages = Path(
    __import__("os").environ.get("NUGET_PACKAGES", root / ".nuget" / "packages")
)

packages: dict[tuple[str, str], dict] = {}


def local_tag(element: ET.Element) -> str:
    return element.tag.rsplit("}", 1)[-1]


def metadata_from_nuspec(nuspec_path: Path) -> dict:
    empty = {
        "licenses": [],
        "licenseUrls": [],
        "licenseFiles": [],
        "projectUrls": [],
        "repositoryUrls": [],
    }
    if not nuspec_path.is_file():
        return empty
    tree = ET.parse(nuspec_path)
    metadata = next(
        (child for child in tree.getroot() if local_tag(child) == "metadata"),
        None,
    )
    if metadata is None:
        return empty

    licenses: list[str] = []
    license_urls: list[str] = []
    license_files: list[str] = []
    project_urls: list[str] = []
    repository_urls: list[str] = []

    for child in metadata:
        tag = local_tag(child)
        text = (child.text or "").strip()
        if tag in {"license", "licenseExpression"}:
            license_type = (child.attrib.get("type") or "expression").strip()
            if license_type == "file":
                if text:
                    license_files.append(text)
            elif text:
                licenses.append(text)
        elif tag == "licenseUrl" and text:
            license_urls.append(text)
        elif tag == "projectUrl" and text:
            project_urls.append(text)
        elif tag == "repository":
            repo_url = (child.attrib.get("url") or "").strip()
            if repo_url:
                repository_urls.append(repo_url)

    return {
        "licenses": sorted(set(licenses)),
        "licenseUrls": sorted(set(license_urls)),
        "licenseFiles": sorted(set(license_files)),
        "projectUrls": sorted(set(project_urls)),
        "repositoryUrls": sorted(set(repository_urls)),
    }


def apply_reviewed_override(name: str, version: str, record: dict) -> None:
    override = reviewed_overrides.get(f"{name}@{version}")
    if not override:
        return
    record["licenses"] = sorted(set(record["licenses"] + override.get("licenses", [])))
    record["licenseUrls"] = sorted(
        set(record["licenseUrls"] + override.get("licenseUrls", []))
    )
    record["licenseFiles"] = sorted(
        set(record["licenseFiles"] + override.get("licenseFiles", []))
    )
    if override.get("evidence"):
        record.setdefault("reviewedLicenseEvidence", [])
        record["reviewedLicenseEvidence"] = sorted(
            set(record["reviewedLicenseEvidence"] + [override["evidence"]])
        )


def has_license_evidence(record: dict) -> bool:
    return bool(record["licenses"] or record["licenseUrls"] or record["licenseFiles"])


def merge_record(existing: dict, incoming: dict, project: str) -> dict:
    return {
        "name": existing["name"],
        "version": existing["version"],
        "projects": sorted(set(existing["projects"] + [project])),
        "licenses": sorted(set(existing["licenses"] + incoming["licenses"])),
        "licenseUrls": sorted(set(existing["licenseUrls"] + incoming["licenseUrls"])),
        "licenseFiles": sorted(set(existing["licenseFiles"] + incoming["licenseFiles"])),
        "projectUrls": sorted(set(existing["projectUrls"] + incoming["projectUrls"])),
        "repositoryUrls": sorted(
            set(existing["repositoryUrls"] + incoming["repositoryUrls"])
        ),
        "reviewedLicenseEvidence": sorted(
            set(
                existing.get("reviewedLicenseEvidence", [])
                + incoming.get("reviewedLicenseEvidence", [])
            )
        ),
    }


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
        record = metadata_from_nuspec(nuspec_path(name, version))
        record = {
            "name": name,
            "version": version,
            "projects": [project],
            **record,
            "reviewedLicenseEvidence": [],
        }
        apply_reviewed_override(name, version, record)
        existing = packages.get(key)
        if existing:
            packages[key] = merge_record(existing, record, project)
            continue
        packages[key] = record


for lock_path in sorted(root.rglob("packages.lock.json")):
    ingest_lock(lock_path)

missing = [p for p in packages.values() if not has_license_evidence(p)]
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
