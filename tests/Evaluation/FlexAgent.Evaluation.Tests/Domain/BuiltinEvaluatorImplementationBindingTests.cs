using System.Security.Cryptography;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class BuiltinEvaluatorImplementationBindingTests
{
    [Fact]
    public void Runner_source_artifact_digest_matches_checked_in_source()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Modules",
            "Evaluation",
            "FlexAgent.Evaluation.Infrastructure",
            "RestrictedBuiltinDeterministicEvaluatorRunner.cs");
        var bytes = File.ReadAllBytes(path);
        var computed = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        Assert.Equal(BuiltinEvaluatorImplementationBinding.RunnerSourceArtifactDigest, computed);
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
