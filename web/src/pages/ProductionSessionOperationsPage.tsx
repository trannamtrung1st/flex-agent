import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useProductionApi } from "../api/production-api";
import {
  createProductionSessionClient,
  createSessionCommandId,
  createSessionIdempotencyKey,
} from "../api/production-session";
import { sessionKeys } from "../features/session/queryKeys";
import type { SessionCommandEnvelopeV1 } from "../contracts/v1";
import {
  Alert,
  CeremonyDialog,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  Key,
  KeyGroup,
  OperateArea,
  ReadoutList,
  WorkWell,
  WorkWellSection,
} from "../design-system";

export function ProductionSessionOperationsPage() {
  const { sessionId = "" } = useParams();
  const { apiState, fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionSessionClient(fetchJson), [fetchJson]);
  const [pending, setPending] = useState<"pause" | "resume" | "terminate" | null>(null);
  const [error, setError] = useState<string | null>(null);

  const snapshotQuery = useQuery({
    queryKey: sessionKeys.snapshot(sessionId),
    queryFn: () => client.getSnapshot(sessionId),
    enabled: apiState === "ready" && sessionId.length > 0,
  });
  const snapshot = snapshotQuery.data;

  async function submit(command: SessionCommandEnvelopeV1) {
    setError(null);
    try {
      const outcome = await client.submitCommand(sessionId, command);
      if (!outcome.succeeded && outcome.outcome_category !== "duplicate") {
        setError("The control command was not accepted.");
      }
      await snapshotQuery.refetch();
    } catch {
      setError("The control command outcome is uncertain. Reload operational state before retrying.");
    }
  }

  if (snapshotQuery.isError) {
    return (
      <Alert variant="danger" title="Session operations unavailable">
        Operational control is not available for this Session.
      </Alert>
    );
  }

  return (
    <OperateArea
      bay="record"
      label="Session operations"
      title="Session operations"
      context={snapshot ? `Lifecycle ${snapshot.lifecycle_state}` : "Loading operational state"}
      frame="record"
    >
      {error ? <Alert variant="danger" title="Control not confirmed">{error}</Alert> : null}
      <WorkWell live={false} label="Operational state">
        <WorkWellSection>
          <ReadoutList
            rows={[
              { term: "Lifecycle", value: snapshot?.lifecycle_state ?? "Loading", emphasis: "inline" },
              { term: "Version", value: snapshot ? String(snapshot.session_version) : "—" },
            ]}
          />
          <p>Transcript and Submission content are not loaded on this route.</p>
        </WorkWellSection>
      </WorkWell>
      <KeyGroup>
        {snapshot?.permitted_actions.includes("pause_session") ? (
          <Key onClick={() => setPending("pause")}>Pause</Key>
        ) : null}
        {snapshot?.permitted_actions.includes("resume_session") ? (
          <Key onClick={() => setPending("resume")}>Resume</Key>
        ) : null}
        {snapshot?.permitted_actions.includes("terminate_session") ? (
          <Key variant="quiet" onClick={() => setPending("terminate")}>Terminate</Key>
        ) : null}
      </KeyGroup>
      {pending ? (
        <CeremonyDialog open onClose={() => setPending(null)} labelledBy="session-ops-title">
          <DialogPlate>
            <DialogPlateHead
              title={pending === "terminate" ? "Terminate this Session?" : `${pending === "pause" ? "Pause" : "Resume"} this Session?`}
              titleId="session-ops-title"
            />
            <DialogPlateBody>
              <p>
                {pending === "terminate"
                  ? "Termination is final and records an administrator terminal reason."
                  : "This changes operational state only. It does not load transcript content."}
              </p>
            </DialogPlateBody>
            <DialogPlateFooter>
              <Key variant="quiet" onClick={() => setPending(null)}>Cancel</Key>
              <Key
                onClick={() => {
                  if (!snapshot) return;
                  const command: SessionCommandEnvelopeV1 = pending === "terminate"
                    ? {
                        schema_version: "v1",
                        command_type: "session.terminate.v1",
                        command_id: createSessionCommandId(),
                        idempotency_key: createSessionIdempotencyKey(),
                        session_locator: { session_id: sessionId },
                        expected_session_version: snapshot.session_version,
                        payload: { reason_code: "administrator_terminate" },
                      }
                    : pending === "pause"
                      ? {
                          schema_version: "v1",
                          command_type: "session.pause.v1",
                          command_id: createSessionCommandId(),
                          idempotency_key: createSessionIdempotencyKey(),
                          session_locator: { session_id: sessionId },
                          expected_session_version: snapshot.session_version,
                          payload: {},
                        }
                      : {
                          schema_version: "v1",
                          command_type: "session.resume.v1",
                          command_id: createSessionCommandId(),
                          idempotency_key: createSessionIdempotencyKey(),
                          session_locator: { session_id: sessionId },
                          expected_session_version: snapshot.session_version,
                          payload: {},
                        };
                  setPending(null);
                  void submit(command);
                }}
              >
                Confirm
              </Key>
            </DialogPlateFooter>
          </DialogPlate>
        </CeremonyDialog>
      ) : null}
    </OperateArea>
  );
}
