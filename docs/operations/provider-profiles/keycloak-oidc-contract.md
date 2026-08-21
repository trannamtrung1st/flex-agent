# Keycloak OIDC contract profile

## Status and authority

| Field | Value |
| --- | --- |
| **Status** | Implementation contract |
| **Owner** | Architecture Lead |
| **Governs** | Local/CI Keycloak `26.7.0` human-identity qualification |
| **Related decisions** | ADR-008, ADR-010 `STACK-DEC-10`, `STACK-DEC-19`–`STACK-DEC-27` |

This profile is a pinned local/CI identity fixture. It does not authorize
Production, Staging, or real Participant data.

## Ownership boundary

Keycloak owns synthetic credentials, MFA enrollment and authentication, and
upstream account lifecycle in this profile. IdentityAccess owns the internal
actor, exact `(issuer, subject)` binding, enabled state, one server-derived
Organization context, capability grants, service delegations, opaque
application session, and trusted lifecycle propagation. Resource-owning
modules retain authority for their resource relationships and workflow state
and provide versioned trusted inputs through owned ports to the authorization
kernel. Keycloak roles or groups are never Flex Agent authorization evidence.

General invitation, membership, actor administration, account recovery, and
provider account provisioning are not introduced by this profile. A later
administration journey remains an IdentityAccess-owned capability behind an
approved application contract; a Keycloak administration API, when selected,
is only its provider adapter.

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

The current `keycloak-contract` Compose file starts PostgreSQL, Keycloak and its
database, plus the restricted NGINX identity gateway. It still expects the API
on the legacy documented host callback port and does not by itself compose the
SPA or an authenticated Assessment journey. The profile below replaces that
split topology when implemented; the infrastructure-only contract remains a
focused provider qualification fixture.

## Authenticated browser profile

The Development/Testing browser profile required by `STACK-DEC-27` must extend
this pinned contract and run through one documented project command. Its
canonical browser-visible origin is `http://localhost:18080`; it must provide:

- an NGINX gateway that serves or proxies the SPA at `/`, routes `/auth`,
  `/v1/assessment`, and existing `/sessions` requests to the API, and exposes
  Keycloak only through `/realms/flex-agent`;
- the exact OIDC redirect URI `http://localhost:18080/auth/callback`, with the
  browser returning to that same public origin and issuer, authorization,
  token, and JWKS configuration aligned with the gateway contract;
- API and migration containers on the profile network, reaching the
  application database at `postgres:5432` without a required host database
  port;
- migrations, health/readiness checks, deterministic synthetic seed, and
  disposable reset;
- an MFA-qualified synthetic Administrator in Keycloak;
- an exact pre-provisioned IdentityAccess binding from that provider identity
  to one enabled actor and one Organization;
- only the minimum application-owned capability grants, Assessment-owned
  relationship records, and Development/Testing source descriptors needed for
  the approved journey; and
- Playwright access through the real OIDC redirect, opaque application session,
  authorization kernel, Assessment API, and PostgreSQL state.

The profile must fail closed when the binding, one-Organization resolution,
accepted MFA evidence, grant, descriptor, route, or dependency is missing. It
must not use `/browser`, provider roles, browser-supplied scope, or a relaxed
Production authentication policy as authority. Test credentials remain
synthetic fixture data and must not appear in screenshots, logs, tracked task
state, or Production configuration.

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

Docker-backed Keycloak `26.7.0` signed back-channel logout and NGINX
restricted-route probes (`/realms/flex-agent` allowed, `/admin` and `/health`
denied) have executable evidence. The remaining browser PKCE, MFA, key
rotation, clock skew, account-disablement, outage, and multi-instance
callback matrix is still a later qualification gate. Do not claim that
remaining matrix from unit tests or the back-channel/NGINX probes alone.
