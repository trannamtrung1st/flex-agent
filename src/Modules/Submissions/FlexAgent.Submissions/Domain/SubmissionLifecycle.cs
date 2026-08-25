namespace FlexAgent.Submissions.Domain;

public static class SubmissionLifecycle
{
    public static bool IncompleteEligibleForCleanup(SubmissionIntakeRecord intake, DateTimeOffset nowUtc) =>
        !IntakeStateMachine.IsTerminal(intake.Status)
        && nowUtc - intake.CreatedAtUtc >= SubmissionLifecycleClocks.IncompleteRetention;

    public static bool RejectedBytesEligibleForCleanup(SubmissionIntakeRecord intake, DateTimeOffset nowUtc) =>
        intake.Status is IntakeStates.Cancelled or IntakeStates.Rejected or IntakeStates.Failed
        && nowUtc - intake.UpdatedAtUtc >= SubmissionLifecycleClocks.RejectedByteRetention;

    public static bool MayDeleteArtifact(bool acceptedReferenceExists, bool legalHoldActive) =>
        !acceptedReferenceExists && !legalHoldActive;

    public static bool AcceptedPayloadEligibleForCleanup(
        DateTimeOffset? activityClosedAtUtc,
        DateTimeOffset nowUtc,
        bool legalHoldActive) =>
        activityClosedAtUtc is DateTimeOffset closedAt
        && !legalHoldActive
        && nowUtc - closedAt >= SubmissionLifecycleClocks.AcceptedRetentionAfterActivityClosure;

    public static bool MayDeleteAcceptedPayload(bool legalHoldActive) => !legalHoldActive;

    public static IReadOnlyList<string> PermittedActions(
        bool intakeAvailable,
        string? intakeStatus,
        bool hasAcceptedVersions)
    {
        var actions = new List<string> { SubmissionPermittedActions.ReturnToMyWork };
        if (!intakeAvailable)
        {
            if (hasAcceptedVersions)
            {
                actions.Add(SubmissionPermittedActions.PreviewItem);
                actions.Add(SubmissionPermittedActions.DownloadItem);
            }

            return actions;
        }

        if (intakeStatus is null)
        {
            actions.Add(SubmissionPermittedActions.BeginIntake);
        }
        else if (intakeStatus is IntakeStates.Receiving or IntakeStates.Received)
        {
            actions.Add(SubmissionPermittedActions.CompleteItem);
            actions.Add(SubmissionPermittedActions.CancelIntake);
            if (intakeStatus == IntakeStates.Received)
            {
                actions.Add(SubmissionPermittedActions.FinalizeIntake);
            }
        }
        else if (intakeStatus == IntakeStates.Validating)
        {
            actions.Add(SubmissionPermittedActions.CancelIntake);
        }
        else if (intakeStatus is IntakeStates.Cancelled or IntakeStates.Rejected or IntakeStates.Failed or IntakeStates.Accepted)
        {
            actions.Add(SubmissionPermittedActions.BeginIntake);
        }

        if (hasAcceptedVersions)
        {
            actions.Add(SubmissionPermittedActions.PreviewItem);
            actions.Add(SubmissionPermittedActions.DownloadItem);
        }

        return actions.Distinct(StringComparer.Ordinal).ToArray();
    }
}
