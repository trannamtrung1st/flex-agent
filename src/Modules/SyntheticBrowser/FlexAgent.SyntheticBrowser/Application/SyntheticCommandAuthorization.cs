using FlexAgent.Contracts.Browser;
using FlexAgent.SyntheticBrowser.Domain;

namespace FlexAgent.SyntheticBrowser.Application;

internal static class SyntheticCommandAuthorization
{
    internal const string SyntheticActivityId = "act.synthetic.campaign-001";
    internal const string SyntheticEnrollmentId = "enr.synthetic.001";
    internal const string SyntheticSessionId = "sess.synthetic.001";
    internal const string SyntheticReviewCaseId = "rev.synthetic.001";
    internal const string SyntheticReleaseId = "rel.synthetic.001";
    internal const string SyntheticResultId = "res.synthetic.001";
    internal const string SyntheticParticipantId = "part.synthetic.001";

    internal static BrowserCommandResultV1? Authorize(
        SyntheticSessionRecord session,
        BrowserCommandEnvelopeV1 command,
        SyntheticScenarioState state)
    {
        var envelopeFailure = SyntheticCommandValidation.ValidateEnvelope(command);
        if (envelopeFailure is not null)
        {
            return envelopeFailure;
        }

        return command.CommandType switch
        {
            "activity.save_draft" => AuthorizeActivityAdmin(session, command, state, requiredLifecycle: "draft"),
            "activity.activate_cohort" => AuthorizeActivityAdmin(session, command, state, requiredLifecycle: "draft", requireSavedDraft: true),
            "enrollment.assign" => AuthorizeEnrollmentAssign(session, command, state),
            "submission.submit_text" => AuthorizeParticipantSubmission(session, command, state),
            "attempt.start" => AuthorizeAttemptStart(session, command, state),
            "session.send_message" => AuthorizeSessionCommand(session, command, state, requireActive: true, requirePayloadKey: "message_text"),
            "session.pause" => AuthorizeSessionCommand(session, command, state, requireActive: true),
            "session.resume" => AuthorizeSessionCommand(session, command, state, requirePaused: true),
            "session.complete" => AuthorizeSessionCommand(session, command, state, requireActiveOrPaused: true),
            "review.approve" => AuthorizeReviewDecision(session, command, state),
            "review.reject" => AuthorizeReviewDecision(session, command, state),
            "review.escalate" => AuthorizeReviewDecision(session, command, state),
            "release.confirm" => AuthorizeReleaseConfirm(session, command, state),
            _ => Denied("Action is not permitted."),
        };
    }

    private static BrowserCommandResultV1? AuthorizeActivityAdmin(
        SyntheticSessionRecord session,
        BrowserCommandEnvelopeV1 command,
        SyntheticScenarioState state,
        string requiredLifecycle,
        bool requireSavedDraft = false)
    {
        if (!HasCapability(session, "activity_admin"))
        {
            return Denied("Activity administration is not permitted for this actor.");
        }

        if (!ResourceMatchesRequired(command.ResourceId, SyntheticActivityId))
        {
            return Denied("Activity resource is required and must be authorized.");
        }

        if (!string.Equals(state.ActivityLifecycle, requiredLifecycle, StringComparison.Ordinal))
        {
            return Denied("Activity is not in the required lifecycle state.");
        }

        if (requireSavedDraft && state.ActivityVersion < 2)
        {
            return Denied("Draft must be saved before activation.");
        }

        return RequireExpectedVersion(command.ExpectedVersion, state.ActivityVersion, "Activity");
    }

    private static BrowserCommandResultV1? AuthorizeEnrollmentAssign(
        SyntheticSessionRecord session,
        BrowserCommandEnvelopeV1 command,
        SyntheticScenarioState state)
    {
        if (!HasCapability(session, "activity_admin"))
        {
            return Denied("Enrollment assignment is not permitted for this actor.");
        }

        if (!ResourceMatchesRequired(command.ResourceId, SyntheticActivityId))
        {
            return Denied("Activity resource is required and must be authorized.");
        }

        if (!string.Equals(state.ActivityLifecycle, "activated", StringComparison.Ordinal))
        {
            return Denied("Cohort must be activated before assigning participants.");
        }

        if (state.EnrollmentCreated)
        {
            return Denied("An active enrollment already exists.");
        }

        var participantFailure = SyntheticCommandValidation.RequirePayloadValue(command, "participant_id");
        if (participantFailure is not null)
        {
            return participantFailure;
        }

        if (!string.Equals(command.Payload!["participant_id"], SyntheticParticipantId, StringComparison.Ordinal))
        {
            return Denied("Participant is not permitted for assignment.");
        }

        return RequireExpectedVersion(command.ExpectedVersion, state.ActivityVersion, "Activity");
    }

    private static BrowserCommandResultV1? AuthorizeParticipantSubmission(
        SyntheticSessionRecord session,
        BrowserCommandEnvelopeV1 command,
        SyntheticScenarioState state)
    {
        if (!HasCapability(session, "participant"))
        {
            return Denied("Submission is not permitted for this actor.");
        }

        if (!ResourceMatchesRequired(command.ResourceId, SyntheticEnrollmentId))
        {
            return Denied("Enrollment resource is required and must be authorized.");
        }

        if (!state.EnrollmentCreated)
        {
            return Denied("No active enrollment is available.");
        }

        if (state.SubmissionAccepted)
        {
            return Denied("A submission version is already accepted.");
        }

        return SyntheticCommandValidation.RequirePayloadValue(command, "submission_text");
    }

    private static BrowserCommandResultV1? AuthorizeAttemptStart(
        SyntheticSessionRecord session,
        BrowserCommandEnvelopeV1 command,
        SyntheticScenarioState state)
    {
        if (!HasCapability(session, "participant"))
        {
            return Denied("Attempt start is not permitted for this actor.");
        }

        if (!ResourceMatchesRequired(command.ResourceId, SyntheticEnrollmentId))
        {
            return Denied("Enrollment resource is required and must be authorized.");
        }

        if (!state.EnrollmentCreated || !state.SubmissionAccepted)
        {
            return Denied("Accepted submission is required before starting an attempt.");
        }

        if (state.AttemptStarted)
        {
            return Denied("Attempt entitlement has already been consumed.");
        }

        return null;
    }

    private static BrowserCommandResultV1? AuthorizeSessionCommand(
        SyntheticSessionRecord session,
        BrowserCommandEnvelopeV1 command,
        SyntheticScenarioState state,
        bool requireActive = false,
        bool requirePaused = false,
        bool requireActiveOrPaused = false,
        string? requirePayloadKey = null)
    {
        if (!SyntheticResourceAuthorization.CanAccessSessionResource(session, state))
        {
            return Denied("Session access is not permitted for this actor.");
        }

        if (!ResourceMatchesRequired(command.ResourceId, SyntheticSessionId))
        {
            return Denied("Session resource is required and must be authorized.");
        }

        if (requireActive && state.SessionLifecycle != "active")
        {
            return Denied("Session is not active.");
        }

        if (requirePaused && state.SessionLifecycle != "paused")
        {
            return Denied("Session is not paused.");
        }

        if (requireActiveOrPaused && state.SessionLifecycle is not ("active" or "paused"))
        {
            return Denied("Session cannot be completed from the current state.");
        }

        if (requirePayloadKey is not null)
        {
            var payloadFailure = SyntheticCommandValidation.RequirePayloadValue(command, requirePayloadKey);
            if (payloadFailure is not null)
            {
                return payloadFailure;
            }
        }

        return RequireExpectedVersion(command.ExpectedVersion, state.SessionVersion, "Session");
    }

    private static BrowserCommandResultV1? AuthorizeReviewDecision(
        SyntheticSessionRecord session,
        BrowserCommandEnvelopeV1 command,
        SyntheticScenarioState state)
    {
        if (!HasCapability(session, "reviewer"))
        {
            return Denied("Review decisions are not permitted for this actor.");
        }

        if (!ResourceMatchesRequired(command.ResourceId, SyntheticReviewCaseId))
        {
            return Denied("Review case resource is required and must be authorized.");
        }

        if (state.SessionLifecycle is not ("completed" or "terminated"))
        {
            return Denied("Session handoff is not complete.");
        }

        if (state.ReviewLifecycle != "ready_for_review")
        {
            return Denied("Review case is not ready for decision.");
        }

        return RequireExpectedVersion(command.ExpectedVersion, state.ReviewCaseVersion, "Review case");
    }

    private static BrowserCommandResultV1? AuthorizeReleaseConfirm(
        SyntheticSessionRecord session,
        BrowserCommandEnvelopeV1 command,
        SyntheticScenarioState state)
    {
        if (!HasCapability(session, "release"))
        {
            return Denied("Release is not permitted for this actor.");
        }

        if (!ResourceMatchesRequired(command.ResourceId, SyntheticReleaseId))
        {
            return Denied("Release resource is required and must be authorized.");
        }

        if (state.ReviewLifecycle != "approved")
        {
            return Denied("Approved review decision is required before release.");
        }

        if (state.ReleaseLifecycle == "released")
        {
            return Denied("Result has already been released.");
        }

        return RequireExpectedVersion(command.ExpectedVersion, state.ReleaseVersion, "Release");
    }

    internal static bool HasCapability(SyntheticSessionRecord session, string capability) =>
        ResolveCapabilities(session.ActorStage).Contains(capability);

    private static IReadOnlyList<string> ResolveCapabilities(string actorStage) => actorStage switch
    {
        SyntheticActorStages.Administrator => ["activity_admin", "governance", "session_control"],
        SyntheticActorStages.Participant => ["participant"],
        SyntheticActorStages.Reviewer => ["reviewer"],
        SyntheticActorStages.ReleaseActor => ["release"],
        _ => [],
    };

    private static bool ResourceMatchesRequired(string? resourceId, params string[] allowed) =>
        !string.IsNullOrWhiteSpace(resourceId) &&
        allowed.Any(id => string.Equals(resourceId, id, StringComparison.Ordinal));

    private static BrowserCommandResultV1? RequireExpectedVersion(int? expectedVersion, int currentVersion, string resourceLabel)
    {
        if (!expectedVersion.HasValue)
        {
            return Denied($"{resourceLabel} expected version is required.");
        }

        if (expectedVersion.Value != currentVersion)
        {
            return Conflict($"{resourceLabel} version is stale. Refresh and retry.");
        }

        return null;
    }

    private static BrowserCommandResultV1 Denied(string message) =>
        new(BrowserSchemaVersion.V1, "denied", Guid.NewGuid().ToString("N"), null, null, "contact_administrator", message);

    private static BrowserCommandResultV1 Conflict(string message) =>
        new(BrowserSchemaVersion.V1, "conflict", Guid.NewGuid().ToString("N"), null, null, "reconcile", message);
}
