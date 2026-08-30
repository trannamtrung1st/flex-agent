import { useMyWorkList } from "../api/useMyWorkList";
import {
  EnrollmentRateLimitedCopy,
} from "../api/production-enrollment";
import { CeremonyArea, CeremonyUnavailable, CeremonyWait } from "../components/shell/SessionChrome";
import { AssignmentWorkPlate } from "../components/work/AssignmentWorkPlate";
import { Grid, OperateArea } from "../design-system";

const MY_WORK_DESCRIPTION =
  "Current Assignments for the signed-in Participant. Open an assignment to prepare a Submission version.";

export function ProductionMyWorkPage() {
  const { items, error, pending, setPending, load } = useMyWorkList(true);

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
        description={MY_WORK_DESCRIPTION}
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
      description={MY_WORK_DESCRIPTION}
    >
      <div className="assignment-bays">
        <section className="assignment-bay" aria-labelledby="current-assignments">
          <h2 className="assignment-bay-head" id="current-assignments">
            Current assignments
          </h2>
          <Grid gap="4" minItemWidth="control" fit="fill">
            {items.map((item) => (
              <AssignmentWorkPlate key={item.enrollment_id} item={item} />
            ))}
          </Grid>
        </section>
      </div>
    </OperateArea>
  );
}
