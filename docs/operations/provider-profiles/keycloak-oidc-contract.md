# Keycloak OIDC contract profile

## Status and authority

| Field | Value |
| --- | --- |
| **Status** | Implementation contract |
| **Owner** | Architecture Lead |
| **Governs** | Local/CI Keycloak `26.7.0` human-identity qualification |
| **Related decisions** | ADR-008, ADR-010 `STACK-DEC-10`, `STACK-DEC-19`–`STACK-DEC-25` |

This profile is a pinned local/CI identity fixture. It does not authorize
Production, Staging, or real Participant data.

## Pin

- Keycloak image: `quay.io/keycloak/keycloak:26.7.0`
- PostgreSQL: `postgres:18`
- NGINX: `nginx:1.30.4`
- Realm: `flex-agent`
- Client: `flex-agent-api`
- Accepted MFA evidence: `acr=acr:mfa` and/or `amr` containing `mfa`

## Compose

```bash
docker compose -f deploy/compose/keycloak-contract.compose.yaml up -d
```

Synthetic operator credentials live only in the disposable compose fixture.
Do not copy them into production secret stores or browser artifacts.

## Qualifying matrix

Exercise PKCE login, logout, revocation, key rotation, clock skew, account
disablement, back-channel logout, provider outage, and multi-instance callback
against synthetic users only. New-login failures and existing-session
revocation must be reported separately.

The imported `flex-agent-api` client sets `backchannel.logout.url` and
`adminUrl` to `http://host.docker.internal:18082/auth/backchannel-logout` so
Keycloak can propagate logout to the host API. The fixture also enables
direct-access grants only so CI can create a synthetic provider session and
drive Keycloak logout without a browser. Do not copy that grant into
Production.

## Logout-token scope

Validated back-channel Logout Tokens follow the OpenID Connect Back-Channel
Logout contract:

- `sid` only: tombstone and revoke that provider session.
- `sub` only: write an `(issuer, subject)` logout watermark and revoke all
  matching application sessions. A later login may succeed only with a newer
  ID-token `iat`.
- `sid` and `sub`: revoke the intersecting provider session only. Do not
  watermark the identity or revoke sibling sessions for the same subject.

Live Keycloak/NGINX execution of this matrix remains a Docker-backed
qualification gate. The application-session code path for these rules is
independently reviewed; do not claim the full live matrix from unit tests
alone.
