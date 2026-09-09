using System.Security.Cryptography;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class BuiltinEvaluatorImplementationBindingTests
{
    [Fact]
    public void Runner_source_artifact_digest_matches_checked_in_source()
    {
        AssertSourceArtifactMatches(
            BuiltinEvaluatorImplementationBinding.RunnerSourceArtifactDigest,
            "src",
            "Modules",
            "Evaluation",
            "FlexAgent.Evaluation.Infrastructure",
            "RestrictedBuiltinDeterministicEvaluatorRunner.cs");
    }

    [Fact]
    public void Deterministic_execution_bounds_source_artifact_digest_matches_checked_in_source()
    {
        AssertSourceArtifactMatches(
            BuiltinEvaluatorImplementationBinding.DeterministicExecutionBoundsSourceArtifactDigest,
            "src",
            "Modules",
            "Evaluation",
            "FlexAgent.Evaluation",
            "Domain",
            "DeterministicExecutionBounds.cs");
    }

    [Fact]
    public void In_process_execution_contract_source_artifact_digest_matches_checked_in_source()
    {
        AssertSourceArtifactMatches(
            BuiltinEvaluatorImplementationBinding.InProcessExecutionContractSourceArtifactDigest,
            "src",
            "Modules",
            "Evaluation",
            "FlexAgent.Evaluation",
            "Domain",
            "InProcessDeterministicExecutionContract.cs");
    }

    [Fact]
    public void Evaluation_identity_source_artifact_digest_matches_checked_in_source()
    {
        AssertSourceArtifactMatches(
            BuiltinEvaluatorImplementationBinding.EvaluationIdentitySourceArtifactDigest,
            "src",
            "Modules",
            "Evaluation",
            "FlexAgent.Evaluation",
            "Domain",
            "EvaluationCodes.cs");
    }

    [Fact]
    public void Evaluation_positive_duration_source_artifact_digest_matches_checked_in_source()
    {
        AssertSourceArtifactMatches(
            BuiltinEvaluatorImplementationBinding.EvaluationPositiveDurationSourceArtifactDigest,
            "src",
            "BuildingBlocks",
            "FlexAgent.Contracts",
            "Evaluation",
            "EvaluationPositiveDuration.cs");
    }

    [Fact]
    public void Project_source_artifact_digests_match_checked_in_source()
    {
        AssertSourceArtifactMatches(
            BuiltinEvaluatorImplementationBinding.EvaluationProjectSourceArtifactDigest,
            "src",
            "Modules",
            "Evaluation",
            "FlexAgent.Evaluation",
            "FlexAgent.Evaluation.csproj");
        AssertSourceArtifactMatches(
            BuiltinEvaluatorImplementationBinding.EvaluationInfrastructureProjectSourceArtifactDigest,
            "src",
            "Modules",
            "Evaluation",
            "FlexAgent.Evaluation.Infrastructure",
            "FlexAgent.Evaluation.Infrastructure.csproj");
    }

    [Fact]
    public void Wall_clock_deadline_uses_minimum_of_elapsed_and_cpu_limits()
    {
        var startedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

        Assert.True(InProcessDeterministicExecutionContract.TryCreateWallClockDeadline(
            startedAt,
            "PT10S",
            "PT5S",
            out var deadlineUtc,
            out _));

        Assert.Equal(startedAt.AddSeconds(5), deadlineUtc);
    }

    [Fact]
    public void Scalar_loop_abort_interval_is_checked_at_expected_indices()
    {
        Assert.False(InProcessDeterministicExecutionContract.ShouldAbortScalarLoop(255));
        Assert.True(InProcessDeterministicExecutionContract.ShouldAbortScalarLoop(256));
        Assert.True(InProcessDeterministicExecutionContract.ShouldAbortScalarLoop(512));
    }

    private static void AssertSourceArtifactMatches(string expectedDigest, params string[] relativePathParts)
    {
        var path = Path.Combine(FindRepositoryRoot(), Path.Combine(relativePathParts));
        var computed = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        Assert.Equal(expectedDigest, computed);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "FlexAgent.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
