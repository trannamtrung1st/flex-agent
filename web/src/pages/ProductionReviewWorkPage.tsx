import { useMemo } from "react";
import {
  Alert,
  CeremonyArea,
  CeremonyUnavailable,
  CeremonyWait,
  DataTableToolbar,
  DatatableCell,
  DatatableId,
  DatatableRow,
  DatatableStateReadout,
  DatatableTable,
  InstantReadout,
  Key,
  registryTableHug,
  StaticHeader,
  ToolbarReadout,
} from "../design-system";
import { useProductionApi } from "../api/production-api";
import { createProductionReviewClient, isReviewAccessLoss } from "../api/production-review";
import { ReviewerQueueEmpty, ReviewerQueueTableShell } from "../components/work/ReviewerQueueTable";
import { ReviewerQueueOperateArea } from "../components/work/ReviewerQueueOperateArea";
import { useReviewWorkInfiniteQuery } from "../features/review/queries";
import {
  EMPTY_REVIEW_WORK,
  INTERNAL_EVALUATION_NOTICE,
  LOADING_REVIEW_WORK,
  REVIEW_WORK_DESCRIPTION,
  REVIEW_WORK_TITLE,
  evaluationProcessingCopy,
  integrityCopy,
  nextActionCopy,
  processingIndicatorVariant,
  workRowTitle,
} from "../features/review/presentation";

export function ProductionReviewWorkPage() {
  const { fetchJson, shell } = useProductionApi();
  const client = useMemo(() => createProductionReviewClient(fetchJson), [fetchJson]);
  const scope = shell
    ? { actorId: shell.actor_id, organizationId: shell.organization_id }
    : null;
  const workQuery = useReviewWorkInfiniteQuery(client, scope);
  const items = useMemo(
    () => workQuery.data?.pages.flatMap((page) => page.items) ?? [],
    [workQuery.data],
  );

  if (!workQuery.data && !workQuery.isError) {
    return (
      <CeremonyArea label={REVIEW_WORK_TITLE} title={REVIEW_WORK_TITLE} description={REVIEW_WORK_DESCRIPTION}>
        <CeremonyWait label={`${LOADING_REVIEW_WORK}…`} />
      </CeremonyArea>
    );
  }

  if (isReviewAccessLoss(workQuery.error)) {
    return (
      <CeremonyUnavailable
        title="Your access changed"
        note="Protected Review work was removed. Return to Home or sign in again."
        danger
        recovery={{ label: "Return to Home", to: "/" }}
      />
    );
  }

  if (workQuery.error && items.length === 0) {
    const note = workQuery.error instanceof Error ? workQuery.error.message : "Request failed";
    return (
      <CeremonyUnavailable
        title={REVIEW_WORK_TITLE}
        description={REVIEW_WORK_DESCRIPTION}
        note={note}
        danger
        recovery={{ label: "Retry", onClick: () => { void workQuery.refetch(); } }}
      />
    );
  }

  const hasMore = workQuery.hasNextPage ?? false;

  return (
    <ReviewerQueueOperateArea
      hug={registryTableHug(items.length)}
      label={REVIEW_WORK_TITLE}
      title={REVIEW_WORK_TITLE}
      description={REVIEW_WORK_DESCRIPTION}
      advisory={{
        label: "Boundary",
        copy: INTERNAL_EVALUATION_NOTICE,
      }}
      context={workQuery.error instanceof Error ? (
        <Alert variant="danger" title="Could not refresh Review work">
          {workQuery.error.message}
          <Key size="compact" onClick={() => void workQuery.refetch()}>Retry</Key>
        </Alert>
      ) : undefined}
    >
      <ReviewerQueueTableShell
        toolbar={(
          <DataTableToolbar
            ariaLabel="Review work registry controls"
            readout={(
              <ToolbarReadout
                label="Showing"
                value={`${items.length} assigned case${items.length === 1 ? "" : "s"}`}
                valueId="reviewWorkCountValue"
              />
            )}
          />
        )}
        scrollProps={{ tabIndex: 0, "aria-label": "Review work rows, scrollable" }}
        table={(
          <DatatableTable caption={REVIEW_WORK_TITLE} hidden={items.length === 0}>
            <thead>
              <tr>
                <StaticHeader label="Participant" colMin="id" />
                <StaticHeader label="Assignment" colMin="title" />
                <StaticHeader label="Evaluation" colMin="state" />
                <StaticHeader label="Integrity" colMin="state" />
                <StaticHeader label="Updated" colMin="instant" />
              </tr>
            </thead>
            <tbody>
              {items.map((item) => {
                const title = workRowTitle(item);
                const action = nextActionCopy(item.next_action);
                return (
                  <DatatableRow key={item.review_case_id}>
                    <DatatableCell kind="id" colMin="id">
                      {item.participant_label ?? "Participant"}
                    </DatatableCell>
                    <DatatableCell kind="content" colMin="title">
                      <DatatableId to={`/review/${item.review_case_id}`}>
                        {action ? `${title} · ${action}` : title}
                      </DatatableId>
                    </DatatableCell>
                    <DatatableCell kind="state" colMin="state">
                      <DatatableStateReadout
                        variant={processingIndicatorVariant(item.evaluation_processing_state)}
                        solid={item.evaluation_processing_state === "completed"}
                        label={evaluationProcessingCopy(item.evaluation_processing_state)}
                      />
                    </DatatableCell>
                    <DatatableCell kind="state" colMin="state">
                      {integrityCopy(item.integrity_state)}
                    </DatatableCell>
                    <DatatableCell kind="content" colMin="instant">
                      <InstantReadout value={item.updated_at} timeZone={item.time_zone_id} />
                    </DatatableCell>
                  </DatatableRow>
                );
              })}
            </tbody>
          </DatatableTable>
        )}
        empty={items.length === 0 ? (
          <ReviewerQueueEmpty
            id="reviewWorkEmpty"
            inset
            label={EMPTY_REVIEW_WORK}
            note="Assigned Review cases appear here when they are assigned to you."
          />
        ) : undefined}
      />
      {hasMore ? (
        <Key
          size="compact"
          disabled={workQuery.isFetchingNextPage}
          onClick={() => { void workQuery.fetchNextPage(); }}
        >
          Load more assigned Review work
        </Key>
      ) : null}
    </ReviewerQueueOperateArea>
  );
}
