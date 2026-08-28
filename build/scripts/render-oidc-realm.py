#!/usr/bin/env python3
"""Render a Keycloak realm import with a runtime-generated client secret."""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path

CANONICAL_BACKCHANNEL = "http://api:8080/auth/backchannel-logout"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--template", required=True)
    parser.add_argument("--secret-file", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--backchannel-url", default=CANONICAL_BACKCHANNEL)
    parser.add_argument("--admin-url", default="http://api:8080")
    args = parser.parse_args()

    secret = Path(args.secret_file).read_text(encoding="utf-8").strip()
    if len(secret) < 16:
        raise SystemExit("generated client secret is missing")

    realm = json.loads(Path(args.template).read_text(encoding="utf-8"))
    client = next(item for item in realm["clients"] if item["clientId"] == "flex-agent-api")
    client["secret"] = secret
    client["adminUrl"] = args.admin_url
    client["rootUrl"] = args.admin_url
    client.setdefault("attributes", {})
    client["attributes"]["backchannel.logout.url"] = args.backchannel_url
    client["attributes"]["backchannel.logout.session.required"] = "false"
    client["attributes"]["pkce.code.challenge.method"] = "S256"
    client["attributes"]["post.logout.redirect.uris"] = "http://localhost:18080/##http://localhost:5274/"

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(realm, indent=2) + "\n", encoding="utf-8")
    os.chmod(output, 0o600)


if __name__ == "__main__":
    main()
