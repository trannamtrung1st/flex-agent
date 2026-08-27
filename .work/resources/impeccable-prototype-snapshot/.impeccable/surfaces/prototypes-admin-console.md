---
version: 1
slug: "prototypes-admin-console"
primary_target: "prototypes/src/routes/AdminPage.tsx"
related_targets: ["prototypes/src/features/admin/CampaignsArea.tsx", "prototypes/src/features/admin/CampaignRegistry.tsx", "prototypes/src/features/admin/EnrollmentsArea.tsx", "prototypes/src/features/admin/SampleArea.tsx", "prototypes/src/features/admin/sampleAreas.tsx"]
---

# Surface: Administrator Console (`/admin-console`)

## Scope & Mode

Operate. The Administrator workspace is a grouped Gangway over two
functional areas and five polished sample/empty destinations.
**Campaigns** (registry, then record configuration and activation) and
**Enrollments** (the Record Wall manifest) remain the working prototype.
Cohorts, Sessions, Users & Access, Policies, and Audit Log are
representative surfaces with honest empty/sample copy — not Agents,
Harnesses, Knowledge, integrations, billing, SSO/SCIM, or a policy-rule
builder. A persistent Gangway answers which area is active. Campaign
selection is URL-driven (`?campaign=`) and owned by the Campaigns
registry plus the Enrollments toolbar selector — not by horizontal tabs.
Built expressly to birth the missing component tier — dropdown selects,
full form patterns (text/date fields, radios, toggles, validation),
Gangway side navigation, and a dense datatable (sort, filter,
pagination) — for extraction into the production frontend.

## Audience, Job, Task

- Audience: an Administrator running assessments across cohorts.
- Job: find a campaign in a 10–100-scale registry; inspect/configure it;
  check cohort/session state; activate a cohort; locate organization
  access, policy bounds, and audit history.
- Content: roughly 20 synthetic campaigns with varied activation states;
  ~120 enrollments on the primary campaign; configuration sets per campaign;
  sample/empty plates for the five new destinations.
- Constraints: React + shared CSS (`prototypes/src/styles/`); canonical terminology (Campaign,
  Cohort, Enrollment, Attempt, Session, Result, Release); configuration
  freezes at cohort activation; WCAG 2.2 AA; no third color voice.

## Routing & navigation

- `/admin-console` redirects to `/admin-console/enrollments` (preserves `?campaign=`).
- `/admin-console/campaigns` — Campaign Registry.
- `/admin-console/campaigns?campaign=CMP-0042` — that Campaign Record and Configure ceremony.
- `/admin-console/enrollments?campaign=CMP-0042` — Enrollment Manifest for that campaign.
- `/admin-console/cohorts` and `/admin-console/sessions` — campaign-scoped sample/empty surfaces (same defaulting as Enrollments).
- `/admin-console/users-access`, `/policies`, `/audit-log` — organization/governance sample surfaces (no `campaign` query).
- Gangway groups: Assessment operations (CAM, COH, ENR, SES), Organization control (ACC, POL), Governance (AUD).
- CAM is the stable Campaigns domain item, remains current on both Registry and Campaign Record, and always opens the Campaign Registry.
- Operational links (COH, ENR, SES) preserve a seated `campaign` query. Organization and governance links omit it. Enrollments, Cohorts, and Sessions canonicalize a missing parameter to the remembered seated campaign, then the first campaign; an invalid parameter canonicalizes to the first campaign. Invalid campaign addresses on Campaigns show an unavailable-record plate rather than inventing context.
- Enrollments, Cohorts, and Sessions show one page-local Campaign Context instrument directly below the page heading: a shared `CampaignContext` component with Campaign picker followed by Activation. Registry, Campaign Record, invalid records, organization, and governance surfaces omit this instrument.

## Chosen direction (seed ebcaf1b5, THE ROLL, locked 2026-08-25)

**The Record Wall inside an Administrator shell.** The enrollment manifest
remains the Enrollments instrument: a glowing wall head (ENROLLMENT MANIFEST)
over one etched board frame with filter/sort toolbar. Campaign selection lives
in the shared page-local Campaign Context. Campaigns is registry-first: a searchable, sortable, paginated
table; opening a row reveals the existing Campaign Record — a
content-hugging etched frame with compact readout bands and a quiet
Configure key in the frame foot; the registry table itself carries the
shared selection/action contract (teal four-state select-mark, header-driven matching
escalation, persistent Export/Download/More bar with compact labels, portalled Actions
menu). Activation opens as a modal ceremony
plate with the single hot amber ACTIVATE key obeying the Amber Ration
Rule. A Gangway (232px, folding to 76px channel codes) provides quiet
structural navigation; at ≤1080px the shell swaps it for a leading
Bulkhead drawer. Raise (from HyperCard challenger): reading and editing
are two modes of one object — a row expands inline for inspection before
any modal is summoned. Approved comp: .impeccable/mocks/decision/admin-record-wall.webp
(comp-led build).

## Memorable moment

Summoning a ceremony plate dims the active frame; activation runs the
phosphor-sweep freeze — the Enrollments wall or Campaigns record frame seals
teal as the configuration locks.

## Unresolved

- Production retention and artifact semantics for campaign export/download/delete remain consumer-owned; this prototype demonstrates the reusable UI contract with synthetic CSV/JSON and draft-only delete.
