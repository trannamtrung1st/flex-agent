import { AssignmentPlate } from "./AssignmentPlate";
import { AssignmentRecordReadout } from "./AssignmentRecordReadout";
import { Key } from "../../design-system";
import type { AssignmentSummaryV1 } from "../../api/production-enrollment";
import { campaignDeadlineCopy, formatCampaignInstant } from "../../lib/campaign-timezone";
import { enrollmentStatusCopy } from "../../lib/enrollment-presentation";

export function assignmentWorkLabel(item: AssignmentSummaryV1): string {
  return item.activity_title ?? item.task_title ?? item.enrollment_id;
}

export function assignmentDeadlineCopy(item: AssignmentSummaryV1): string {
  if (!item.deadline_utc) {
    return "No exclusive cutoff";
  }
  return campaignDeadlineCopy(formatCampaignInstant(item.deadline_utc, item.time_zone_id ?? "UTC"));
}

export function isReleasedAssignment(status: string): boolean {
  return /releas|seal/i.test(status);
}

export function AssignmentWorkPlate({ item }: { item: AssignmentSummaryV1 }) {
  const label = assignmentWorkLabel(item);
  const released = isReleasedAssignment(item.status);
  return (
    <AssignmentPlate
      label={label}
      released={released}
      rows={[
        { term: "Campaign", value: item.activity_title ?? item.enrollment_id },
        { term: "Assignment", value: item.task_title ?? "Task not titled", emphasis: "title" },
        { term: "Deadline", value: assignmentDeadlineCopy(item) },
        {
          term: "Record",
          value: (
            <AssignmentRecordReadout
              variant={released ? "sealed" : "rest"}
              solid={released}
              label={enrollmentStatusCopy(item.status)}
            />
          ),
          emphasis: "inline",
        },
      ]}
      action={
        <Key variant="open" to={`/my-work/${item.enrollment_id}`} ariaLabel={`Open ${label}`}>
          Open
        </Key>
      }
    />
  );
}
