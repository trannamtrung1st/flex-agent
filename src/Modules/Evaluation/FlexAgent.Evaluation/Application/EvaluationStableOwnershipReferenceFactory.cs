using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationStableOwnershipReferenceFactory
{
    public static EvaluationStableOwnershipReference From(
        EvaluationOwnership ownership,
        Guid evaluationId) =>
        new(
            StableOrganizationId(ownership.OrganizationId),
            StableActivityId(ownership.ActivityId),
            StableParticipantId(ownership.ParticipantId),
            StableAttemptId(ownership.AttemptId),
            StableSessionId(ownership.SessionId),
            StableEvaluationId(evaluationId));

    public static SessionOwnershipRefV1 ToSessionOwnershipRef(EvaluationOwnership ownership) =>
        new(
            StableOrganizationId(ownership.OrganizationId),
            StableActivityId(ownership.ActivityId),
            StableParticipantId(ownership.ParticipantId),
            StableAttemptId(ownership.AttemptId),
            StableSessionId(ownership.SessionId));

    public static bool SessionOwnershipRefMatches(
        SessionOwnershipRefV1 ownership,
        EvaluationOwnership scope) =>
        string.Equals(ownership.OrganizationId, StableOrganizationId(scope.OrganizationId), StringComparison.Ordinal)
        && string.Equals(ownership.ActivityId, StableActivityId(scope.ActivityId), StringComparison.Ordinal)
        && string.Equals(ownership.ParticipantId, StableParticipantId(scope.ParticipantId), StringComparison.Ordinal)
        && string.Equals(ownership.AttemptId, StableAttemptId(scope.AttemptId), StringComparison.Ordinal)
        && string.Equals(ownership.SessionId, StableSessionId(scope.SessionId), StringComparison.Ordinal);

    public static string StableRequestId(Guid requestId) =>
        requestId == Guid.Empty
            ? throw new ArgumentException("Request id must not be empty.", nameof(requestId))
            : $"ereq.{requestId:N}";

    public static string StableInvocationAttemptId(Guid invocationAttemptId) =>
        invocationAttemptId == Guid.Empty
            ? throw new ArgumentException("Invocation attempt id must not be empty.", nameof(invocationAttemptId))
            : $"eatt.{invocationAttemptId:N}";

    public static string StableDeterministicInvocationId(Guid deterministicAttemptId) =>
        deterministicAttemptId == Guid.Empty
            ? throw new ArgumentException("Deterministic attempt id must not be empty.", nameof(deterministicAttemptId))
            : $"dinv.{deterministicAttemptId:N}";

    public static string StableOrganizationId(Guid organizationId) =>
        $"org.{organizationId:N}";

    public static string StableActivityId(Guid activityId) =>
        $"act.{activityId:N}";

    public static string StableParticipantId(Guid participantId) =>
        $"part.{participantId:N}";

    public static string StableAttemptId(Guid attemptId) =>
        $"att.{attemptId:N}";

    public static string StableSessionId(Guid sessionId) =>
        $"sess.{sessionId:N}";

    public static string StableEvaluationId(Guid evaluationId) =>
        $"eval.{evaluationId:N}";
}
