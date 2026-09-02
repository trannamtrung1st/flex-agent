import type {
  SessionCommandEnvelopeV1,
  SessionCommandOutcomeV1,
  SessionSnapshotV1,
} from "../contracts/v1";

export function createSessionCommandId(): string {
  return `cmd.${crypto.randomUUID().replaceAll("-", "").slice(0, 24)}`;
}

export function createSessionIdempotencyKey(): string {
  return `idem-${crypto.randomUUID().replaceAll("-", "").slice(0, 20)}`;
}

export function createProductionSessionClient(fetchJson: <T>(path: string, init?: RequestInit) => Promise<T>) {
  return {
    getSnapshot(sessionId: string) {
      return fetchJson<SessionSnapshotV1>(`/v1/sessions/${sessionId}`);
    },
    submitCommand(sessionId: string, command: SessionCommandEnvelopeV1) {
      return fetchJson<SessionCommandOutcomeV1>(`/v1/sessions/${sessionId}/commands`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(command),
      });
    },
  };
}
