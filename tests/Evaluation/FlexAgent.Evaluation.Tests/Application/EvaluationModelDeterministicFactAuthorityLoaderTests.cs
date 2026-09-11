using System.Text;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationModelDeterministicFactAuthorityLoaderTests
{
    private static readonly EvaluationOwnership Ownership = EvaluationFixtures.Ownership();
    private static readonly Guid RequestId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab");
    private static readonly Guid AttemptId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public async Task Loader_returns_store_backed_material_matching_verified_projection()
    {
        var verified = CreateVerifiedFacts("""{"valid":true}""");

        var result = await EvaluationModelDeterministicFactAuthorityLoader.TryLoadAsync(
            Ownership,
            RequestId,
            AttemptId,
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            verified,
            new FakeOutputStore(verified.Values.Single(), "prot.eval.fact.0002"),
            CancellationToken.None);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Single(result.Value!);
        Assert.Equal("prot.eval.fact.0002", result.Value!.Values.Single().ProtectedRef.ProtectedRef);
    }

    [Fact]
    public async Task Loader_fails_when_store_returns_forged_protected_ref_digest()
    {
        var verified = CreateVerifiedFacts("""{"valid":true}""");

        var result = await EvaluationModelDeterministicFactAuthorityLoader.TryLoadAsync(
            Ownership,
            RequestId,
            AttemptId,
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            verified,
            new FakeOutputStore(verified.Values.Single(), "prot.eval.fact.0002", protectedDigest: new string('f', 64)),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    [Fact]
    public async Task Loader_fails_when_store_has_no_material()
    {
        var verified = CreateVerifiedFacts("""{"valid":true}""");

        var result = await EvaluationModelDeterministicFactAuthorityLoader.TryLoadAsync(
            Ownership,
            RequestId,
            AttemptId,
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            verified,
            new FakeOutputStore(projection: null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProtectedContent, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    [Fact]
    public async Task Loader_fails_when_verified_fact_attempt_does_not_match_expected_invocation()
    {
        var verified = CreateVerifiedFacts("""{"valid":true}""");

        var result = await EvaluationModelDeterministicFactAuthorityLoader.TryLoadAsync(
            Ownership,
            RequestId,
            Guid.Parse("44444444-4444-4444-8444-444444444444"),
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            verified,
            new FakeOutputStore(verified.Values.Single(), "prot.eval.fact.0002"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    private static Dictionary<string, EvaluationSafeFactProjection> CreateVerifiedFacts(string json)
    {
        var digest = new string('c', 64);
        var sourceId = EvaluationEvidenceSourceIdentity.DeterministicFactSourceId(AttemptId);
        return new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal)
        {
            [sourceId] = new(
                sourceId,
                EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(digest),
                digest,
                Encoding.UTF8.GetBytes(json)),
        };
    }

    private sealed class FakeOutputStore(
        EvaluationSafeFactProjection? projection,
        string protectedRef = "prot.eval.fact.0002",
        string? protectedDigest = null) : IProtectedDeterministicOutputStore
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
            CancellationToken cancellationToken) =>
            Task.FromResult(projection);

        public Task<VerifiedDeterministicOutputMaterial?> TryLoadVerifiedMaterialAsync(
            EvaluationOwnership ownership,
            Guid requestId,
            Guid deterministicAttemptId,
            string expectedContentDigest,
            string expectedCriterionId,
            string expectedCriterionVersion,
            CancellationToken cancellationToken)
        {
            if (projection is null
                || ownership.OrganizationId != Ownership.OrganizationId
                || ownership.ActivityId != Ownership.ActivityId
                || requestId != RequestId
                || deterministicAttemptId != AttemptId)
            {
                return Task.FromResult<VerifiedDeterministicOutputMaterial?>(null);
            }

            return Task.FromResult<VerifiedDeterministicOutputMaterial?>(
                new VerifiedDeterministicOutputMaterial(
                    projection,
                    new ProtectedPayloadRefV1(protectedRef, protectedDigest ?? projection.ContentDigest)));
        }
    }
}
