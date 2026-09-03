import { useMemo } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useProductionApi } from "../api/production-api";
import { createProductionSessionClient } from "../api/production-session";
import { sessionKeys } from "../features/session/queryKeys";
import { SessionTranscriptLedger } from "../components/work/SessionTranscriptLedger";
import {
  Alert,
  EmptyPlate,
  OperateArea,
  ReadoutList,
  WorkWell,
  WorkWellHead,
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
      framed={false}
      label="Session transcript"
      title="Session transcript"
      context={snapshot ? `Lifecycle ${snapshot.lifecycle_state}` : "Loading"}
    >
      <WorkWell
        seat="stack"
        live={false}
        label="Terminal transcript"
        head={<WorkWellHead title="Terminal transcript" />}
      >
        <WorkWellSection>
          <ReadoutList
            rows={[
              { term: "Lifecycle", value: snapshot?.lifecycle_state ?? "Loading", emphasis: "inline" },
              { term: "Cutoff", value: snapshot?.cutoff_sequence ?? "—" },
            ]}
          />
        </WorkWellSection>
        {(snapshot?.transcript?.items ?? []).length > 0 ? (
          <SessionTranscriptLedger
            label="Historical transcript"
            items={snapshot?.transcript?.items ?? []}
          />
        ) : (
          <EmptyPlate
            label="No transcript items"
            note="This Session has no inspectable turns on the current assignment."
          />
        )}
      </WorkWell>
    </OperateArea>
  );
}
