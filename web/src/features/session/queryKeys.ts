export const sessionKeys = {
  all: ["session"] as const,
  snapshot: (sessionId: string) => ["session", "v1", "snapshot", sessionId] as const,
};
