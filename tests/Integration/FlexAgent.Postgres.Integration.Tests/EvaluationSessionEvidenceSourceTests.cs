using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationSessionEvidenceSourceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Session_evidence_source_loads_authoritative_handoff_and_scoped_transcript()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        var source = new PostgresEvaluationSessionEvidenceSource(
            Fixture.Services.ConnectionAccessor,
            new PostgresEvaluationHandoffSource(Fixture.Services.ConnectionAccessor));

        var handoffId = prepared.Request.FrozenInput.HandoffId;
        var bundle = await source.LoadAsync(
            prepared.Request.FrozenInput.Ownership.OrganizationId,
            prepared.Request.FrozenInput.Ownership.SessionId,
            handoffId,
            CancellationToken);

        Assert.NotNull(bundle);
        Assert.Equal(handoffId, bundle!.Handoff.HandoffId);
        Assert.Equal(42, bundle.Handoff.CutoffSequence);
        Assert.NotNull(bundle.TranscriptItemsAtOrBeforeCutoff);
    }
}
