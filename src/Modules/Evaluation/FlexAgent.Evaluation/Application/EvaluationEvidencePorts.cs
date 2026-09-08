using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record EvaluationSessionEvidenceBundle(
    EvaluationHandoffSnapshot Handoff,
    IReadOnlyList<EvaluationSessionTranscriptMaterial> TranscriptItemsAtOrBeforeCutoff,
    EvaluationSafeFactProjection? ConfigurationFact = null,
    EvaluationSafeFactProjection? ManifestFact = null);

public interface IEvaluationSessionEvidenceSource
{
    Task<EvaluationSessionEvidenceBundle?> LoadAsync(
        Guid organizationId,
        Guid sessionId,
        string handoffId,
        CancellationToken cancellationToken);
}

public sealed record EvaluationSubmissionEvidenceBundle(
    IReadOnlyList<EvaluationSubmissionMaterial> BoundItems);

public interface IEvaluationSubmissionEvidenceSource
{
    Task<EvaluationSubmissionEvidenceBundle?> LoadBoundItemsAsync(
        Guid organizationId,
        Guid activityId,
        Guid participantId,
        Guid attemptId,
        Guid sessionId,
        CancellationToken cancellationToken);
}
