using System.Text.Json;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class DeterministicFactContextLoaderTests
{
    private static readonly EvaluationProcedureV1 Procedure =
        EvaluationProcedureTestFixtures.LoadP0TextSynthetic();
    [Fact]
    public async Task Loader_returns_empty_dictionary_when_no_deterministic_locators()
    {
        using var locatorDocument = JsonDocument.Parse("{}");
        var entries = new[]
        {
            new EvidenceLocatorVerificationEntry(
                Guid.CreateVersion7(),
                "crit.objective.word-count",
                locatorDocument.RootElement.Clone()),
        };

        var result = await DeterministicFactContextLoader.TryLoadForCompletionAsync(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Procedure,
            entries,
            new FakeOutputStore(),
            CancellationToken.None);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Loader_materializes_projection_from_store()
    {
        var attemptId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        var outputUtf8 = """{"schema":"eval.output.word-count.v1","value":2}"""u8.ToArray();
        var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(outputUtf8))
            .ToLowerInvariant();
        var projection = EvaluationDeterministicFactProjector.TryCreate(attemptId, outputUtf8, digest).Value!;
        using var locatorDocument = JsonDocument.Parse(
            BuildLocatorJson(projection.SourceId, projection.SourceVersion, digest));
        var organizationId = Guid.CreateVersion7();
        var requestId = Guid.CreateVersion7();
        var entries = new[]
        {
            new EvidenceLocatorVerificationEntry(
                Guid.CreateVersion7(),
                "crit.objective.word-count",
                locatorDocument.RootElement.Clone()),
        };

        var result = await DeterministicFactContextLoader.TryLoadForCompletionAsync(
            organizationId,
            requestId,
            Procedure,
            entries,
            new FakeOutputStore(
                projection,
                "crit.objective.word-count",
                "crit.objective.word-count.v1"),
            CancellationToken.None);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Single(result.Value!);
        Assert.Equal(projection.SourceId, result.Value!.Keys.Single());
        Assert.Equal(projection.ContentDigest, result.Value[projection.SourceId].ContentDigest);
    }

    [Fact]
    public async Task Loader_fails_when_store_has_no_materialized_payload()
    {
        var attemptId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        var digest = new string('a', 64);
        var sourceId = EvaluationEvidenceSourceIdentity.DeterministicFactSourceId(attemptId);
        using var locatorDocument = JsonDocument.Parse(
            BuildLocatorJson(
                sourceId,
                EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(digest),
                digest));
        var entries = new[]
        {
            new EvidenceLocatorVerificationEntry(
                Guid.CreateVersion7(),
                "crit.objective.word-count",
                locatorDocument.RootElement.Clone()),
        };

        var result = await DeterministicFactContextLoader.TryLoadForCompletionAsync(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Procedure,
            entries,
            new FakeOutputStore(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProtectedContent, result.OutcomeCode);
    }

    [Fact]
    public async Task Loader_fails_when_same_source_id_requires_conflicting_digests()
    {
        var attemptId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        var sourceId = EvaluationEvidenceSourceIdentity.DeterministicFactSourceId(attemptId);
        var firstDigest = new string('a', 64);
        var secondDigest = new string('b', 64);
        using var firstLocator = JsonDocument.Parse(
            BuildLocatorJson(
                sourceId,
                EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(firstDigest),
                firstDigest));
        using var secondLocator = JsonDocument.Parse(
            BuildLocatorJson(
                sourceId,
                EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(secondDigest),
                secondDigest));
        var entries = new[]
        {
            new EvidenceLocatorVerificationEntry(
                Guid.CreateVersion7(),
                "crit.objective.word-count",
                firstLocator.RootElement.Clone()),
            new EvidenceLocatorVerificationEntry(
                Guid.CreateVersion7(),
                "crit.assisted.structure",
                secondLocator.RootElement.Clone()),
        };

        var result = await DeterministicFactContextLoader.TryLoadForCompletionAsync(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Procedure,
            entries,
            new FakeOutputStore(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, result.OutcomeCode);
    }

    [Fact]
    public async Task Loader_fails_when_completion_criterion_does_not_match_producing_attempt()
    {
        var attemptId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        var outputUtf8 = """{"schema":"eval.output.word-count.v1","value":2}"""u8.ToArray();
        var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(outputUtf8))
            .ToLowerInvariant();
        var projection = EvaluationDeterministicFactProjector.TryCreate(attemptId, outputUtf8, digest).Value!;
        using var locatorDocument = JsonDocument.Parse(
            BuildLocatorJson(projection.SourceId, projection.SourceVersion, digest));
        var entries = new[]
        {
            new EvidenceLocatorVerificationEntry(
                Guid.CreateVersion7(),
                "crit.assisted.structure",
                locatorDocument.RootElement.Clone()),
        };

        var result = await DeterministicFactContextLoader.TryLoadForCompletionAsync(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Procedure,
            entries,
            new FakeOutputStore(
                projection,
                "crit.objective.word-count",
                "crit.objective.word-count.v1"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProtectedContent, result.OutcomeCode);
    }

    private static string BuildLocatorJson(string sourceId, string sourceVersion, string sourceDigest) =>
        $$"""
        {
          "locator_schema":"evidence-locator.v1",
          "source_type":"deterministic.fact",
          "source_ref":{"source_id":"{{sourceId}}","source_version":"{{sourceVersion}}"},
          "integrity":{"source_digest":"{{sourceDigest}}","adapter_version":"locator-adapter.v1","verification_state":"verified"}
        }
        """;

    private sealed class FakeOutputStore(
        EvaluationSafeFactProjection? projection = null,
        string? boundCriterionId = null,
        string? boundCriterionVersion = null) : IProtectedDeterministicOutputStore
    {
        public Task<EvaluationDecision<bool>> TryPersistAsync(
            ProtectedDeterministicOutputPersistCommand command,
            CancellationToken cancellationToken) =>
            Task.FromResult(EvaluationDecision<bool>.Ok(true));

        public Task<EvaluationSafeFactProjection?> TryLoadProjectionAsync(
            Guid organizationId,
            Guid requestId,
            Guid deterministicAttemptId,
            string expectedContentDigest,
            string expectedCriterionId,
            string expectedCriterionVersion,
            CancellationToken cancellationToken)
        {
            if (projection is null
                || boundCriterionId is null
                || boundCriterionVersion is null
                || !string.Equals(expectedCriterionId, boundCriterionId, StringComparison.Ordinal)
                || !string.Equals(expectedCriterionVersion, boundCriterionVersion, StringComparison.Ordinal))
            {
                return Task.FromResult<EvaluationSafeFactProjection?>(null);
            }

            return Task.FromResult<EvaluationSafeFactProjection?>(projection);
        }

        public Task<VerifiedDeterministicOutputMaterial?> TryLoadVerifiedMaterialAsync(
            EvaluationOwnership ownership,
            Guid requestId,
            Guid deterministicAttemptId,
            string expectedContentDigest,
            string expectedCriterionId,
            string expectedCriterionVersion,
            CancellationToken cancellationToken) =>
            Task.FromResult<VerifiedDeterministicOutputMaterial?>(null);
    }
}
