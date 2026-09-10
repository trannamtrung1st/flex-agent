using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public interface IEvaluationHandoffSource
{
    Task<EvaluationHandoffSnapshot?> GetCompletedHandoffAsync(
        Guid organizationId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<EvaluationHandoffSnapshot?> GetCompletedHandoffAsync(
        Guid organizationId,
        Guid sessionId,
        string handoffId,
        CancellationToken cancellationToken);

    Task<EvaluationHandoffScanPage> ScanCompletedHandoffsAsync(
        EvaluationHandoffCursor? after,
        int limit,
        CancellationToken cancellationToken);
}

public sealed record EvaluationHandoffSnapshot(
    string HandoffId,
    EvaluationOwnership Ownership,
    string TerminalState,
    Guid TerminalRecordId,
    long CutoffSequence,
    string ManifestSealProcedureId,
    string TerminalSealDigest,
    Guid ConfigurationId,
    string ConfigurationDigest,
    Guid ManifestId,
    string ManifestDigest);

public sealed record EvaluationHandoffCursor(
    DateTimeOffset CommittedAt,
    Guid OrganizationId,
    Guid SessionId,
    string HandoffId);

public sealed record EvaluationHandoffScanPage(
    IReadOnlyList<EvaluationHandoffSnapshot> Items,
    EvaluationHandoffCursor? NextCursor);

public interface IEvaluationHandoffInbox
{
    Task<bool> RecordAsync(
        Guid deliveryId,
        EvaluationHandoffSnapshot handoff,
        CancellationToken cancellationToken);
}

public sealed class EvaluationHandoffDeliveryHandler(
    IEvaluationHandoffSource source,
    IEvaluationHandoffInbox inbox)
{
    public async Task<bool> HandleAsync(
        Guid deliveryId,
        Guid organizationId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var snapshot = await source.GetCompletedHandoffAsync(
            organizationId,
            sessionId,
            cancellationToken);
        return snapshot is not null
            && await inbox.RecordAsync(deliveryId, snapshot, cancellationToken);
    }

    public async Task<EvaluationHandoffCursor?> ReconcileAsync(
        EvaluationHandoffCursor? after,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        var page = await source.ScanCompletedHandoffsAsync(after, limit, cancellationToken);
        foreach (var handoff in page.Items)
        {
            await inbox.RecordAsync(Guid.CreateVersion7(), handoff, cancellationToken);
        }

        return page.NextCursor;
    }
}

public interface IProtectedEvaluationProcedureSource
{
    Task<ProtectedCanonicalUtf8?> GetCanonicalUtf8Async(
        Guid organizationId,
        Guid sourceId,
        Guid sourceVersionId,
        string expectedDigest,
        CancellationToken cancellationToken);
}

public sealed record ProtectedCanonicalUtf8(
    Guid SourceId,
    Guid SourceVersionId,
    ReadOnlyMemory<byte> Utf8,
    string ContentDigest);
