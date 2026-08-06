# MVP operational defaults

Approved cross-cutting defaults for P0 Submission intake, authentication
sessions, protected-data lifecycle, and recovery placement.

## Status and authority

| Field | Value |
| --- | --- |
| **Status** | Approved |
| **Owner** | Product Lead |
| **Approvers** | Product Lead, Architecture Lead, Security/Privacy reviewer |
| **Version** | 0.2 |
| **Approved date** | 2026-08-06 |
| **Applies to** | MVP reference deployment and any deployment that does not supply a stricter approved Organization policy |
| **Governs** | Observable operational limits and lifecycle defaults; it does not select infrastructure products or make a compliance claim |

These values resolve `Q-ARCH-10` through `Q-ARCH-13` from the initial MVP
architecture review. Version 0.2 clarifies that external malware scanning is a
policy-controlled validation step and is not mandatory for the initial
text/Markdown-only categories. Organization policy may impose stricter limits
or shorter retention where dependencies permit, but lower scopes may not widen
approved Organization bounds. A deployment-specific override must be explicit,
versioned, authorized, auditable, and tested before protected data is accepted.

## Submission intake defaults

### Submission intake requirements

- `REQ-OPS-1`: Direct text input must not exceed 1 MiB per Submission version.
- `REQ-OPS-2`: One Submission version may contain at most 10 attachments, each
  no larger than 10 MiB, with no more than 25 MiB of attachments in total.
- `REQ-OPS-3`: The MVP must accept only `.txt` and `.md` attachment categories
  whose content passes strict UTF-8 validation. A filename extension alone is
  not validation evidence.
- `REQ-OPS-4`: Archives, executable content, repository URLs, and links that
  would require fetching external content must remain disabled. Submitted links
  are inert text and must never trigger an automatic fetch.
- `REQ-OPS-5`: Every validation step required by the frozen material-category
  policy must finish within two minutes or leave the version non-accepted with
  an explicit retry or failure state. External malware scanning may be
  `disabled_by_approved_policy` for the initial strictly validated UTF-8 `.txt`
  and `.md` categories. When scanning is `required`, unavailable, stale,
  timed-out, or inconclusive scanning must never cause acceptance. A disabled
  scanner must not be recorded or presented as having returned a clean result.
- `REQ-OPS-6`: Incomplete upload payloads must be disposed of within 24 hours of
  the last upload activity. Rejected or quarantined payload bytes must be
  disposed of within seven days unless an authorized security investigation or
  stricter policy requires an earlier or held disposition.
- `REQ-OPS-7`: Bounded rejected-upload metadata may remain under the applicable
  audit and lifecycle policy, but it must not preserve rejected payload content
  unnecessarily.
- `REQ-OPS-8`: An authorized artifact-download capability must expire no later
  than five minutes after issuance, remain bound to the exact actor, scope, and
  artifact version, and be denied after permission revocation.

## OIDC and application-session defaults

### OIDC and application-session requirements

- `REQ-OPS-9`: Browser sign-in must use OIDC Authorization Code flow with PKCE.
  The API server performs the code exchange and validates the provider response.
- `REQ-OPS-10`: Provider access, ID, and refresh tokens must not be stored in
  browser local storage or session storage. Refresh credentials, when required,
  remain server-side and encrypted through the deployment's protected secret
  facility.
- `REQ-OPS-11`: The browser application session uses an opaque cookie marked
  `HttpOnly`, `Secure`, and `SameSite=Lax`. The API must rotate its session
  identifier after login, privilege change, and sensitive reauthentication.
- `REQ-OPS-12`: A normal application session expires after 30 minutes of
  inactivity and no later than 12 hours after creation. Deployment policy may
  shorten either bound.
- `REQ-OPS-13`: Revocation must affect new protected requests immediately and
  an active request or event stream within 60 seconds. The client must reconnect
  through the authenticated path rather than treating an old stream as
  authority.
- `REQ-OPS-14`: Concurrent sessions may exist, but each must have a stable
  identity, bounded lifecycle, audit context, and individual revocation path.
- `REQ-OPS-15`: Logout must revoke the Flex Agent application session and
  initiate provider logout where the selected OIDC provider safely supports it.
  Session expiry must preserve unsent user input locally only when it can be
  retained without exposing protected data to another actor.
- `REQ-OPS-16`: MFA is required for Administrator and Reviewer access.
  Participant MFA is Organization-configurable and recommended. An Organization
  may require recent authentication for sensitive Release or export actions.
- `REQ-OPS-17`: The MVP reference deployment supports one configured OIDC issuer
  at a time while preserving the provider-neutral stable `(issuer, subject)`
  identity and adapter boundary needed for future multi-issuer support.

## Protected-data lifecycle defaults

### Default policy matrix

| Record or payload class | Default disposition clock | Default duration and disposition |
| --- | --- | --- |
| Incomplete temporary upload | Last upload activity | Delete within 24 hours |
| Rejected or quarantined payload bytes | Rejection or quarantine decision | Delete within 7 days unless an authorized hold applies |
| Operational logs without protected content | Log event time | Delete within 30 days |
| Terminal durable-work records and bounded failure diagnostics | Work terminal time | Delete or irreversibly minimize within 90 days |
| Idempotency records | Related command or work terminal time | Retain for 90 days, then dispose dependency-safely |
| Accepted Submissions and exact accepted versions | Activity closure | Retain for 365 days |
| Session messages, transcripts, resolved configuration, and execution manifest | Activity closure | Retain for 365 days |
| Evidence, Evaluations, Human revisions, Review decisions, Results, and Releases | Activity closure | Retain for 365 days |
| Authentication and security audit metadata without protected payload content | Audit event time | Retain for 730 days |
| Automated backup sets | Backup creation | Expire after 35 days, subject to restore-chain safety and legal hold |
| Point-in-time recovery data | Recovery-data creation | Maintain a rolling 14-day recovery window |
| Non-authoritative Participant working context | Owning Session or work terminal time | Delete within 24 hours after terminal recovery/reconciliation completes |

### Protected-data lifecycle requirements

- `REQ-OPS-18`: Every protected record class must resolve to an approved,
  versioned lifecycle policy before the system accepts or creates the record.
- `REQ-OPS-19`: Organization policy establishes non-bypassable bounds. Activity
  policy may shorten retention but must not widen those bounds or break stable
  Evidence, Evaluation, revision, decision, Result, Release, or audit lineage.
- `REQ-OPS-20`: Deletion must respect dependency order, preserve the minimum
  authorized provenance needed to explain lawful unavailability, and never
  silently rewrite immutable or audit-relevant history.
- `REQ-OPS-21`: An explicit legal or investigation hold may suspend normal
  disposition only for authorized scope, actor, reason, and duration. Applying,
  changing, and releasing a hold must be audited.
- `REQ-OPS-22`: Backup expiry, restore, export, and deletion must preserve
  Organization isolation. A deleted live record must not be represented as
  fully disposed until policy-governed backup copies also expire or are
  explicitly handled.
- `REQ-OPS-23`: Raw protected content must not be copied into operational logs,
  metrics, traces, idempotency records, or failure diagnostics to extend its
  lifecycle indirectly.

## Recovery placement default

- `REQ-OPS-24`: Regional-disaster recovery copies must be encrypted and stored
  in a separate failure domain or secondary region within the same approved
  data jurisdiction as the primary deployment.
- `REQ-OPS-25`: Recovery access, restore, lifecycle, hold, and deletion controls
  must be at least as restrictive as the corresponding primary controls.
- `REQ-OPS-26`: The deployment must measure recovery against the architecture's
  approved targets. If an Organization's approved residency policy prevents the
  required copy placement, the deployment must approve and publish the weaker
  achievable RPO/RTO instead of claiming the default target.

## Acceptance criteria

- `AC-OPS-1`: Given an attachment outside an approved type, count, per-file, or
  aggregate limit, when intake validates it, then the version remains
  non-accepted and no Session may bind it.
- `AC-OPS-2`: Given a policy that requires external scanning, when the scanner
  is unavailable, stale, timed out, or inconclusive, then intake never marks the
  payload accepted and exposes a bounded safe recovery state. Given an approved
  policy that disables external scanning for the initial text/Markdown
  categories, intake may accept only after every remaining required validation
  succeeds and must record the policy mode without claiming a clean scan.
- `AC-OPS-3`: Given a repository URL, archive, invalid UTF-8 file, or misleading
  filename, when intake processes it, then no external fetch, archive expansion,
  execution, or acceptance occurs.
- `AC-OPS-4`: Given login, privilege change, expiry, revocation, and logout,
  when protected requests and SSE connections are exercised, then cookie,
  rotation, timing, and revocation behavior satisfies `REQ-OPS-9` through
  `REQ-OPS-17` without exposing provider tokens to browser storage.
- `AC-OPS-5`: Given records from every lifecycle row and an applicable hold,
  when the lifecycle worker runs across each boundary, then it disposes only
  eligible records, preserves dependency-safe lineage, and audits holds and
  dispositions without copying protected content into diagnostics.
- `AC-OPS-6`: Given restored database and artifact backups in an isolated
  environment, when lineage and isolation checks run, then exact versions,
  integrity metadata, Organization scope, and hold/lifecycle state remain
  consistent.

## Related decisions and specifications

- [MVP architecture](../architecture/mvp-architecture.md)
- [ADR-002: Authorization enforcement and delegation](../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md)
- [ADR-006: MVP architecture baseline and evolution](../architecture/decisions/ADR-006-mvp-architecture-baseline-and-evolution.md)
- [ADR-007: OSS-first self-hostable deployment](../architecture/decisions/ADR-007-oss-first-self-hostable-deployment.md)
- [ADR-008: Bounded OSS component set and provider/deployment defaults](../architecture/decisions/ADR-008-bounded-oss-component-set.md)
- [Authorization and resource isolation](features/auth-resource-isolation.md)
- [Submission and Attempts](features/submission-attempts.md)
- [Text Session lifecycle](features/session-text-lifecycle.md)
- [Evidence and Evaluation](features/evidence-evaluation.md)
- [Human review and Result Release](features/review-result-release.md)
