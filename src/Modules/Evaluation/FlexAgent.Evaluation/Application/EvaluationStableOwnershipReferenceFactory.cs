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
