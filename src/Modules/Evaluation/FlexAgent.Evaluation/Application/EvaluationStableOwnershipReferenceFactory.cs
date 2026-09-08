using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationStableOwnershipReferenceFactory
{
    public static EvaluationStableOwnershipReference From(
        EvaluationOwnership ownership,
        Guid evaluationId) =>
        new(
            ownership.OrganizationId.ToString("D"),
            ownership.ActivityId.ToString("D"),
            ownership.ParticipantId.ToString("D"),
            ownership.AttemptId.ToString("D"),
            ownership.SessionId.ToString("D"),
            evaluationId.ToString("D"));
}
