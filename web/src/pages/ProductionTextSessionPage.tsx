import { useCallback, useEffect, useMemo, useReducer, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useProductionApi } from "../api/production-api";
import {
  createProductionSessionClient,
  createSessionCommandId,
  createSessionIdempotencyKey,
} from "../api/production-session";
import { TextSessionStation } from "../components/work/TextSessionStation";
import { sessionKeys } from "../features/session/queryKeys";
import { emptySessionLiveView, sessionLiveReducer } from "../features/session/session-view";
import type { SessionCommandEnvelopeV1, SessionHostedEventEnvelopeV1, SessionSnapshotV1 } from "../contracts/v1";
import {
  Alert,
  CeremonyDialog,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  FieldTextarea,
  FormField,
  Key,
  KeyGroup,
  ReadoutList,
  WorkWell,
  WorkWellSection,
} from "../design-system";

function can(snapshot: SessionSnapshotV1 | null, action: SessionSnapshotV1["permitted_actions"][number]) {
  return snapshot?.permitted_actions.includes(action) ?? false;
}

export function ProductionTextSessionPage() {
  const { sessionId = "" } = useParams();
  const { apiState, fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionSessionClient(fetchJson), [fetchJson]);
  const [view, dispatch] = useReducer(sessionLiveReducer, emptySessionLiveView);
  const [confirmComplete, setConfirmComplete] = useState(false);
  const [pendingIdempotency, setPendingIdempotency] = useState<string | null>(null);

  const snapshotQuery = useQuery({
    queryKey: sessionKeys.snapshot(sessionId),
    queryFn: () => client.getSnapshot(sessionId),
    enabled: apiState === "ready" && sessionId.length > 0,
  });

  useEffect(() => {
    if (snapshotQuery.data) {
      dispatch({ type: "snapshot", snapshot: snapshotQuery.data });
    }
  }, [snapshotQuery.data]);

  useEffect(() => {
    if (apiState !== "ready" || !sessionId) {
      return;
    }

    const source = new EventSource(`/v1/sessions/${sessionId}/events`);
    dispatch({ type: "connection", connection: "connecting" });
    source.onopen = () => dispatch({ type: "connection", connection: "connected" });
    source.onerror = () => dispatch({ type: "connection", connection: "reconnecting" });
    source.onmessage = (message: MessageEvent<string>) => {
      try {
        const event = JSON.parse(message.data) as SessionHostedEventEnvelopeV1;
        dispatch({ type: "event", event });
      } catch {
        dispatch({ type: "error", message: "A Session update could not be applied. Refresh from the server." });
      }
    };
    return () => source.close();
  }, [apiState, sessionId]);

  const submit = useCallback(async (command: SessionCommandEnvelopeV1) => {
    dispatch({ type: "send", sendState: "pending" });
    try {
      const outcome = await client.submitCommand(sessionId, command);
      if (outcome.outcome_category === "uncertain") {
        dispatch({ type: "send", sendState: "uncertain" });
        dispatch({ type: "error", message: "The command was not confirmed. Reconcile before sending again." });
        return;
      }
      if (!outcome.succeeded && outcome.outcome_category !== "duplicate") {
        dispatch({ type: "send", sendState: "idle" });
        dispatch({ type: "error", message: "The Session could not accept that command." });
        await snapshotQuery.refetch();
        return;
      }
      dispatch({ type: "send", sendState: "idle" });
      if (command.command_type === "session.message.send.v1") {
        dispatch({ type: "draft", draft: "" });
        setPendingIdempotency(null);
      }
      await snapshotQuery.refetch();
    } catch {
      dispatch({ type: "send", sendState: "uncertain" });
      dispatch({ type: "error", message: "The command outcome is uncertain. Reconcile before sending again." });
    }
  }, [client, sessionId, snapshotQuery]);

  const snapshot = view.snapshot;
  const terminal = snapshot?.lifecycle_state === "completed"
    || snapshot?.lifecycle_state === "terminated"
    || snapshot?.lifecycle_state === "aborted";

  const paused = snapshot?.lifecycle_state === "paused";
  const composerClosed = terminal || paused || !can(snapshot, "send_message");

  if (snapshotQuery.isError && !snapshot) {
    return (
      <Alert variant="danger" title="Session unavailable">
        This Session cannot be opened with the current access.
      </Alert>
    );
  }

  if (snapshotQuery.isLoading && !snapshot) {
    return (
      <Alert variant="info" title="Loading Session">
        Restoring the authoritative Session snapshot.
      </Alert>
    );
  }

  return (
    <TextSessionStation
      homeTo="/my-work"
      homeLabel="My work"
      railLabel="Session instruments"
      brandSuffix="Session"
      warned={snapshot?.timing?.warning_code === "imminent"}
      complete={terminal}
      instruments={(
        <ReadoutList
          label="Session status"
          rows={[
            { term: "Status", value: snapshot?.lifecycle_state ?? "Loading", emphasis: "title" },
            { term: "Connection", value: view.connection === "reconnecting" ? "Reconnecting" : view.connection },
            { term: "Work", value: snapshot?.activity?.work_state ?? "idle" },
          ]}
        />
      )}
      composer={composerClosed ? (
        <p>{paused ? "This Session is paused. Sending is closed until an administrator resumes it." : "Composer closed."}</p>
      ) : (
        <form
          onSubmit={(event) => {
            event.preventDefault();
            if (!snapshot || view.sendState !== "idle" || view.draft.trim().length === 0) {
              return;
            }
            const idempotency = pendingIdempotency ?? createSessionIdempotencyKey();
            setPendingIdempotency(idempotency);
            void submit({
              schema_version: "v1",
              command_type: "session.message.send.v1",
              command_id: createSessionCommandId(),
              idempotency_key: idempotency,
              session_locator: { session_id: sessionId },
              expected_session_version: snapshot.session_version,
              payload: { message_text: view.draft },
            });
          }}
        >
          <FormField id="session-message" label="Message" layout="stack">
            {(control) => (
              <FieldTextarea
                {...control}
                rows={3}
                resize="vertical"
                placeholder="Write your next message"
                value={view.draft}
                onChange={(event) => dispatch({ type: "draft", draft: event.target.value })}
                disabled={view.sendState !== "idle"}
              />
            )}
          </FormField>
          <KeyGroup>
            <Key type="submit" disabled={view.sendState !== "idle" || view.draft.trim().length === 0}>
              {view.sendState === "pending" ? "Sending" : "Send"}
            </Key>
            {can(snapshot, "complete_session") ? (
              <Key variant="quiet" type="button" onClick={() => setConfirmComplete(true)}>Complete</Key>
            ) : null}
            {view.sendState === "uncertain" ? (
              <Key
                variant="quiet"
                type="button"
                onClick={() => {
                  if (!snapshot) return;
                  void submit({
                    schema_version: "v1",
                    command_type: "session.reconcile.v1",
                    command_id: createSessionCommandId(),
                    idempotency_key: createSessionIdempotencyKey(),
                    session_locator: { session_id: sessionId },
                    expected_session_version: snapshot.session_version,
                    client_last_seen_sequence: snapshot.last_confirmed_sequence === "0"
                      ? "1"
                      : snapshot.last_confirmed_sequence,
                    payload: {},
                  });
                }}
              >
                Reconcile
              </Key>
            ) : null}
          </KeyGroup>
        </form>
      )}
      examiner={(
        <WorkWell live={false} label="Session facts">
          <WorkWellSection>
            <p>{snapshot?.agent?.display_name ?? "Assessment Agent"}</p>
            <p>{snapshot?.bound_submission?.summary ?? "Bound Submission"}</p>
            <p>{snapshot?.timing?.policy === "disabled" ? "Time is accounted on the server." : `${snapshot?.timing?.remaining_seconds ?? "—"} s remaining`}</p>
          </WorkWellSection>
        </WorkWell>
      )}
    >
      {view.connection === "reconnecting" || view.connection === "offline" ? (
        <Alert variant="info" title="Connection recovery">
          Reconnecting to the Session. Authoritative state is restored from the server snapshot, not from this browser clock.
        </Alert>
      ) : null}
      {snapshot?.activity?.work_state === "no_action" ? (
        <Alert variant="info" title="No further Agent output">
          The Agent recorded an explicit no-action for this turn. You may continue when sending is permitted.
        </Alert>
      ) : null}
      {view.lastError ? <Alert variant="danger" title="Session recovery">{view.lastError}</Alert> : null}
      <ol aria-label="Examination transcript">
        {(snapshot?.transcript?.items ?? []).map((item) => (
          <li key={item.item_id}>
            <strong>{item.author}</strong>
            {" "}
            {item.status === "unavailable" ? "Content unavailable." : item.content}
          </li>
        ))}
      </ol>
      {confirmComplete ? (
        <CeremonyDialog open onClose={() => setConfirmComplete(false)} labelledBy="complete-session-title">
          <DialogPlate>
            <DialogPlateHead title="Complete this Session?" titleId="complete-session-title" />
            <DialogPlateBody>
              <p>Completion is final. You cannot send further messages after the Session completes.</p>
            </DialogPlateBody>
            <DialogPlateFooter>
              <Key variant="quiet" onClick={() => setConfirmComplete(false)}>Cancel</Key>
              <Key
                onClick={() => {
                  if (!snapshot) return;
                  setConfirmComplete(false);
                  void submit({
                    schema_version: "v1",
                    command_type: "session.complete.v1",
                    command_id: createSessionCommandId(),
                    idempotency_key: createSessionIdempotencyKey(),
                    session_locator: { session_id: sessionId },
                    expected_session_version: snapshot.session_version,
                    payload: {},
                  });
                }}
              >
                Complete Session
              </Key>
            </DialogPlateFooter>
          </DialogPlate>
        </CeremonyDialog>
      ) : null}
    </TextSessionStation>
  );
}
