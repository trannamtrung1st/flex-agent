using FlexAgent.Contracts.Browser;
using FlexAgent.Contracts.Transport;
using FlexAgent.SyntheticBrowser.Domain;

namespace FlexAgent.SyntheticBrowser.Application;

public interface ISyntheticBrowserService
{
    bool IsEnabled { get; }

    bool IsHarnessAuthorized(string? harnessApiKey);

    ScenarioGrantResponseV1 CreateScenarioGrant(ScenarioGrantRequestV1 request);

    bool RevokeScenarioAccess(ScenarioInstanceRevokeRequestV1 request);

    ScenarioGrantExchangeResultV1? ExchangeGrant(string grantToken);

    SyntheticSessionRecord? ResolveSession(string sessionId);

    ActorContextV1? GetActorContext(SyntheticSessionRecord session);

    NavigationProjectionV1 GetNavigation(SyntheticSessionRecord session);

    HomeProjectionV1 GetHome(SyntheticSessionRecord session);

    ActivitiesListProjectionV1 GetActivities(SyntheticSessionRecord session);

    ActivityDetailProjectionV1? GetActivityDetail(SyntheticSessionRecord session, string activityId);

    EnrollmentProjectionV1? GetEnrollment(SyntheticSessionRecord session, string activityId);

    AssignmentProjectionV1? GetMyWorkAssignment(SyntheticSessionRecord session, string? enrollmentId);

    SessionProjectionV1? GetSession(SyntheticSessionRecord session, string sessionId);

    ReviewWorkProjectionV1 GetReviewWork(SyntheticSessionRecord session);

    ReviewCaseDetailProjectionV1? GetReviewCase(SyntheticSessionRecord session, string caseId);

    ReleaseWorkProjectionV1 GetReleaseWork(SyntheticSessionRecord session);

    ReleaseDetailProjectionV1? GetReleaseDetail(SyntheticSessionRecord session, string releaseId);

    ResultsProjectionV1 GetResults(SyntheticSessionRecord session);

    ResultDetailProjectionV1? GetResultDetail(SyntheticSessionRecord session, string resultId);

    GovernanceProjectionV1 GetGovernance(SyntheticSessionRecord session);

    PlannedTierProjectionV1 GetPlannedTier(SyntheticSessionRecord session, string moduleName);

    BrowserCommandResultV1 ExecuteCommand(SyntheticSessionRecord session, BrowserCommandEnvelopeV1 command);

    IEnumerable<SseSessionEventV1> GetSessionEvents(
        SyntheticSessionRecord session,
        string sessionId,
        string? lastEventId);
}
