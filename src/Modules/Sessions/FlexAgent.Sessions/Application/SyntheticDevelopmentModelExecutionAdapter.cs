using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

/// <summary>
/// Repeating Development/Testing adapter that always admits a Respond envelope
/// and a short participant-visible reply. Unlike
/// <see cref="DeterministicFakeModelExecutionAdapter"/>, it is not a scripted
/// queue and can serve live compose Sessions.
/// </summary>
public sealed class SyntheticDevelopmentModelExecutionAdapter : IModelExecutionPort
{
    public const string ParticipantVisibleReply =
        "Thank you. What trade-off would you change if you rebuilt this system today?";

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

        var envelope = new EnvelopeRecommendation(
            "adec.synth." + Guid.NewGuid().ToString("N"),
            request.AgentInvocationId,
            DateTimeOffset.UtcNow,
            DecisionDispositions.Respond,
            [
                new OutputRecommendation(
                    AgentOutputKinds.Message,
                    "out.message.primary",
                    "participant_reply",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null),
            ],
            [],
            null,
            null);
        var utf8Json = AgentDecisionEnvelopeSerializer.ToUtf8Json(envelope);
        if (utf8Json.Length > request.MaxControlUtf8Bytes)
        {
            return Task.FromResult<ModelExecutionAttemptResult>(
                new ModelExecutionFailed(ExecutionFailureReasons.MalformedControl));
        }

        if (!ValidatedAgentDecisionEnvelope.TryAdmit(utf8Json, out var admitted, out var failureReasonCategory)
            || admitted is null)
        {
            return Task.FromResult<ModelExecutionAttemptResult>(
                new ModelExecutionFailed(failureReasonCategory));
        }

        if (!string.Equals(admitted.Envelope.InvocationId, request.AgentInvocationId, StringComparison.Ordinal))
        {
            return Task.FromResult<ModelExecutionAttemptResult>(
                new ModelExecutionFailed(ExecutionFailureReasons.MalformedControl));
        }

        return Task.FromResult<ModelExecutionAttemptResult>(new ModelExecutionStructuredControl(admitted));
    }

    public async IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
        ModelContentStreamRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ModelContentTextDelta(ParticipantVisibleReply);
        yield return new ModelContentCompleted();
        await Task.CompletedTask;
    }
}
