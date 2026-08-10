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

    internal static BrowserCommandResultV1? Authorize(
        SyntheticSessionRecord session,
        BrowserCommandEnvelopeV1 command,
        SyntheticScenarioState state)
    {
        return command.CommandType switch
        {
            "activity.save_draft" => AuthorizeActivityAdmin(session, command, state, requiredLifecycle: "draft"),
            "activity.activate_cohort" => AuthorizeActivityAdmin(session, command, state, requiredLifecycle: "draft", requireSavedDraft: true),
            "enrollment.assign" => AuthorizeEnrollmentAssign(session, command, state),
            "submission.submit_text" => AuthorizeParticipantSubmission(session, command, state),
            "attempt.start" => AuthorizeAttemptStart(session, command, state),
            "session.send_message" => AuthorizeSessionCommand(session, command, state, requireActive: true),
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

        if (!ResourceMatches(command.ResourceId, SyntheticActivityId))
        {
            return Denied("Activity resource is not authorized.");
        }

        if (!string.Equals(state.ActivityLifecycle, requiredLifecycle, StringComparison.Ordinal))
        {
            return Denied("Activity is not in the required lifecycle state.");
        }

        if (requireSavedDraft && state.ActivityVersion < 2)
        {
            return Denied("Draft must be saved before activation.");
        }

        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != state.ActivityVersion)
        {
            return Conflict("Revision is stale. Refresh and retry.");
        }

        return null;
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

        if (!ResourceMatches(command.ResourceId, SyntheticActivityId, SyntheticEnrollmentId))
        {
            return Denied("Enrollment resource is not authorized.");
        }

        if (!string.Equals(state.ActivityLifecycle, "activated", StringComparison.Ordinal))
        {
            return Denied("Cohort must be activated before assigning participants.");
        }

        if (state.EnrollmentCreated)
        {
            return Denied("An active enrollment already exists.");
        }

        return null;
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

        if (!ResourceMatches(command.ResourceId, SyntheticEnrollmentId))
        {
            return Denied("Enrollment resource is not authorized.");
        }

        if (!state.EnrollmentCreated)
        {
            return Denied("No active enrollment is available.");
        }

        return null;
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

        if (!ResourceMatches(command.ResourceId, SyntheticEnrollmentId))
        {
            return Denied("Enrollment resource is not authorized.");
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
        bool requireActiveOrPaused = false)
    {
        if (!CanAccessSessionResource(session, state))
        {
            return Denied("Session access is not permitted for this actor.");
        }

        if (!ResourceMatches(command.ResourceId, SyntheticSessionId))
        {
            return Denied("Session resource is not authorized.");
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

        if (command.ExpectedVersion.HasValue && command.ExpectedVersion.Value != state.SessionVersion)
        {
            return Conflict("Session version is stale. Refresh and retry.");
        }

        return null;
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

        if (!ResourceMatches(command.ResourceId, SyntheticReviewCaseId))
        {
            return Denied("Review case is not authorized.");
        }

        if (state.SessionLifecycle is not ("completed" or "terminated"))
        {
            return Denied("Session handoff is not complete.");
        }

        if (state.ReviewLifecycle != "ready_for_review")
        {
            return Denied("Review case is not ready for decision.");
        }

        return null;
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

        if (!ResourceMatches(command.ResourceId, SyntheticReleaseId))
        {
            return Denied("Release resource is not authorized.");
        }

        if (state.ReviewLifecycle != "approved")
        {
            return Denied("Approved review decision is required before release.");
        }

        if (state.ReleaseLifecycle == "released")
        {
            return Denied("Result has already been released.");
        }

        return null;
    }

    internal static bool CanAccessSessionResource(SyntheticSessionRecord session, SyntheticScenarioState state)
    {
        if (!state.AttemptStarted)
        {
            return false;
        }

        return session.ActorStage is SyntheticActorStages.Participant or SyntheticActorStages.Administrator;
    }

    private static bool HasCapability(SyntheticSessionRecord session, string capability) =>
        ResolveCapabilities(session.ActorStage).Contains(capability);

    private static IReadOnlyList<string> ResolveCapabilities(string actorStage) => actorStage switch
    {
        SyntheticActorStages.Administrator => ["activity_admin", "governance", "session_control"],
        SyntheticActorStages.Participant => ["participant"],
        SyntheticActorStages.Reviewer => ["reviewer"],
        SyntheticActorStages.ReleaseActor => ["release"],
        _ => [],
    };

    private static bool ResourceMatches(string? resourceId, params string[] allowed)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return true;
        }

        return allowed.Any(id => string.Equals(resourceId, id, StringComparison.Ordinal));
    }

    private static BrowserCommandResultV1 Denied(string message) =>
        new(BrowserSchemaVersion.V1, "denied", Guid.NewGuid().ToString("N"), null, null, "contact_administrator", message);

    private static BrowserCommandResultV1 Conflict(string message) =>
        new(BrowserSchemaVersion.V1, "conflict", Guid.NewGuid().ToString("N"), null, null, "reconcile", message);
}
