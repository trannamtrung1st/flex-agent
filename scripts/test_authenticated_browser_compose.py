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
            "worker": {"build": {}},
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

    missing_worker = copy.deepcopy(valid_config())
    del missing_worker["services"]["worker"]
    expect_fail(missing_worker, "missing services")

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

    check_generated_bind_mount_permissions()

    print("authenticated-browser compose validator negatives ok")


def _mode(path: Path) -> int:
    return path.stat().st_mode & 0o777


def check_generated_bind_mount_permissions() -> None:
    renderer = ROOT / "build" / "scripts" / "render-oidc-realm.py"
    profile = (ROOT / "build" / "scripts" / "authenticated-browser-profile.sh").read_text(encoding="utf-8")
    gitignore = (ROOT / ".gitignore").read_text(encoding="utf-8")
    compose = (ROOT / "deploy" / "compose" / "authenticated-browser.compose.yaml").read_text(encoding="utf-8")
    if 'chmod 700 "${GENERATED_DIR}"' not in profile:
        raise SystemExit(".generated host root must stay mode 0700")
    if 'chmod 755 "${GENERATED_DIR}/secrets"' not in profile:
        raise SystemExit("secrets directory must be traversable by the API container user")
    if 'chmod 644 "${GENERATED_DIR}/secrets/oidc-client-secret"' not in profile:
        raise SystemExit("oidc-client-secret must be container-readable")
    if 'chmod 644 "${GENERATED_DIR}/flex-agent-realm.json"' not in profile:
        raise SystemExit("rendered realm must be container-readable")
    if 'chmod 600 "${GENERATED_DIR}/keycloak.env"' not in profile:
        raise SystemExit("keycloak.env must stay host-private")
    if "(\n      umask 077" not in profile:
        raise SystemExit("keycloak.env umask 077 must be scoped to a subshell")
    if 'chmod 700 "${GENERATED_DIR}" "${GENERATED_DIR}/secrets"' in profile:
        raise SystemExit("secrets directory must not inherit host-only 0700")
    if "chmod 777" in profile:
        raise SystemExit("world-writable generated fixtures are not permitted")
    if "os.chmod(output, 0o644)" not in renderer.read_text(encoding="utf-8"):
        raise SystemExit("realm renderer must chmod the output 0644")
    if "deploy/compose/authenticated-browser/.generated/" not in gitignore:
        raise SystemExit("generated OIDC fixtures must remain gitignored")
    if ".generated/flex-agent-realm.json:/opt/keycloak/data/import/flex-agent-realm.json:ro" not in compose:
        raise SystemExit("realm bind-mount must stay read-only")
    if ".generated/secrets:/run/secrets:ro" not in compose:
        raise SystemExit("secrets bind-mount must stay read-only")
    if ".Config.Env" in profile or "{{json .}}" in profile:
        raise SystemExit("OIDC diagnostics must not dump complete inspect or container env")

    with tempfile.TemporaryDirectory() as tmp:
        generated = Path(tmp) / ".generated"
        secrets = generated / "secrets"
        secrets.mkdir(parents=True)
        secret = secrets / "oidc-client-secret"
        secret.write_text("synthetic-oidc-client-secret", encoding="utf-8")
        realm_out = generated / "flex-agent-realm.json"
        env_file = generated / "keycloak.env"
        env_file.write_text("KC_BOOTSTRAP_ADMIN_USERNAME=admin\nKC_BOOTSTRAP_ADMIN_PASSWORD=placeholder\n", encoding="utf-8")
        subprocess.run(
            [
                sys.executable,
                str(renderer),
                "--template",
                str(REALM),
                "--secret-file",
                str(secret),
                "--output",
                str(realm_out),
            ],
            check=True,
        )
        subprocess.run(
            [
                sys.executable,
                str(renderer),
                "--template",
                str(REALM),
                "--secret-file",
                str(secret),
                "--output",
                str(realm_out),
            ],
            check=True,
        )
        if _mode(realm_out) != 0o644:
            raise SystemExit(f"rendered realm mode is {_mode(realm_out):o}, expected 644")
        generated.chmod(0o700)
        secrets.chmod(0o755)
        secret.chmod(0o644)
        env_file.chmod(0o600)
        if _mode(generated) != 0o700:
            raise SystemExit("generated root must be host-private 0700")
        if _mode(secrets) != 0o755:
            raise SystemExit("secrets directory must be other-executable 0755")
        if _mode(secret) != 0o644:
            raise SystemExit("client secret file must be other-readable 0644")
        if _mode(env_file) != 0o600:
            raise SystemExit("keycloak.env must be host-private 0600")
        if not (realm_out.stat().st_mode & 0o200):
            raise SystemExit("rendered realm must remain owner-writable for re-validate")



if __name__ == "__main__":
    main()
