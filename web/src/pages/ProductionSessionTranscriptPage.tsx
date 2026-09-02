import { useMemo } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useProductionApi } from "../api/production-api";
import { createProductionSessionClient } from "../api/production-session";
import { sessionKeys } from "../features/session/queryKeys";
import {
  Alert,
  OperateArea,
  ReadoutList,
  WorkWell,
  WorkWellSection,
} from "../design-system";

export function ProductionSessionTranscriptPage() {
  const { sessionId = "" } = useParams();
  const { apiState, fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionSessionClient(fetchJson), [fetchJson]);
  const snapshotQuery = useQuery({
    queryKey: sessionKeys.snapshot(sessionId),
    queryFn: () => client.getSnapshot(sessionId),
    enabled: apiState === "ready" && sessionId.length > 0,
  });
  const snapshot = snapshotQuery.data;

  if (snapshotQuery.isError) {
    return (
      <Alert variant="danger" title="Transcript unavailable">
        This historical transcript cannot be opened with the current assignment.
      </Alert>
    );
  }

  return (
    <OperateArea
      bay="record"
      label="Session transcript"
      title="Session transcript"
      context={snapshot ? `Lifecycle ${snapshot.lifecycle_state}` : "Loading"}
      frame="record"
    >
      <WorkWell live={false} label="Terminal transcript">
        <WorkWellSection>
          <ReadoutList
            rows={[
              { term: "Lifecycle", value: snapshot?.lifecycle_state ?? "Loading", emphasis: "inline" },
              { term: "Cutoff", value: snapshot?.cutoff_sequence ?? "—" },
            ]}
          />
          <ol aria-label="Historical transcript">
            {(snapshot?.transcript?.items ?? []).map((item) => (
              <li key={item.item_id}>
                <strong>{item.author}</strong>
                {" "}
                {item.status === "unavailable" ? "Content unavailable." : item.content}
              </li>
            ))}
          </ol>
        </WorkWellSection>
      </WorkWell>
    </OperateArea>
  );
}
