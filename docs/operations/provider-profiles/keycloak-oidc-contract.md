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

- Keycloak image: `quay.io/keycloak/keycloak:26.7.0` (digest-pinned in Compose)
- PostgreSQL: `postgres:18` (digest-pinned in Compose)
- NGINX: `nginx:1.30.4` (digest-pinned in Compose)
- Realm: `flex-agent`
- Client: `flex-agent-api`
- Accepted MFA evidence: synthetic hardcoded `acr=acr:mfa` and/or `amr` containing `mfa`. This is not a live Keycloak OTP/WebAuthn qualification.

## Evidence layers

| Layer | Command | What it proves |
| --- | --- | --- |
| Deterministic contract | ordinary `dotnet test` / `verify-dotnet` | Application OIDC/session rules and fixture structure. Does not start Keycloak. |
| Keycloak logout-token compatibility | `pnpm verify:oidc` (`FlexAgent.Keycloak.Integration.Tests`) | Pinned Keycloak emits a signed Logout Token the adapter accepts. Does not prove API or PostgreSQL revocation. |
| Canonical Compose contract | `bash build/scripts/authenticated-browser-profile.sh validate` | Rendered services, digest pins, loopback gateway, generated secrets, in-compose back-channel `http://api:8080/auth/backchannel-logout`. |
| Full-stack OIDC acceptance | `pnpm verify:oidc` canonical Playwright | Real PKCE, opaque session, protected read, local logout, signed back-channel revocation, unbound/ambiguous fail-closed, public route denials. |
| Candidate transition regression | `pnpm verify:oidc` candidate/non-Production project | Wave 8.1 auth shell against candidate `web/` through the explicit overlay. Not Production. |
| Deferred `AC-OPS-4` matrix | successor task(s) | Real MFA, key rotation, clock skew, account-disablement, provider outage, multi-instance callback/session. |

## Compose

The canonical Development/Testing browser profile is one documented command:

```bash
bash build/scripts/authenticated-browser-profile.sh
```

Use `down`, `reset`, `status`, `seed`, `validate`, `--overlay candidate`, or
`--project-name`. The wrapper generates bearer-capable client and operator
secrets into an ignored `.generated/` directory, renders the realm import, and
fails if Docker Compose is missing. Do not copy those values into production
secret stores, logs, or browser artifacts.

The focused Keycloak logout-token compatibility test uses Testcontainers and
the shared realm template. It does not run Compose, the API, or PostgreSQL
application-session revocation. Docker published ports arrive as non-loopback
source addresses, so Keycloak's master realm treats host HTTP token requests as
external (`HTTPS required`). The test uses in-container `kcadm` to set
`sslRequired=NONE` on master after startup. That is a harness workaround, not
production TLS policy. The imported `flex-agent` realm already sets
`sslRequired` to `none` for the HTTP Development/Testing origin.

The blocking local/CI gate is:

```bash
pnpm verify:oidc
```

Implementation CI runs that command in the `oidc` job of
[`.github/workflows/implementation.yml`](../../../.github/workflows/implementation.yml)
and always tears down the Compose project. Playwright lives in
`tests/Browser/FlexAgent.Oidc.Playwright` (`@flex-agent/oidc-playwright`).

Required case IDs:

| ID | Mode |
| --- | --- |
| `OIDC-E2E-01` | Canonical PKCE login |
| `OIDC-E2E-02` | Opaque cookie and protected read (`Secure` follows request scheme) |
| `OIDC-E2E-03` | Local logout |
| `OIDC-E2E-04` | Provider-forced logout through the real API |
| `OIDC-E2E-05A` | Unbound identity fail-closed |
| `OIDC-E2E-05B` | Zero/ambiguous Organization fail-closed |
| `OIDC-E2E-06` | Public route allowlist |
| `OIDC-E2E-07` | Wrapper negatives and injected-failure cleanup |
| `OIDC-CANDIDATE-01` | Wave 8.1 candidate/non-Production auth shell |

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
- an MFA-qualified synthetic Administrator in Keycloak whose `acr`/`amr`
  claims are a synthetic accepted-strength fixture;
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

The imported `flex-agent-api` client sets `backchannel.logout.url` to
`http://api:8080/auth/backchannel-logout` so Keycloak can reach the API on the
canonical Compose network. Direct-access grants remain enabled only so the
focused compatibility fixture can create a synthetic provider session without
a browser. Do not copy that grant into Production.

## Logout-token scope

Validated back-channel Logout Tokens follow the OpenID Connect Back-Channel
Logout contract:

- `sid` only: tombstone and revoke that provider session.
- `sub` only: write an `(issuer, subject)` logout watermark and revoke all
  matching application sessions. A later login may succeed only with a newer
  ID-token `iat`.
- `sid` and `sub`: revoke the intersecting provider session only. Do not
  watermark the identity or revoke sibling sessions for the same subject.

## Current qualification

`pnpm verify:oidc` is the required live evidence command. It covers signed
logout-token compatibility, rendered Compose semantics, NGINX allowlist,
browser PKCE, opaque PostgreSQL-backed sessions, local logout, provider-forced
logout through the real API, unbound and ambiguous-Organization fail-closed
cases, and the named candidate/non-Production Wave 8.1 shell. Full `AC-OPS-4`
remains `Partial` until real MFA, key rotation, clock skew, account
disablement, provider outage, and multi-instance callback/session cases pass.
The Development HTTP gateway uses same-as-request cookie `Secure` flags;
`Secure=true` on every cookie remains a TLS/production concern.
