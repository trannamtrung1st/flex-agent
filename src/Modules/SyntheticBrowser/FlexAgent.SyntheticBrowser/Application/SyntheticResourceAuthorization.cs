using FlexAgent.SyntheticBrowser.Domain;

namespace FlexAgent.SyntheticBrowser.Application;

internal static class SyntheticResourceAuthorization
{
    internal static bool CanAccessSessionResource(SyntheticSessionRecord session, SyntheticScenarioState state)
    {
        if (IsAccessRevoked(session, state))
        {
            return false;
        }

        if (!state.AttemptStarted)
        {
            return false;
        }

        return session.ActorStage is SyntheticActorStages.Participant or SyntheticActorStages.Administrator;
    }

    internal static bool CanReadReviewCase(SyntheticSessionRecord session, SyntheticScenarioState state, string caseId)
    {
        if (!HasCapability(session, "reviewer") || IsAccessRevoked(session, state))
        {
            return false;
        }

        if (!string.Equals(caseId, SyntheticCommandAuthorization.SyntheticReviewCaseId, StringComparison.Ordinal))
        {
            return false;
        }

        if (state.SessionLifecycle is not ("completed" or "terminated"))
        {
            return false;
        }

        return state.ReviewLifecycle is
            "ready_for_review" or
            "approved" or
            "rejected" or
            "escalated";
    }

    internal static bool CanReadReleaseDetail(SyntheticSessionRecord session, SyntheticScenarioState state, string releaseId)
    {
        if (!HasCapability(session, "release") || IsAccessRevoked(session, state))
        {
            return false;
        }

        if (!string.Equals(releaseId, SyntheticCommandAuthorization.SyntheticReleaseId, StringComparison.Ordinal))
        {
            return false;
        }

        return state.ReviewLifecycle == "approved";
    }

    internal static bool CanReadResultDetail(SyntheticSessionRecord session, SyntheticScenarioState state, string resultId)
    {
        if (!HasCapability(session, "participant") || IsAccessRevoked(session, state) || !state.EnrollmentCreated)
        {
            return false;
        }

        return string.Equals(resultId, SyntheticCommandAuthorization.SyntheticResultId, StringComparison.Ordinal);
    }

    internal static bool IsAccessRevoked(SyntheticSessionRecord session, SyntheticScenarioState state) =>
        session.ScenarioId == SyntheticScenarioIds.DeniedAccess || state.PermissionRevoked;

    private static bool HasCapability(SyntheticSessionRecord session, string capability) =>
        SyntheticCommandAuthorization.HasCapability(session, capability);
}
