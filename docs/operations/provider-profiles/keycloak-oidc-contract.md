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
