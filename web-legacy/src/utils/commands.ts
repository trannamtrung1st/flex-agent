export const ACTION_COMMAND_MAP: Record<string, string> = {
  save_draft: "activity.save_draft",
  activate_cohort: "activity.activate_cohort",
  assign_participant: "enrollment.assign",
  submit_text: "submission.submit_text",
  start_attempt: "attempt.start",
  send_message: "session.send_message",
  pause_session: "session.pause",
  resume_session: "session.resume",
  complete_session: "session.complete",
  approve: "review.approve",
  reject: "review.reject",
  escalate: "review.escalate",
  release_result: "release.confirm",
};

export function createIdempotencyKey(): string {
  return crypto.randomUUID();
}

export function mapActionToCommand(actionId: string): string | undefined {
  return ACTION_COMMAND_MAP[actionId];
}
