import { useCallback, useEffect, useMemo, useState } from "react";
import { useProductionApi } from "../api/production-api";
import {
  EnrollmentRateLimitedCopy,
  createProductionEnrollmentClient,
  enrollmentFailureCopy,
  type AssignmentSummaryV1,
} from "../api/production-enrollment";
import { AssignmentPlate } from "../components/work/AssignmentPlate";
import { CeremonyArea, CeremonyUnavailable, CeremonyWait } from "../components/shell/SessionChrome";
import { campaignDeadlineCopy, formatCampaignInstant } from "../lib/campaign-timezone";
import { Key, OperateArea, StateReadout } from "../design-system";

function assignmentLabel(item: AssignmentSummaryV1): string {
  return item.activity_title ?? item.task_title ?? item.enrollment_id;
}

function deadlineCopy(item: AssignmentSummaryV1): string {
  if (!item.deadline_utc) {
    return "No exclusive cutoff";
  }
  return campaignDeadlineCopy(formatCampaignInstant(item.deadline_utc, item.time_zone_id ?? "UTC"));
}

function isReleasedRecord(status: string): boolean {
  return /releas|seal/i.test(status);
}

export function ProductionMyWorkPage() {
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const [items, setItems] = useState<AssignmentSummaryV1[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  const load = useCallback((signal?: { cancelled: boolean }) => {
    return client.listMyWork()
      .then((page) => {
        if (signal?.cancelled) return;
        setItems(page.items);
        setError(null);
      })
      .catch((caught: unknown) => {
        if (!signal?.cancelled) {
          setError(enrollmentFailureCopy(caught, "My work is not available."));
        }
      });
  }, [client]);

  useEffect(() => {
    const signal = { cancelled: false };
    void load(signal);
    return () => {
      signal.cancelled = true;
    };
  }, [load]);

  if (error) {
    const rateLimited = error === EnrollmentRateLimitedCopy;
    return (
      <CeremonyUnavailable
        title={rateLimited ? "Too many requests" : "My work unavailable"}
        note={error}
        danger
        recovery={rateLimited ? {
          label: "Try again",
          disabled: pending,
          onClick: () => {
            setPending(true);
            void load().finally(() => setPending(false));
          },
        } : undefined}
      />
    );
  }

  if (items === null) {
    return (
      <CeremonyArea label="My work" title="My work">
        <CeremonyWait label="Loading My work…" />
      </CeremonyArea>
    );
  }

  if (items.length === 0) {
    return (
      <OperateArea
        className="workspace-area work-plane assignment-board--hug"
        label="My work"
        title="My work"
        description="Current Assignments for the signed-in Participant. Open an assignment to prepare a Submission version."
        empty={{
          label: "No current assignments",
          note: "There is no assigned work for the current authorized relationship.",
        }}
      />
    );
  }

  return (
    <OperateArea
      className="workspace-area work-plane"
      framed={false}
      label="My work"
      title="My work"
      description="Current Assignments for the signed-in Participant. Open an assignment to prepare a Submission version."
    >
      <div className="assignment-bays plate-bays--hug">
        <section className="assignment-bay" aria-labelledby="current-assignments">
          <h2 className="assignment-bay-head" id="current-assignments">
            Current assignments
          </h2>
          <div className="assignment-bay-plates">
            {items.map((item) => {
              const label = assignmentLabel(item);
              const released = isReleasedRecord(item.status);
              return (
                <AssignmentPlate
                  key={item.enrollment_id}
                  label={label}
                  released={released}
                  rows={[
                    { term: "Campaign", value: item.activity_title ?? item.enrollment_id },
                    { term: "Assignment", value: item.task_title ?? "Task not titled", className: "assignment-plate-row--title" },
                    { term: "Deadline", value: deadlineCopy(item) },
                    {
                      term: "Record",
                      value: (
                        <StateReadout
                          variant={released ? "sealed" : "rest"}
                          solid={released}
                          label={item.status}
                          className="assignment-record"
                          labelClassName="assignment-record-label"
                        />
                      ),
                      className: "assignment-plate-row--record",
                    },
                  ]}
                  action={
                    <Key variant="open" to={`/my-work/${item.enrollment_id}`} ariaLabel={`Open ${label}`}>
                      Open
                    </Key>
                  }
                />
              );
            })}
          </div>
        </section>
      </div>
    </OperateArea>
  );
}
