using FlexAgent.Contracts.Evaluation;
using System.Security.Cryptography;
using System.Text;

namespace FlexAgent.Evaluation.Domain;

public static class DeterministicInvocationOutcomes
{
    public const string Succeeded = "succeeded";

    public const string Failed = "failed";

    public const string Timeout = "timeout";

    public const string ResourceExhausted = "resource_exhausted";

    public const string InvalidOutput = "invalid_output";

    public static string ToPersistenceOutcome(string contractOutcome) =>
        contractOutcome switch
        {
            Succeeded => "succeeded",
            Failed => "failed",
            Timeout => "timed_out",
            ResourceExhausted => "bound_exhausted",
            InvalidOutput => "invalid_output",
            _ => Failed,
        };
}

public static class DeterministicInvocationIdentity
{
    public static Guid ComputeAttemptId(
        Guid requestId,
        string criterionId,
        string canonicalInputDigest)
    {
        var payload = Encoding.UTF8.GetBytes($"{requestId:N}|{criterionId}|{canonicalInputDigest}");
        var hash = SHA256.HashData(payload);
        return new Guid(hash.AsSpan(0, 16));
    }
}

public sealed record DeterministicEvaluatorCanonicalInput(
    ReadOnlyMemory<byte> CanonicalUtf8,
    string CanonicalInputDigest);

public sealed record DeterministicEvaluatorExecutionRequest(
    EvaluationOwnership Ownership,
    Guid RequestId,
    Guid InvocationAttemptId,
    string CriterionId,
    string CriterionVersion,
    DeterministicEvaluatorBindingV1 Binding,
    DeterministicEvaluatorCanonicalInput Input);

public sealed record DeterministicEvaluatorExecutionResult(
    Guid DeterministicAttemptId,
    string Outcome,
    string? FailureCategory,
    ReadOnlyMemory<byte>? OutputUtf8,
    string? OutputContentDigest,
    string? ProtectedInputRef,
    string? ProtectedOutputRef,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt);

public sealed record DeterministicInvocationAppendCommand(
    EvaluationOwnership Ownership,
    Guid RequestId,
    Guid InvocationAttemptId,
    Guid DeterministicAttemptId,
    string CriterionId,
    string CriterionVersion,
    string CanonicalInputDigest,
    DeterministicEvaluatorBindingV1 Binding,
    DeterministicEvaluatorExecutionResult Result);
