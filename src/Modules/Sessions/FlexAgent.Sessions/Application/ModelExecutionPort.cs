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

public interface IModelExecutionPort
{
    Task<ModelExecutionAttemptResult> ExecuteAsync(
        ModelExecutionAttemptRequest request,
        CancellationToken cancellationToken);
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

public sealed class DeterministicFakeModelExecutionAdapter : IModelExecutionPort
{
    private readonly Queue<Func<ModelExecutionAttemptRequest, CancellationToken, ModelExecutionAttemptResult>> _scripted =
        new();

    public void EnqueueEnvelope(EnvelopeRecommendation envelope) =>
        _scripted.Enqueue((request, _) => CompleteFromJson(request, AgentDecisionEnvelopeSerializer.ToUtf8Json(envelope)));

    public void EnqueueControlJson(byte[] utf8Json) =>
        _scripted.Enqueue((request, _) => CompleteFromJson(request, utf8Json));

    public void EnqueueFailure(string reasonCategory) =>
        _scripted.Enqueue((_, _) => new ModelExecutionFailed(reasonCategory));

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
