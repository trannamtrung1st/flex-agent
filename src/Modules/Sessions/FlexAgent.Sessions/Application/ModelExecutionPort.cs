using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record ModelExecutionAttemptRequest(
    SessionOwnership Ownership,
    string AgentInvocationId,
    string ProviderId,
    string CredentialBindingReference,
    string CredentialBindingVersion,
    InvocationContext Context,
    int AttemptOrdinal,
    int MaxControlUtf8Bytes);

public abstract record ModelExecutionAttemptResult;

public sealed record ModelExecutionStructuredControl(ValidatedAgentDecisionEnvelope Control)
    : ModelExecutionAttemptResult
{
    public EnvelopeRecommendation Envelope => Control.Envelope;
}

public sealed record ModelExecutionFailed(string ReasonCategory) : ModelExecutionAttemptResult;

public abstract record ModelContentEvent;

/// <summary>
/// A non-overlapping suffix. Adapters must supply well-formed Unicode scalar
/// text (no unpaired surrogates). This event is not a resume cursor; after any
/// durable fragment, interrupted delta streams must not restart without a
/// proven provider position.
/// </summary>
public sealed record ModelContentTextDelta(string ExactUtf8Text) : ModelContentEvent;

/// <summary>
/// A cumulative snapshot of the assembled visible text. Adapters must supply
/// well-formed Unicode scalar text. The normalizer verifies the committed
/// prefix before taking any suffix.
/// </summary>
public sealed record ModelContentCumulativeSnapshot(string ExactUtf8Text) : ModelContentEvent;

public sealed record ModelContentMetadata : ModelContentEvent;

public sealed record ModelContentCompleted : ModelContentEvent;

/// <summary>
/// Starts a content stream for an invocation/generation attempt. The request
/// does not carry a provider cursor, committed ordinal, or byte position.
/// Resume is therefore not proven; the worker seals a visible prefix
/// <c>Incomplete</c> instead of restarting a delta stream.
/// </summary>
public sealed record ModelContentStreamRequest(
    SessionOwnership Ownership,
    string AgentInvocationId,
    string GenerationAttemptId);

public interface IModelExecutionPort
{
    Task<ModelExecutionAttemptResult> ExecuteAsync(
        ModelExecutionAttemptRequest request,
        CancellationToken cancellationToken);

    IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
        ModelContentStreamRequest request,
        CancellationToken cancellationToken) =>
        AsyncEnumerable.Empty<ModelContentEvent>();
}

public static class ModelExecutionPreflight
{
    public static ModelExecutionFailed? RejectIfBindingUnavailable(
        ModelDeploymentCredentialBindingResult binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (binding.Succeeded && binding.Binding is not null)
        {
            return null;
        }

        return new ModelExecutionFailed(ExecutionFailureReasons.CredentialBindingFailed);
    }
}

public sealed class FailClosedModelExecutionPort : IModelExecutionPort
{
    public static FailClosedModelExecutionPort Instance { get; } = new();

    public Task<ModelExecutionAttemptResult> ExecuteAsync(
        ModelExecutionAttemptRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult<ModelExecutionAttemptResult>(
            new ModelExecutionFailed(ExecutionFailureReasons.ProviderUnavailable));
    }

    public IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
        ModelContentStreamRequest request,
        CancellationToken cancellationToken) =>
        AsyncEnumerable.Empty<ModelContentEvent>();
}

public sealed class DeterministicFakeModelExecutionAdapter : IModelExecutionPort
{
    private readonly Queue<Func<ModelExecutionAttemptRequest, CancellationToken, ModelExecutionAttemptResult>> _scripted =
        new();
    private readonly Queue<ModelContentEvent[]> _content = new();

    public void EnqueueEnvelope(EnvelopeRecommendation envelope) =>
        _scripted.Enqueue((request, _) => CompleteFromJson(request, AgentDecisionEnvelopeSerializer.ToUtf8Json(envelope)));

    public void EnqueueControlJson(byte[] utf8Json) =>
        _scripted.Enqueue((request, _) => CompleteFromJson(request, utf8Json));

    public void EnqueueFailure(string reasonCategory) =>
        _scripted.Enqueue((_, _) => new ModelExecutionFailed(reasonCategory));

    public void EnqueueContent(params ModelContentEvent[] events) =>
        _content.Enqueue(events);

    public Task<ModelExecutionAttemptResult> ExecuteAsync(
        ModelExecutionAttemptRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult<ModelExecutionAttemptResult>(
                new ModelExecutionFailed(ExecutionAttemptOutcomeCategories.Cancelled));
        }

        if (string.IsNullOrWhiteSpace(request.CredentialBindingReference)
            || string.IsNullOrWhiteSpace(request.CredentialBindingVersion))
        {
            return Task.FromResult<ModelExecutionAttemptResult>(
                new ModelExecutionFailed(ExecutionFailureReasons.CredentialBindingFailed));
        }

        if (_scripted.Count == 0)
        {
            throw new InvalidOperationException("Deterministic fake adapter has no scripted response.");
        }

        return Task.FromResult(_scripted.Dequeue()(request, cancellationToken));
    }

    public async IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
        ModelContentStreamRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_content.Count == 0)
        {
            yield break;
        }

        foreach (var item in _content.Dequeue())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.CompletedTask;
        }
    }

    private static ModelExecutionAttemptResult CompleteFromJson(
        ModelExecutionAttemptRequest request,
        byte[] utf8Json)
    {
        if (utf8Json.Length > request.MaxControlUtf8Bytes)
        {
            return new ModelExecutionFailed(ExecutionFailureReasons.MalformedControl);
        }

        if (!ValidatedAgentDecisionEnvelope.TryAdmit(utf8Json, out var admitted, out var failureReasonCategory)
            || admitted is null)
        {
            return new ModelExecutionFailed(failureReasonCategory);
        }

        if (!string.Equals(admitted.Envelope.InvocationId, request.AgentInvocationId, StringComparison.Ordinal))
        {
            return new ModelExecutionFailed(ExecutionFailureReasons.MalformedControl);
        }

        return new ModelExecutionStructuredControl(admitted);
    }
}
