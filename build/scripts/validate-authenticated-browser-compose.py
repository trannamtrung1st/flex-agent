#!/usr/bin/env python3
"""Validate rendered authenticated-browser Compose semantics (not source greps)."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any

DIGEST = re.compile(r"@sha256:[a-fA-F0-9]{64}$")
REQUIRED_SERVICES = {
    "postgres",
    "keycloak-db",
    "keycloak",
    "migrate",
    "seed",
    "seaweedfs",
    "api",
    "spa",
    "nginx",
}
IMAGE_SERVICES = {
    "postgres",
    "keycloak-db",
    "keycloak",
    "migrate",
    "seed",
    "seaweedfs",
    "nginx",
}
CANONICAL_CALLBACK = "http://localhost:18080/auth/callback"
CANDIDATE_CALLBACK = "http://127.0.0.1:5274/auth/callback"
CANONICAL_BACKCHANNEL = "http://api:8080/auth/backchannel-logout"


def fail(message: str) -> None:
    print(message, file=sys.stderr)
    raise SystemExit(1)


def load_json(path: str | None) -> dict[str, Any]:
    if path in (None, "-"):
        return json.load(sys.stdin)
    return json.loads(Path(path).read_text(encoding="utf-8"))


def published_ports(service: dict[str, Any]) -> list[dict[str, Any]]:
    ports = service.get("ports") or []
    return [port for port in ports if isinstance(port, dict)]


def image_ref(service: dict[str, Any]) -> str:
    image = service.get("image") or ""
    if isinstance(image, dict):
        return str(image.get("name") or "")
    return str(image)


def env_map(service: dict[str, Any]) -> dict[str, str]:
    env = service.get("environment") or {}
    if isinstance(env, list):
        mapped: dict[str, str] = {}
        for item in env:
            if isinstance(item, str) and "=" in item:
                key, value = item.split("=", 1)
                mapped[key] = value
        return mapped
    return {str(key): str(value) for key, value in env.items()}


def volume_sources(service: dict[str, Any]) -> list[str]:
    sources: list[str] = []
    for volume in service.get("volumes") or []:
        if isinstance(volume, str):
            sources.append(volume.split(":", 1)[0])
        elif isinstance(volume, dict):
            sources.append(str(volume.get("source") or ""))
    return sources


def depends_condition(service: dict[str, Any], name: str) -> str | None:
    depends = service.get("depends_on") or {}
    if isinstance(depends, list):
        return "service_started" if name in depends else None
    item = depends.get(name)
    if item is None:
        return None
    if isinstance(item, dict):
        return str(item.get("condition") or "service_started")
    return "service_started"


def validate_compose(config: dict[str, Any], mode: str) -> None:
    services = config.get("services") or {}
    names = set(services)
    missing = REQUIRED_SERVICES - names
    extra = names - REQUIRED_SERVICES
    if missing:
        fail(f"canonical profile missing services: {sorted(missing)}")
    if extra:
        fail(f"canonical profile has unexpected services: {sorted(extra)}")

    for name in IMAGE_SERVICES:
        ref = image_ref(services[name])
        if not DIGEST.search(ref):
            fail(f"{name} image is not digest-pinned: {ref or '(empty)'}")

    if published_ports(services["postgres"]) or published_ports(services["keycloak-db"]):
        fail("application or Keycloak database host publication is not permitted")

    nginx_ports = published_ports(services["nginx"])
    if len(nginx_ports) != 1:
        fail("gateway must publish exactly one host port")
    port = nginx_ports[0]
    host_ip = str(port.get("host_ip") or "")
    published = str(port.get("published") or "")
    target = str(port.get("target") or "")
    if host_ip != "127.0.0.1" or published != "18080" or target != "80":
        fail(f"non-loopback gateway publication is not permitted: {host_ip or '*'}:{published}:{target}")

    for name, service in services.items():
        if name == "nginx":
            continue
        if published_ports(service):
            fail(f"{name} must not publish host ports")

    api_env = env_map(services["api"])
    expected_callback = CANDIDATE_CALLBACK if mode == "candidate" else CANONICAL_CALLBACK
    if api_env.get("HumanAuthentication__RedirectUri") != expected_callback:
        fail("OIDC callback does not match the selected profile mode")
    if api_env.get("HumanAuthentication__Issuer") != "http://localhost:18080/realms/flex-agent":
        fail("browser-visible issuer must remain the canonical gateway realm")
    if "/browser" in json.dumps(config):
        fail("synthetic browser route is not permitted in this profile")

    secret_sources = volume_sources(services["api"])
    if not any(source.replace("\\", "/").endswith(".generated/secrets") for source in secret_sources):
        fail("API must mount generated synthetic secrets from .generated/secrets")

    keycloak = services["keycloak"]
    if not keycloak.get("healthcheck"):
        fail("Keycloak must declare a Compose healthcheck")
    if depends_condition(services["api"], "keycloak") != "service_healthy":
        fail("API must wait for Keycloak health before starting")
    if depends_condition(services["nginx"], "api") != "service_healthy":
        fail("gateway must wait for API health before starting")


def validate_nginx(nginx_text: str) -> None:
    required = (
        "location / {",
        "proxy_pass http://spa:8080",
        "location /auth/",
        "location /v1/assessment",
        "location /v2/assessment",
        "location /sessions/",
        "proxy_pass http://api:8080",
        "location /realms/flex-agent",
        "proxy_pass http://keycloak:8080/realms/flex-agent",
        "location /admin",
        "location /health",
        "location /metrics",
        "location /realms/master",
        "location /browser",
        "return 404",
    )
    for token in required:
        if token not in nginx_text:
            fail(f"gateway configuration missing {token}")
    if "host.docker.internal" in nginx_text:
        fail("gateway must not expose host.docker.internal")
    if "proxy_pass http://keycloak:8080/realms/master" in nginx_text:
        fail("master realm must not be proxied through the public gateway")


def validate_realm(realm: dict[str, Any], generated: bool) -> None:
    client = next(item for item in realm["clients"] if item["clientId"] == "flex-agent-api")
    redirects = client.get("redirectUris") or []
    if CANONICAL_CALLBACK not in redirects:
        fail("realm is missing the canonical gateway callback")
    if CANDIDATE_CALLBACK not in redirects:
        fail("realm is missing the candidate-dev callback")
    attributes = client.get("attributes") or {}
    if attributes.get("pkce.code.challenge.method") != "S256":
        fail("realm must require S256 PKCE")
    if attributes.get("backchannel.logout.url") != CANONICAL_BACKCHANNEL:
        fail("canonical realm back-channel must target the in-compose API")
    if "host.docker.internal" in json.dumps(client):
        fail("canonical realm must not use host.docker.internal")
    secret = client.get("secret")
    if generated:
        if not isinstance(secret, str) or len(secret) < 16:
            fail("generated realm is missing a runtime client secret")
    elif secret:
        fail("committed realm template must not contain a bearer-capable client secret")
    usernames = {user["username"] for user in realm.get("users") or []}
    for required in (
        "synthetic.administrator",
        "synthetic.participant",
        "synthetic.unbound",
        "synthetic.zeroorg",
        "synthetic.ambiguous",
    ):
        if required not in usernames:
            fail(f"realm is missing synthetic identity {required}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--compose-json")
    parser.add_argument("--nginx")
    parser.add_argument("--realm")
    parser.add_argument("--mode", choices=("canonical", "candidate"), default="canonical")
    parser.add_argument("--generated-realm", action="store_true")
    args = parser.parse_args()

    if args.compose_json:
        validate_compose(load_json(args.compose_json), args.mode)
    if args.nginx:
        validate_nginx(Path(args.nginx).read_text(encoding="utf-8"))
    if args.realm:
        validate_realm(json.loads(Path(args.realm).read_text(encoding="utf-8")), args.generated_realm)
    print("authenticated-browser compose contract ok")


if __name__ == "__main__":
    main()
