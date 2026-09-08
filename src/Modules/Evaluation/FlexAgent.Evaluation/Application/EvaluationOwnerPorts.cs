using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public interface IEvaluationHandoffSource
{
    Task<EvaluationHandoffSnapshot?> GetCompletedHandoffAsync(
        Guid organizationId,
        Guid sessionId,
        Guid handoffId,
        CancellationToken cancellationToken);
}

public sealed record EvaluationHandoffSnapshot(
    Guid HandoffId,
    EvaluationOwnership Ownership,
    string TerminalState,
    string ManifestSealProcedureId,
    string ConfigurationDigest,
    string ManifestDigest);

public interface IProtectedEvaluationProcedureSource
{
    Task<ProtectedCanonicalUtf8?> GetCanonicalUtf8Async(
        Guid organizationId,
        Guid sourceId,
        Guid sourceVersionId,
        string expectedDigest,
        CancellationToken cancellationToken);
}

public sealed record ProtectedCanonicalUtf8(ReadOnlyMemory<byte> Utf8, string ContentDigest);
