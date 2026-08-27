import { ReadoutGrid, ReadoutGridField, ReadoutGridRow } from "../../components";
import { SampleArea } from "./SampleArea";

export function CohortsArea() {
  return (
    <SampleArea
      title="Cohort Register"
      description="Administrative groupings whose members each receive isolated, comparable Sessions."
      advisoryLabel="Prototype"
      advisoryCopy="Cohort records are not loaded in this synthetic dataset. A Cohort is an administrative grouping, never a shared room. Future / not in current MVP."
      emptyLabel="No cohort records loaded"
      emptyNote="Frozen configuration at cohort activation is the fairness baseline. This plate will not invent cohort membership or accommodations."
      campaignScoped
    />
  );
}

export function SessionsArea() {
  return (
    <SampleArea
      title="Session Monitor"
      description="Isolated participant Sessions under the selected Campaign's resolved configuration."
      advisoryLabel="Future / not in current MVP"
      advisoryCopy="Live Session telemetry is not seated here. Each Session remains one participant; shared rooms are deferred."
      emptyLabel="No session telemetry loaded"
      emptyNote="Operational monitoring would list isolated Sessions, remaining time, and Record state. This prototype does not fabricate live session traffic."
      campaignScoped
    />
  );
}

export function UsersAccessArea() {
  return (
    <SampleArea
      title="Users & Access"
      description="Organization operators, roles, and invitations — not participant Enrollments."
      advisoryLabel="Future / not in current MVP"
      advisoryCopy="Administrators and Reviewers are operator accounts. Participants remain Campaign Enrollments and are not managed on this plate. This area is a design-lab reference and is absent from production."
      emptyLabel="No access records loaded"
      emptyNote="Role assignment and invitations are not seated in this prototype. Enterprise SSO, SCIM, and service accounts are out of scope."
    />
  );
}

export function PoliciesArea() {
  return (
    <SampleArea
      title="Organization Policies"
      description="Non-bypassable tenant boundaries. Lower scopes may narrow permissions; they may not widen them."
      advisoryLabel="Future / not in current MVP"
      advisoryCopy="These are demonstration readouts of organization limits — not a preference sheet and not a policy-rule builder. Absent from production."
    >
      <ReadoutGrid label="Organization policy bounds" columns={2}>
        <ReadoutGridRow label="Authorization and retention">
          <ReadoutGridField term="Authorization">
            Explicit at every sensitive boundary
          </ReadoutGridField>
          <ReadoutGridField term="Retention">
            Inspectable history; no silent overwrite
          </ReadoutGridField>
        </ReadoutGridRow>
        <ReadoutGridRow label="Memory and human review">
          <ReadoutGridField term="Memory">
            Stable during assessment; no cross-participant learning
          </ReadoutGridField>
          <ReadoutGridField term="Human review">Required before Release</ReadoutGridField>
        </ReadoutGridRow>
      </ReadoutGrid>
      <p className="frozen-line">Policy authoring is not seated in this prototype</p>
    </SampleArea>
  );
}

export function AuditLogArea() {
  return (
    <SampleArea
      title="Audit Log"
      description="Immutable actor, action, object, and timestamp history for configuration, review, and Release."
      advisoryLabel="Future / not in current MVP"
      advisoryCopy="Audit-relevant history cannot be silently overwritten. This prototype does not fabricate production events. Production audit management is not seated here."
      emptyLabel="No audit events loaded"
      emptyNote="A seated log would record operator identity, action, object, and an unambiguous timestamp. Nothing is appended from this demonstration plate."
    />
  );
}
