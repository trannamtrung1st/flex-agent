---
id: oidc-integration-harness-normalization
status: planned
created: 2026-08-27
updated: 2026-08-27
---

# Goal

Normalize the Keycloak, Docker Compose, and browser integration-test harness so
its names, topology, automation, and evidence accurately reflect what is
tested. Preserve the focused Keycloak provider-compatibility test, establish
one canonical Development/Testing Compose profile, and turn the existing Wave
8.1 manual OIDC proof into committed repeatable evidence. During the ADR-020
transition, keep two explicitly named browser modes on the same application
topology: a blocking canonical journey against the shipped `web-legacy/`
production selector, and a blocking transition regression against candidate
`web/` for the Wave 8.1 authentication shell. Neither mode may imply that the
candidate is Production before the approved cutover. The canonical journey
must traverse the real gateway, Keycloak, API, PostgreSQL application session,
application-owned authorization, and logout/revocation boundary.

This task improves implementation evidence; it does not change product meaning
or claim the full `AC-OPS-4` qualification matrix. Full `AC-OPS-4` remains
`Partial` until the explicitly deferred real MFA, key-rotation, clock-skew,
account-disablement, provider-outage, multi-instance callback/session, and
future privilege-change adoption cases pass.

# Sequencing and task boundary

- The intended insertion point is after frontend rebuild Wave 8.1 polish,
  verification, and review close and before Wave 8.2 implementation begins.
  Execute this as a separate stabilization task; do not fold it into
  `.work/active/impeccable-frontend-rebuild.md` or reopen the completed
  `.work/active/oidc-application-session-foundation.md`.
- At kickoff, reconcile that insertion point with the repository owner and the
  frontend task's current marker. If Wave 8.2 has already started, do not infer
  permission to overlap or rewrite it: finish or pause only at an owner-approved
  green handoff boundary, then record the actual insertion decision here.
- Do not leave either task with intentionally failing tests or half-applied
  state.
- Begin Wave 8.2 after this task's focused CI/browser gate and independent review
  pass. The task does not authorize unrelated frontend migration,
  production cutover, or later production journeys.

# Governing sources

- `AGENTS.md`, `.agents/skills/implementation-workflow/SKILL.md`, and
  `.work/README.md` — tracked state, specification-driven TDD, verification,
  review, and handoff rules.
- `docs/README.md#authority-by-concern` — product, requirement, UI/UX,
  architecture, and implementation authority boundaries.
- `docs/product/concept-model.md` — Organization isolation and explicit
  authorization at sensitive boundaries.
- `docs/product/mvp-scope.md` and `docs/product/overview.md` — P0 scope,
  provider-neutral identity, truthful qualification, and remaining production
  gates.
- `docs/requirements/mvp-operational-defaults.md`:
  - `REQ-OPS-9`–`REQ-OPS-17`: PKCE/server exchange, browser token exclusion,
    opaque cookie, expiry, revocation, concurrent sessions, logout, MFA, and
    one configured issuer.
  - `REQ-OPS-27`–`REQ-OPS-29`: exact pre-provisioned identity, one
    server-derived Organization, provider disablement/forced logout, and outage
    semantics.
  - `AC-OPS-4`: the complete acceptance matrix remains broader than this task.
- `docs/requirements/features/auth-resource-isolation.md`:
  `REQ-AUTH-1`, `REQ-AUTH-2`, `REQ-AUTH-4`, `REQ-AUTH-15`,
  `REQ-AUTH-16`, `REQ-AUTH-19`–`REQ-AUTH-21`, and
  `REQ-AUTH-26`–`REQ-AUTH-30`; `AC-AUTH-1`, `AC-AUTH-4`,
  `AC-AUTH-11`–`AC-AUTH-13`, and `AC-AUTH-21`.
- `docs/ui-ux/activity-campaign-journey.md` — server-confirmed capability,
  one Organization context, non-disclosing denial, and protected-state
  teardown.
- `docs/ui-ux/design-system/README.md` and the authentication-shell modules
  selected by `docs/ui-ux/design-system/implementation-guide.md` — accessible
  loading, denied, ready, logout, protected-content, keyboard, focus, and
  narrow-view verification. This task does not redesign those states.
- `docs/architecture/decisions/ADR-008-bounded-oss-component-set.md`:
  `OSS-DEC-1`, `OSS-DEC-2`, `OSS-DEC-7`–`OSS-DEC-9`, and
  `OSS-DEC-13`; identity, gateway, secret, supply-chain, and Compose evidence
  gates.
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`:
  `STACK-DEC-10`, `STACK-DEC-14`, `STACK-DEC-16`, and
  `STACK-DEC-19`–`STACK-DEC-27`.
- `docs/operations/provider-profiles/keycloak-oidc-contract.md` — current
  Keycloak profile contract, claimed evidence, and deferred matrix.
- `docs/contributing/development-harness.md` and
  `docs/contributing/workspace.md` — one-command local profile, Playwright
  rules, current frontend selector, and truthful gate status.
- Completed predecessor evidence:
  `.work/active/oidc-application-session-foundation.md` and Wave 8.1 evidence
  in `.work/active/impeccable-frontend-rebuild.md`.

# Current implementation findings

1. `AuthenticatedBrowserProfileTests` asserts raw strings in Compose, NGINX,
   realm JSON, seed SQL, and the wrapper script. These checks can pass while
   rendered Compose semantics, service readiness, routes, or the live OIDC
   journey fail.
2. `KeycloakBackChannelLogoutTests` is a focused provider-compatibility probe:
   it starts only Keycloak, rewrites the imported client through the admin API,
   receives logout through a raw host listener, and directly validates the
   token. It does not run either Compose profile, the real API back-channel
   endpoint, PostgreSQL application-session revocation, or application-owned
   authorization. Its name and placement overstate its integration scope.
3. `keycloak-contract.compose.yaml` and
   `authenticated-browser.compose.yaml` duplicate Keycloak/PostgreSQL/NGINX
   configuration without a tested inheritance boundary. The focused file
   contains an unused application PostgreSQL service and proxies application
   routes to an externally started host API, while the full profile embeds the
   API and application database.
4. The imported realm's back-channel target points to a separately hosted API,
   not the API service in the canonical full Compose profile. Consequently the
   full profile cannot itself prove provider-forced logout reaches and revokes
   the application session.
5. The candidate-development overlay documents a wrapper invocation that the
   wrapper does not consume. The wrapper validates only its hard-coded primary
   file, not the final rendered configuration when an overlay is used.
6. The broad .NET test command may skip the live Keycloak test when Docker or a
   host listener is unavailable. CI has no separate required OIDC job, so a
   successful implementation workflow does not prove that the Compose/browser
   journey ran.
7. Wave 8.1 recorded a real browser login, authenticated Home/Activities,
   logout, and narrow-shell proof, but that evidence is a manual/local
   confirmation rather than a committed repeatable Playwright suite.
8. Hard-coded `acr`/`amr` fixture claims exercise Flex Agent's accepted-strength
   claim path; they do not prove Keycloak OTP enrollment or a real MFA
   challenge. Test names, docs, and readiness rows must retain that distinction.
9. The authentication profile uses floating container tags where
   `OSS-DEC-13` and `STACK-DEC-16` require immutable release-profile artifacts.
   A client-secret fixture is tracked under a `secrets` path even though the
   approved local/CI secret boundary calls for generated synthetic mounted
   files. Implementation must reconcile these facts without placing values in
   this task record, logs, or browser artifacts.
10. Startup/readiness diagnostics are weak: the current wrapper can wait
    silently, API readiness is a TCP-open check, Keycloak has no Compose health
    condition in the full profile, and failure cleanup/evidence is not owned by
    a blocking CI job.
11. ADR-020 keeps `web-legacy/` as the shipped production selector until Phase
    9 cutover, while Wave 8.1's real-OIDC proof exercised candidate `web/`
    through the development overlay. A production-selector-only suite would
    leave the Wave 8.1 candidate proof manual; a candidate-only suite would
    violate the transition boundary and overstate production evidence.

# Target verification architecture

Use explicit layers whose names and failure semantics match their evidence.

| Layer | Purpose | Runtime dependencies | Required behavior |
| --- | --- | --- | --- |
| Deterministic contract tests | Validate application OIDC/session rules and fixture structure without claiming live provider behavior | No Docker or external network | Fast, non-skipping, included in ordinary .NET/web verification |
| Keycloak compatibility test | Prove pinned Keycloak can emit a signed standards-shaped logout token accepted by the provider adapter | Focused Keycloak container only | Clearly provider-scoped; no claim of API/database revocation |
| Canonical Compose contract | Prove final rendered services, images, mounts, networks, ports, routes, health, seed, and callback/back-channel addresses | Docker Compose | One non-interactive wrapper; semantic validation before startup; loopback-only host publications |
| Full-stack OIDC acceptance | Prove the real browser/provider/application boundary and application-session consequences | Canonical Compose plus Playwright | Blocking CI; no silent skip; synthetic data only; bounded diagnostics and cleanup |
| Candidate transition regression | Preserve Wave 8.1 auth-shell behavior without changing the shipped production selector | Canonical services plus the explicit ADR-020 candidate overlay and candidate Vite server | Blocking during the transition; named candidate evidence; no Production or cutover claim; retired or folded into canonical acceptance at Phase 9 cutover |
| Deferred qualification matrix | Certify adversarial provider/runtime behavior | Successor task(s) | Remains `Partial` and cannot be inferred from lower layers |

# Scope

## In

- Establish one canonical full-stack Development/Testing Compose contract for
  PostgreSQL, Keycloak and its separate database, migrations, seed, API, SPA,
  NGINX, and already-required artifact storage.
- Remove or reduce the redundant focused Compose profile after all useful
  provider and route evidence has a clear owner. The focused Keycloak test may
  continue to use Testcontainers and the shared realm fixture without
  pretending to be the full application profile.
- Make the canonical browser-visible issuer/origin and callback remain
  `http://localhost:18080` as required by `STACK-DEC-27`.
- Give Keycloak a profile-correct internal back-channel route that reaches the
  real API endpoint in the full Compose network. Focused compatibility tests
  may override their callback sink explicitly without changing the canonical
  application topology.
- Validate the final rendered Compose model rather than grepping source text:
  service set, exact images/digests, networks, dependencies, health checks,
  mount modes, loopback host publications, absence of host PostgreSQL
  publication, OIDC endpoints, callback, back-channel target, and forbidden
  `/browser` authority.
- Validate NGINX configuration syntax and live public-route behavior. The realm
  route is allowed; Keycloak administration, health, metrics, and master realm
  routes remain unavailable through the public gateway.
- Rename and, if useful for ownership clarity, relocate the existing Keycloak
  test so its project/class/test name says `compatibility` or
  `logout-token compatibility`, not full application integration.
- Replace the raw host-listener/port-race arrangement when a deterministic
  container-network or actual-API target is available. Retain direct-access
  grant only inside the focused synthetic compatibility fixture and never use
  it as evidence for the browser PKCE journey.
- Add a production-selector browser suite that starts from the public gateway,
  follows the real Authorization Code flow, observes `S256` PKCE, completes
  the API callback, receives a PostgreSQL-backed opaque application session,
  accesses one protected API/UI state through application-owned authorization,
  logs out, and proves the old application authority is unusable.
- Add a separate transition-mode candidate suite that commits the Wave 8.1
  anonymous gate, real PKCE return, authenticated Home/Activities, local logout,
  protected-state teardown, keyboard/focus, and narrow-shell checks. It must use
  the same canonical services through the explicit candidate overlay, must be
  labeled candidate/non-Production in commands and reports, and must not alter
  `pnpm build`, the SPA Dockerfile, or another production selector before
  ADR-020 Phase 9.
- Add a full-stack provider-forced/back-channel logout case that reaches the
  real API, validates the signed token, commits PostgreSQL revocation, and
  causes the existing browser session/protected request to lose authority
  within the approved bound.
- Add bounded fail-closed identity cases using (a) a synthetic provider identity
  without an eligible application binding and (b) an exact bound identity with
  zero or multiple eligible Organizations. Prove no actor, Organization
  membership, grant, or application session is created implicitly and the
  browser receives a non-disclosing outcome.
- Verify that browser local storage and session storage contain no provider
  access, ID, or refresh tokens; inspect the opaque application cookie's
  required attributes without persisting its value.
- Create a named `verify:oidc`/wrapper command for local and CI execution with
  prerequisite checks, bounded readiness, sanitized failure diagnostics, and
  guaranteed Compose cleanup. Docker unavailability is an explicit failure for
  this command and the required CI job.
- Add a blocking OIDC integration job to the implementation workflow. Keep it
  separate from ordinary deterministic .NET tests so execution and failure are
  visible. Initially run it for every implementation-relevant change; narrow
  path selection only after a reviewed dependency map proves equivalent
  coverage.
- Generate mounted synthetic secret files at runtime in an ignored,
  permission-bounded temporary location; never write values to `.work/`, CI
  output, browser storage, Playwright artifacts, or committed fixtures. Remove
  tracked bearer-capable secret files after the generated path is verified.
- Pin authentication-profile container images by immutable digest and verify
  the rendered profile uses those pins. Preserve the approved product-family
  lines and re-run relevant SBOM/vulnerability/secret checks.
- Update implementation-contract and workspace/readiness documentation so each
  layer's evidence and the remaining `AC-OPS-4` gaps are stated accurately.

## Out

- Real OTP/TOTP/WebAuthn enrollment or challenge automation and a claim of
  Keycloak MFA qualification. Hard-coded accepted-strength claims remain
  synthetic claim fixtures only until the later real-MFA gate.
- Key rotation, clock-skew injection, provider outage, account-disablement
  event-listener qualification, multi-instance callback/session execution,
  load/flood qualification, and the complete `AC-OPS-4` matrix.
- New login, session, authorization, Organization-selection, invitation,
  account-provisioning, password-recovery, or provider-administration product
  behavior.
- Changes to application-session domain semantics, database migrations, or
  public API contracts unless an observed harness defect proves the current
  implementation violates an already-approved requirement. Such a discovery
  must be recorded before scope expands.
- Frontend redesign, frontend production cutover, Assessment Wave 8.2 feature
  implementation, or changes to approved authentication-shell content.
- Production/Staging deployment, TLS certification, real identities, real
  Participant data, or production-readiness claims.
- Commits, pushes, pull requests, releases, or external credential changes.

# Requirement-to-evidence matrix

| Obligation | In-scope evidence | Explicit residual |
| --- | --- | --- |
| `REQ-OPS-9`, `REQ-OPS-10` | Browser follows real Keycloak authorization redirect; `code_challenge_method=S256`; API performs callback/exchange; browser storage scan has no provider tokens | Adversarial key rotation, clock skew, and outage remain later |
| `REQ-OPS-11`, `REQ-OPS-14` | Opaque cookie attributes; authenticated session projection; PostgreSQL-backed continuity; old cookie denied after logout | Multi-instance execution remains existing deterministic/persistence evidence plus later live qualification |
| `REQ-OPS-12` | Existing deterministic and PostgreSQL expiry-boundary suites remain green and are not weakened by fixture changes | This task does not add live elapsed-time qualification for the 30-minute/12-hour bounds |
| `REQ-OPS-13`, `REQ-OPS-15`, `REQ-OPS-29` | Local logout revokes before provider handoff; provider-forced logout reaches real API and protected request loses authority within 60 seconds | Account-disablement bridge and provider-outage matrix remain later |
| `REQ-OPS-16`, `STACK-DEC-21` | Exact accepted-strength claims admit the synthetic Administrator; missing/unrecognized claims remain covered by deterministic negative tests | This task does not claim a real Keycloak MFA ceremony |
| `REQ-OPS-17`, `REQ-OPS-27`, `REQ-OPS-28`, `STACK-DEC-19` | Known exact identity resolves one server-owned Organization; unbound, zero-Organization, and multi-Organization fixtures fail closed without application authority | Multi-issuer UI and provisioning remain out of scope |
| `REQ-AUTH-1`, `REQ-AUTH-2`, `AC-AUTH-1` | Protected request succeeds only after application session and current grant/relationship checks; anonymous/unbound access is denied | Broader protected-resource matrix remains owned by feature suites |
| `REQ-AUTH-16`, `REQ-OPS-28` | Browser/provider scope cannot select or create an Organization; ambiguous contexts fail before protected access | Future authenticated Organization selection/change remains out of scope |
| `REQ-AUTH-4`, `AC-AUTH-4` | Existing deterministic wrong-Organization protected-resource suites remain green and are not weakened by fixture changes | This task does not claim a new full-stack cross-Organization resource matrix |
| `REQ-AUTH-19`–`REQ-AUTH-21`, `AC-AUTH-11`–`AC-AUTH-13` | Old/revoked authority is denied, browser state tears down, failure is non-disclosing, and no protected mutation occurs | Held-SSE and privilege-change adoption remain separately tracked where not exercised here |
| `REQ-AUTH-15`, `REQ-AUTH-26`–`REQ-AUTH-29` | Back-channel handling uses the same server authorization/audit boundary; login denial and logout/revocation security events retain required minimized fields, append-only history, and no credentials or raw tokens | Broader authorization-audit lifecycle and durability matrix remains owned by ADR-003 suites |
| `REQ-AUTH-30`, `AC-AUTH-21` | Named positive, anonymous, unbound, ambiguous-Organization, revoked-session, and route-boundary cases execute in required CI; existing resource-specific negative suites remain green | This harness does not replace the per-resource wrong-participant, wrong-assignment, forged-ID, list/count, or cross-session matrices |
| `OSS-DEC-7`, `OSS-DEC-13`, `STACK-DEC-16` | Generated mounted synthetic secrets, immutable image digests, secret scan, relevant SBOM/vulnerability checks | Production operator secret injection/rotation remains a deployment gate |
| `STACK-DEC-14`, `STACK-DEC-27` | Focused Testcontainers compatibility plus canonical Compose and Playwright journey, one documented command, canonical gateway origin | Full production certification remains prohibited |

# Security and privacy verification

| Risk | Required control/evidence in this task |
| --- | --- |
| Login CSRF, state replay, or callback substitution | Real PKCE redirect plus existing deterministic state/nonce/correlation/replay negatives; exact callback/origin in rendered Compose |
| Provider claim becomes application authorization | Protected access depends on pre-provisioned actor, one server-derived Organization, current application grant/relationship, and action; provider roles/groups never enter the seed or assertions as authority |
| Back-channel forgery or replay | Full-stack case uses Keycloak-signed token; existing invalid-signature/JTI replay tests stay green; revocation consequence is observed through the application boundary |
| Test-driver privilege leaks into the browser/public gateway | Provider-administration and database assertions run only from the test runner through bounded `docker compose exec` helpers; no admin/database route or port is published, browser contexts receive no operator credential, and helper output is redacted |
| Session fixation or stale authority | No pre-login credential is promoted; opaque identifier is rotated/issued by the API; logout and forced logout make prior authority unusable |
| Browser/provider token disclosure | Storage and URL scan; cookie value is never printed or attached; logs, screenshots, traces, and test reports contain no token or credential values |
| Secret or credential committed to source | Runtime-generated mounted secret files, ignored temporary path, tracked-file scan, gitleaks, and no values in task state |
| Cross-Organization or unbound identity admission | Synthetic unbound identity fails without actor/session/grant creation; trusted seed supplies the only eligible Organization for the positive identity |
| Public infrastructure exposure | All host publications bind loopback; no host database port; gateway denies admin/health/metrics/master routes; forwarded-host behavior remains server-installed and tested |
| Flaky or silently absent evidence | Required CI job fails on missing Docker/browser, readiness timeout, skipped required test, or incomplete cleanup; each service emits bounded sanitized diagnostics |
| Overstated MFA/provider readiness | Names and status rows call hard-coded ACR/AMR a synthetic accepted-strength fixture and retain the real MFA/full-matrix residual |

# Planned implementation surfaces

| Surface | Planned responsibility |
| --- | --- |
| `deploy/compose/authenticated-browser.compose.yaml` | Canonical service topology, immutable image references, health/dependency rules, internal callback/back-channel reachability, read-only mounts, and loopback gateway publication |
| `deploy/compose/keycloak-contract.compose.yaml` and `deploy/compose/nginx/keycloak-contract.conf` | Retire after evidence parity, or reduce only if review proves a distinct non-duplicating contract remains necessary |
| `deploy/compose/authenticated-browser.candidate-dev.compose.yaml` | Required transition-only override with an invocation the wrapper actually supports and validates; never the canonical CI authority or a production pointer |
| `deploy/compose/keycloak/` | Reviewed realm template/static structure and, if required, generation input without committed bearer-capable values |
| `deploy/compose/authenticated-browser/` | Deterministic non-sensitive seed/config plus ignored generated secret/render output location; no tracked bearer-capable secret file |
| `deploy/compose/nginx/authenticated-browser.conf` | Public allowlist, API/SPA/realm routing, forwarded-header replacement, SSE behavior, and infrastructure-route denial |
| `build/scripts/authenticated-browser-profile.sh` | One user-facing lifecycle wrapper over the exact rendered file set; validation, start, readiness, status, reset, and cleanup |
| New or renamed `build/scripts/verify-oidc*.sh` | CI/local acceptance orchestration, required-mode prerequisite checks, named-case manifest assertion, sanitized diagnostics, timeouts, and `always` cleanup behavior |
| `tests/Runtime/FlexAgent.Runtime.Tests/AuthenticatedBrowserProfileTests.cs` | Replace source-string assertions with deterministic responsibilities that do not require Docker; move live rendered/profile checks to the named OIDC gate |
| New `tests/Integration/FlexAgent.Keycloak.Integration.Tests/` project | Relocate and rename the focused Keycloak logout-token compatibility test; remove PostgreSQL/application-integration implications and host-listener flakiness; include it explicitly in the solution and required OIDC gate |
| New `tests/Browser/FlexAgent.Oidc.Playwright/` pnpm workspace package | Transition-neutral, stack-owned Playwright config/specs; canonical shipped-selector cases plus explicitly named candidate-transition cases; stable role/name locators; bounded runner-only `docker compose exec` helpers for provider control/database assertions; no credential screenshots or traces; survives ADR-020 cutover/legacy retirement |
| `pnpm-workspace.yaml`, root `package.json`, and the new browser-test package | Exact-pinned Playwright dependency and named `verify:oidc` commands without changing the production selector or design-lab isolation |
| `.github/workflows/implementation.yml` | Separate blocking OIDC job with visible execution, bounded timeout, sanitized failure handling, and cleanup |
| `docs/operations/provider-profiles/keycloak-oidc-contract.md` | Accurate profile topology, layer names, commands, evidence, and residual qualification matrix |
| `docs/contributing/development-harness.md` and `docs/contributing/workspace.md` | Accurate local/candidate/CI commands and truthful `GATE-STACK-BROWSER`, `GATE-STACK-HTTP`, and `GATE-STACK-SESSION` status |

# Planned full-stack cases

| Case | Preconditions and action | Required result |
| --- | --- | --- |
| `OIDC-E2E-01` — PKCE login | Anonymous browser enters through `http://localhost:18080`, selects sign in, and authenticates as the bound synthetic Administrator | Redirect uses non-empty challenge with `S256`; callback returns to the canonical origin; application session is authenticated; no provider token appears in URL/local/session storage |
| `OIDC-E2E-02` — Cookie and protected authority | Continue from `OIDC-E2E-01` and request the current shell plus one stable protected read | Opaque application cookie is `HttpOnly`, `Secure`, and `SameSite=Lax`; protected response succeeds only through server-derived actor/Organization/current grants; provider roles are irrelevant |
| `OIDC-E2E-03` — Local logout | Invoke the real cookie-authenticated/antiforgery logout flow, including provider handoff when returned | Local application session is revoked and cookie cleared before provider outcome; replaying the old cookie cannot regain protected access; current session is anonymous |
| `OIDC-E2E-04` — Provider-forced logout | Establish a fresh session, then invoke Keycloak's synthetic forced-logout control through the test driver | Keycloak sends a signed Logout Token to the real API; PostgreSQL revocation commits; current/protected requests lose authority within 60 seconds; no raw token is logged |
| `OIDC-E2E-05A` — Unbound identity | Authenticate a synthetic Keycloak identity with no eligible exact application binding | Callback fails closed with a non-disclosing state; no actor, membership, grant, or live application session is created |
| `OIDC-E2E-05B` — Ambiguous Organization | Authenticate an exact bound synthetic identity with zero or multiple eligible application-owned Organizations | Callback fails closed before session creation; the browser/provider cannot select Organization scope; no new membership or grant is created |
| `OIDC-E2E-06` — Public route boundary | Probe canonical public paths before and after authentication | Required realm/resources, SPA, auth, and approved API routes behave as documented; admin, master-realm, health, metrics, and host database access are unavailable publicly |
| `OIDC-E2E-07` — Required-gate negative control | Run the verification wrapper with an injected missing prerequisite or intentionally failing route assertion in a bounded test of the wrapper | Command/job fails, names the safe reason, emits no secret/token, and still removes containers, volumes, generated secret material, and browser state |
| `OIDC-CANDIDATE-01` — Wave 8.1 transition regression | Run the candidate Vite server through the explicit overlay and real canonical services; exercise anonymous sign-in, PKCE return, Home/Activities, logout, keyboard focus, and narrow shell | The committed Wave 8.1 behavior passes without `/browser`; the report labels the target candidate/non-Production; no production pointer or cutover file changes |

# Test discipline

- First run each committed browser case against the current pre-fix baseline
  for that behavior and record PASS, FAIL, or BLOCKED. Existing behavior
  captured by a passing test is characterization evidence, not a red phase. For
  each observed defect or
  behavior change, preserve the smallest useful failing regression before the
  fix. Never manufacture a failure or call a missing test file/CI job “red.”
- For Compose/NGINX/script restructuring where unit-level red/green is not
  meaningful, use executable configuration negative controls: mutate a
  temporary rendered input to introduce a non-loopback port, host database
  publication, wrong callback/back-channel target, forbidden route, floating
  image, missing health dependency, or tracked secret path and prove validation
  rejects it before refactoring the real fixture.
- Never weaken an existing state/nonce/JWKS/logout/session/authorization test to
  obtain green. If the live provider reveals a domain defect, preserve it with
  the smallest failing regression at the owning layer before implementation
  scope changes.
- Playwright is acceptance evidence, not a replacement for deterministic
  application/session, PostgreSQL, signature/replay, and authorization tests.

# Implementation decisions

- **One canonical application profile.** Use
  `authenticated-browser.compose.yaml` as the sole full-stack browser contract.
  The interim implementation default is to retire
  `keycloak-contract.compose.yaml` and its host-API NGINX configuration after
  focused Keycloak compatibility and full-profile restricted-route coverage
  replace every useful assertion. Rationale: two independently authored
  topologies have already drifted, while `STACK-DEC-14` requires different
  test techniques, not duplicate configuration.
- **Focused test remains focused.** Keep one pinned-Keycloak compatibility test
  for signed logout-token production/validation, but name and locate it by that
  responsibility. It must not query PostgreSQL or claim application revocation.
- **Actual application consequence belongs to full-stack acceptance.** The
  canonical profile owns callback, application-session creation, protected
  request, local logout, signed back-channel delivery, and PostgreSQL revocation
  evidence.
- **Canonical CI uses the shipped production selector.** The full-stack job
  builds whatever `pnpm build`/the SPA Dockerfile currently defines as
  production. The candidate-dev overlay remains explicitly non-Production and
  is not the canonical OIDC acceptance authority.
  After Phase 9 cutover, the same acceptance suite follows the new production
  selector without a second auth contract.
- **Candidate evidence is a transition gate, not production evidence.** While
  ADR-020 keeps `web-legacy/` shipped, the same OIDC job also runs the named
  `OIDC-CANDIDATE-01` mode through a supported documented wrapper mode so Wave
  8.1 proof is repeatable. Validation must inspect the complete rendered file
  set. The mode cannot alter production build inputs and is removed or folded
  into canonical acceptance as part of the approved Phase 9 cutover.
- **Privileged test control stays outside the browser.** Provider-forced logout
  and scoped database/security-event assertions use bounded runner-side
  `docker compose exec` helpers against services already inside the canonical
  project. Do not publish Keycloak administration or PostgreSQL, add a browser
  test API, or expose operator credentials to page code, URLs, reports, or
  artifacts.
- **Required evidence cannot skip.** Ordinary local test suites may report a
  Docker-dependent compatibility test as unavailable, but the named OIDC
  verification command and CI job treat missing Docker, browser, or required
  test execution as failure. Playwright must emit a machine-readable report;
  the wrapper validates the exact required case-ID manifest for canonical and
  candidate modes, rejects skipped/unexpectedly absent/`only` cases, and does
  not rely on a brittle aggregate count alone.
- **Synthetic claim evidence is not real MFA evidence.** Preserve hard-coded
  accepted-strength claims only when needed for the bounded Administrator
  journey, label them accurately, and keep the live MFA gate open.
- **No new product contract is expected.** Compose/test topology, CI ownership,
  diagnostics, and fixture generation are implementation details. Amend the
  provider-profile/workspace documentation for accuracy. Create or amend an ADR
  only if implementation discovers a conflict with an approved `STACK-DEC-*`
  or selects a durable topology not already covered.

# Open questions and interim defaults

- **Q-OIDC-HARNESS-1 — Focused compatibility-test callback sink.** Should it
  retain a host listener or use a container-network callback sink?
  **Interim default:** use a repo-owned ephemeral callback-sink container on the
  focused Testcontainers network, built from an already-pinned repository
  runtime image; do not publish it to the host. Rationale: this works
  consistently on Linux CI and Docker Desktop and avoids a wildcard host
  listener, free-port race, or host-gateway behavior becoming provider
  compatibility evidence.
- **Q-OIDC-HARNESS-2 — Generated synthetic realm values.** Should the entire
  realm import be generated, or only bearer-capable client/operator secrets?
  **Interim default:** keep non-sensitive deterministic fixture identities in
  reviewed realm source, generate bearer-capable client/operator secret files
  at runtime, and prevent their values from entering source or evidence.
  Rationale: this removes transferable secret material without making realm
  structure opaque. If Keycloak import mechanics require a temporary rendered
  realm, generate it in an ignored permission-bounded directory and delete it
  during cleanup.
- **Q-OIDC-HARNESS-3 — Full-stack unbound-identity database assertion.** Should
  Playwright query PostgreSQL directly?
  **Interim default:** keep browser assertions at the public boundary and add a
  separate scoped integration assertion against the seeded database/security
  event state. Rationale: browser code must not gain database authority, while
  absence of unintended actor/session/grant creation still needs executable
  proof.
- **Q-OIDC-HARNESS-4 — CI frequency after stabilization.** Can the full OIDC
  job become path-filtered?
  **Interim default:** run it for every implementation-relevant change until a
  reviewed dependency map includes API authentication, IdentityAccess,
  migrations, gateway/Compose, realm/seed, SPA authentication shell, packages,
  and CI scripts. Rationale: premature filtering can silently omit a
  foundational regression.

# Plan

- [ ] **Phase 0 — Freeze the evidence contract and baseline.** Re-run focused
      deterministic human-auth tests, the current Keycloak compatibility test,
      Compose `config`, NGINX probes, and the Wave 8.1 real-OIDC smoke where
      available. Record actual commands, duration, skips, hangs, ports, service
      health, and sanitized failure behavior. Do not treat the prior silent
      test attempt as a pass. Reconcile the owner-approved insertion point with
      the frontend task marker before marking this phase current.
- [ ] **Phase 1 — Red tests for topology contracts.** Add the smallest failing
      tests/checks that require one canonical application profile, semantic
      rendered-config validation, loopback-only publications, separate
      Keycloak/application databases, correct internal back-channel delivery,
      pinned images, generated mounted secret paths, accurate candidate-overlay
      invocation, and restricted gateway routes. Treat test/class/project
      renaming as a mechanically verified ownership correction, not a
      fabricated behavioral red phase.
- [ ] **Phase 2 — Normalize fixtures and wrapper.** Refactor the Compose,
      NGINX, realm-rendering/seed, secret-generation, and wrapper surfaces to
      satisfy Phase 1. Add real service health/readiness, bounded timeouts,
      rendered-model validation, deterministic reset, and cleanup. Retire the
      redundant profile only after parity checks prove no useful gate was lost.
- [ ] **Phase 3 — Green the focused Keycloak compatibility layer.** Rename and
      isolate the provider test, replace the wildcard host listener when
      feasible, preserve exact signed-token validation, and verify the test
      cannot be mistaken for application/database logout evidence. Add an
      explicit CI-required mode that fails rather than skips when its
      prerequisites are promised.
- [ ] **Phase 4 — Characterize full-stack browser/API behavior.** Add the
      transition-neutral Playwright package, canonical cases for real PKCE
      login/session/protected access/local logout, signed provider-forced
      logout, unbound-identity denial, ambiguous-Organization denial, and the
      named Wave 8.1 candidate-transition case. Run every committed case against
      the post-Phase-3 baseline and record PASS, FAIL, or BLOCKED. Preserve real
      failures as regressions; do not manufacture red from absent historical
      automation or already-correct behavior.
- [ ] **Phase 5 — Close observed full-stack gaps.** Implement only the fixture,
      routing, readiness, and test-driver changes needed for the cases to pass.
      Reuse the current API/session/authorization behavior; do not weaken
      validation, synthesize browser authority, use `/browser`, or change
      approved session semantics to make the harness green.
- [ ] **Phase 6 — Add blocking CI ownership.** Add a distinct OIDC job and one
      named project command. Install locked dependencies/browser, validate and
      start the canonical profile, run focused compatibility and full-stack
      cases in canonical and candidate-transition modes, validate the exact
      machine-readable required-case manifest, emit bounded redacted diagnostics
      on failure, and always tear down containers/volumes and generated secret
      material. Use job/test timeouts and concurrency-safe project naming.
- [ ] **Phase 7 — Security, supply-chain, and regression verification.** Run
      focused human-auth runtime/persistence/JWKS/back-channel suites,
      architecture tests, full OIDC command, web auth-shell tests, Compose and
      NGINX validation, gitleaks, relevant SBOM/vulnerability checks, docs, and
      whitespace checks. Inspect Playwright accessibility snapshots and
      desktop/narrow screenshots for anonymous, ready, access-lost/logout, and
      non-disclosing denial states; store only inspected synthetic PNGs under
      `.playwright-mcp/` and keep traces/browser state untracked.
- [ ] **Phase 8 — Reconcile authority and evidence.** Update the Keycloak
      provider profile, workspace commands/gate rows, development harness, and
      predecessor/successor references. State exactly which `AC-OPS-4` cases
      now have live evidence and which remain deferred. Recheck product,
      requirements, UI/UX, and ADR boundaries; promote only durable technical
      changes that require authoritative documentation.
- [ ] **Phase 9 — Independent review and frontend resume handoff.** Obtain
      backend/architecture, security/privacy, and test-quality review. Resolve
      every blocking/high finding, record residuals, mark this task complete
      only from executable evidence, and hand back to the owner-approved Wave
      8.2 marker
      without changing its scope or checklist history.

# Planned verification

`pnpm verify:oidc` is the stable public local/CI gate. Phase 1 may finalize
subordinate script and mode names, but they must remain implementation details
behind that one command.

| Check | Status | Required evidence |
| --- | --- | --- |
| Baseline deterministic human-auth/JWKS/session suites | pending | Counts, pass/fail, and no unexpected skip |
| Focused Keycloak compatibility | pending | Pinned image starts; named signed logout-token case executes; required mode fails if unavailable |
| Rendered Compose contract | pending | `docker compose config --format json` semantics, immutable digests, mounts, ports, networks, dependencies, endpoints, and no forbidden route/host DB publication |
| NGINX syntax and live route allowlist | pending | Syntax pass; realm/resources as required; admin/health/metrics/master denied |
| Browser PKCE and application session | pending | Real authorization redirect with S256, callback, opaque cookie attributes, protected access, no browser tokens |
| Wave 8.1 candidate transition regression | pending | Explicit candidate overlay; anonymous/login/Home/Activities/logout/keyboard/narrow cases; report says candidate/non-Production; production selector unchanged |
| Local logout | pending | Application authority revoked before/independent of provider handoff; old session cannot access protected resource |
| Provider-forced logout | pending | Signed token reaches real API; PostgreSQL revocation commits; protected access denied within 60 seconds |
| Identity/Organization fail-closed | pending | Unbound plus zero/multiple-Organization non-disclosing denial and no implicit actor/Organization/grant/application-session creation |
| Candidate/production selector consistency | pending | Canonical job follows shipped selector; required transition mode is accurately labeled and rendered-config validated; no cutover pointer changes |
| Required CI job | pending | Machine-readable manifest proves every canonical and candidate case ran without skip/`only`; bounded timeout, redacted diagnostics, and cleanup verified on success and injected failure |
| Web auth-shell regression | pending | Existing unit/component tests plus Playwright accessibility and desktop/narrow evidence; no UI redesign |
| Authentication/security audit regression | pending | Login denial and logout/revocation events remain minimized, append-only, attributable where context exists, and free of credentials/raw tokens |
| Secret/privacy checks | pending | No tracked generated secret, token/cookie/credential leakage, real data, or unsafe Playwright artifact |
| Supply-chain | pending | Auth-profile images digest-pinned; relevant SBOM/vulnerability and license evidence passes |
| Documentation and whitespace | pending | `python3 scripts/check_docs.py` and `git diff --check` pass |
| Independent review | pending | No unresolved blocker/high finding; residual deferred matrix recorded |

# Success criteria

- A maintainer can explain which test is deterministic, provider-compatible,
  full-stack, or deferred from its name and command alone.
- One documented non-interactive command validates, starts, exercises, and
  cleans the canonical authenticated Development/Testing profile.
- The required CI job cannot be green when Docker/browser prerequisites are
  missing or required OIDC cases did not execute.
- Real browser evidence covers PKCE login, server-side callback, opaque
  PostgreSQL-backed session, application-owned protected access, local logout,
  signed back-channel logout, and unbound/ambiguous-identity fail-closed cases.
- Wave 8.1 candidate authentication-shell evidence is committed and blocking
  during the ADR-020 transition while remaining explicitly non-Production.
- Keycloak roles/groups, `/browser`, browser-supplied Organization, raw provider
  tokens, and tracked bearer-capable secret files never become authority or
  evidence shortcuts.
- Full `AC-OPS-4`, real MFA, key rotation, clock skew, outage,
  account-disablement, and multi-instance status remain truthfully `Partial`.
- The frontend rebuild proceeds into Wave 8.2 without losing or reopening the
  completed Wave 8.1 polish checkpoint and without an unrecorded scope change.

# Current state

The plan has completed a consistency and implementation-readiness review and is
technically ready once the kickoff scheduling gate below is reconciled. No
implementation step has started and no baseline command has been claimed as
passing by this task.
The shared worktree contains frontend rebuild/task-file changes and untracked
Playwright PNGs. They belong to the frontend rebuild; preserve them and do not
stage, overwrite, rename, delete, or treat them as this task's evidence.

Next action when scheduled: reconcile the intended post-Wave-8.1/pre-Wave-8.2
insertion point with the repository owner and current frontend task marker;
then mark Phase 0 current and capture the baseline with explicit
Docker/Playwright prerequisite and timeout behavior.

# Findings / deviations

- The repository owner clarified on 2026-08-27 that the intended stabilization
  insertion point is after Wave 8.1 polish and before Wave 8.2. The frontend
  task marker is mutable working state and changed during planning; it must be
  reconciled at kickoff rather than treated as silent authority to overlap an
  active frontend microcycle.
- Wave 8.1's 11-case local confirmation is valuable discovery evidence but is
  not repeatable CI evidence because no committed production OIDC Playwright
  suite or required CI job owns it.
- The predecessor OIDC foundation correctly kept the broader live matrix open;
  this task narrows and strengthens evidence rather than reopening its
  implemented domain/session design.
- The 2026-08-27 readiness review separated shipped-production and candidate
  transition evidence, corrected the browser-test red/characterization rule,
  added machine-readable required-case proof, selected transition-neutral test
  ownership, and repaired Organization/audit requirement traceability.

# Blockers

None for technical planning. Scheduling is gated on reconciling the intended
insertion point with the owner and the current frontend handoff state.
Implementation requires Docker Compose and a Playwright browser in its local or
CI execution environment; the named verification gate must fail explicitly
rather than silently pass when either is unavailable.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [ ] Governing specifications were rechecked
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
