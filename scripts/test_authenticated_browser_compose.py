#!/usr/bin/env python3
"""Negative controls for authenticated-browser rendered Compose validation."""

from __future__ import annotations

import copy
import json
import os
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VALIDATOR = ROOT / "build" / "scripts" / "validate-authenticated-browser-compose.py"
NGINX = ROOT / "deploy" / "compose" / "nginx" / "authenticated-browser.conf"
REALM = ROOT / "deploy" / "compose" / "keycloak" / "flex-agent-realm.json"

DIGEST = "@sha256:" + ("a" * 64)


def valid_config() -> dict:
    return {
        "services": {
            "postgres": {
                "image": f"postgres:18{DIGEST}",
                "tmpfs": ["/var/lib/postgresql"],
            },
            "keycloak-db": {
                "image": f"postgres:18{DIGEST}",
                "tmpfs": ["/var/lib/postgresql"],
            },
            "keycloak": {
                "image": f"quay.io/keycloak/keycloak:26.7.0{DIGEST}",
                "healthcheck": {"test": ["CMD-SHELL", "true"]},
            },
            "migrate": {"image": f"mcr.microsoft.com/dotnet/sdk:10.0.100-noble{DIGEST}"},
            "seed": {"image": f"postgres:18{DIGEST}"},
            "seaweedfs": {
                "image": f"chrislusf/seaweedfs:4.29{DIGEST}",
                "tmpfs": ["/data"],
            },
            "api": {
                "build": {},
                "environment": {
                    "HumanAuthentication__RedirectUri": "http://localhost:18080/auth/callback",
                    "HumanAuthentication__Issuer": "http://localhost:18080/realms/flex-agent",
                },
                "volumes": [
                    {
                        "type": "bind",
                        "source": str(ROOT / "deploy/compose/authenticated-browser/.generated/secrets"),
                        "target": "/run/secrets",
                    }
                ],
                "depends_on": {
                    "keycloak": {"condition": "service_healthy"},
                    "seed": {"condition": "service_completed_successfully"},
                },
            },
            "spa": {"build": {}},
            "nginx": {
                "image": f"nginx:1.30.4{DIGEST}",
                "ports": [
                    {"host_ip": "127.0.0.1", "published": "18080", "target": 80},
                ],
                "depends_on": {"api": {"condition": "service_healthy"}},
            },
        }
    }


def run_validator(
    config: dict,
    nginx: Path | None = None,
    realm: Path | None = None,
    generated: bool = False,
    demo_work: bool = False,
) -> subprocess.CompletedProcess[str]:
    with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False) as handle:
        json.dump(config, handle)
        config_path = handle.name
    command = [
        sys.executable,
        str(VALIDATOR),
        "--compose-json",
        config_path,
        "--nginx",
        str(nginx or NGINX),
        "--realm",
        str(realm or REALM),
    ]
    if generated:
        command.append("--generated-realm")
    if demo_work:
        command.append("--demo-work")
    return subprocess.run(command, check=False, capture_output=True, text=True)


def expect_fail(config: dict, needle: str, demo_work: bool = False) -> None:
    result = run_validator(config, demo_work=demo_work)
    if result.returncode == 0:
        raise SystemExit(f"expected validation failure containing {needle!r}")
    if needle not in (result.stderr + result.stdout):
        raise SystemExit(f"failure did not mention {needle!r}: {result.stderr}")


def expect_nginx_fail(needle: str, nginx_text: str) -> None:
    with tempfile.NamedTemporaryFile("w", suffix=".conf", delete=False) as handle:
        handle.write(nginx_text)
        nginx_path = handle.name
    result = run_validator(valid_config(), nginx=Path(nginx_path))
    if result.returncode == 0:
        raise SystemExit(f"expected nginx validation failure containing {needle!r}")
    if needle not in (result.stderr + result.stdout):
        raise SystemExit(f"failure did not mention {needle!r}: {result.stderr}")


def expect_realm_fail(needle: str, realm: dict) -> None:
    with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False) as handle:
        json.dump(realm, handle)
        realm_path = handle.name
    result = run_validator(valid_config(), realm=Path(realm_path))
    if result.returncode == 0:
        raise SystemExit(f"expected realm validation failure containing {needle!r}")
    if needle not in (result.stderr + result.stdout):
        raise SystemExit(f"failure did not mention {needle!r}: {result.stderr}")


def main() -> None:
    ok = run_validator(valid_config())
    if ok.returncode != 0:
        raise SystemExit(ok.stderr)

    demo_work = copy.deepcopy(valid_config())
    demo_work["services"]["seed-demo-work"] = {
        "image": f"postgres:18{DIGEST}",
        "depends_on": {"seed": {"condition": "service_completed_successfully"}},
    }
    demo_work["services"]["api"]["depends_on"] = {
        "keycloak": {"condition": "service_healthy"},
        "seed-demo-work": {"condition": "service_completed_successfully"},
    }
    ok_demo_work = run_validator(demo_work, demo_work=True)
    if ok_demo_work.returncode != 0:
        raise SystemExit(ok_demo_work.stderr)

    missing_demo_work_seed = copy.deepcopy(demo_work)
    del missing_demo_work_seed["services"]["seed-demo-work"]
    expect_fail(missing_demo_work_seed, "missing services", demo_work=True)

    floating = copy.deepcopy(valid_config())
    floating["services"]["keycloak"]["image"] = "quay.io/keycloak/keycloak:26.7.0"
    expect_fail(floating, "digest-pinned")

    published_db = copy.deepcopy(valid_config())
    published_db["services"]["postgres"]["ports"] = [
        {"host_ip": "127.0.0.1", "published": "5432", "target": 5432},
    ]
    expect_fail(published_db, "database host publication")

    non_loopback = copy.deepcopy(valid_config())
    non_loopback["services"]["nginx"]["ports"][0]["host_ip"] = "0.0.0.0"
    expect_fail(non_loopback, "non-loopback gateway")

    wrong_callback = copy.deepcopy(valid_config())
    wrong_callback["services"]["api"]["environment"]["HumanAuthentication__RedirectUri"] = (
        "http://example.test/auth/callback"
    )
    expect_fail(wrong_callback, "OIDC callback")

    missing_health = copy.deepcopy(valid_config())
    del missing_health["services"]["keycloak"]["healthcheck"]
    expect_fail(missing_health, "healthcheck")

    tracked_secret = copy.deepcopy(valid_config())
    tracked_secret["services"]["api"]["volumes"] = [
        {
            "type": "bind",
            "source": str(ROOT / "deploy/compose/authenticated-browser/secrets"),
            "target": "/run/secrets",
        }
    ]
    expect_fail(tracked_secret, ".generated/secrets")

    browser_route = copy.deepcopy(valid_config())
    browser_route["services"]["api"]["environment"]["Synthetic__Path"] = "/browser/debug"
    expect_fail(browser_route, "synthetic browser route")

    nginx = NGINX.read_text(encoding="utf-8")
    poisoned_nginx = nginx + "\n    location /legacy-master {\n        proxy_pass http://keycloak:8080/realms/master;\n    }\n"
    expect_nginx_fail("master realm must not be proxied", poisoned_nginx)

    realm = json.loads(REALM.read_text(encoding="utf-8"))
    api_client = next(item for item in realm["clients"] if item["clientId"] == "flex-agent-api")
    api_client["secret"] = "tracked-bearer-capable-secret"
    expect_realm_fail("bearer-capable client secret", realm)

    env = os.environ.copy()
    env["FLEXAGENT_OIDC_SIMULATE_MISSING_DOCKER"] = "1"
    missing_docker = subprocess.run(
        ["bash", str(ROOT / "build/scripts/verify-oidc.sh")],
        check=False,
        capture_output=True,
        text=True,
        env=env,
    )
    if missing_docker.returncode == 0:
        raise SystemExit("verify:oidc must fail when Docker is missing from PATH")
    combined = missing_docker.stderr + missing_docker.stdout
    if "requires Docker Compose" not in combined:
        raise SystemExit(f"missing-Docker failure did not name the reason: {combined}")
    if "client_secret=" in combined.lower() or "access_token=" in combined.lower():
        raise SystemExit("missing-Docker failure leaked a token-shaped value")

    print("authenticated-browser compose validator negatives ok")


if __name__ == "__main__":
    main()
