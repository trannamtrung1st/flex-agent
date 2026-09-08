using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationSubmissionEvidenceSourceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Submission_evidence_source_loads_bound_completed_attempt_items()
    {
        var (prepared, artifacts) = await EvaluationPersistenceTestSeed.PrepareBoundSubmissionEvidenceAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        var source = CreateSource(artifacts);
        var ownership = prepared.Request.FrozenInput.Ownership;

        var bundle = await source.LoadBoundItemsAsync(
            ownership.OrganizationId,
            ownership.ActivityId,
            ownership.ParticipantId,
            ownership.AttemptId,
            ownership.SessionId,
            CancellationToken);

        Assert.NotNull(bundle);
        Assert.Single(bundle!.BoundItems);
        Assert.Equal(
            EvaluationEvidenceSourceIdentity.SubmissionItemSourceId(prepared.BoundSubmission.ItemId),
            bundle.BoundItems[0].SourceId);
        Assert.Equal("bound submission evidence text", System.Text.Encoding.UTF8.GetString(bundle.BoundItems[0].ExactUtf8.Span));
    }

    [Fact]
    public async Task Submission_evidence_source_rejects_wrong_session_scope()
    {
        var (prepared, artifacts) = await EvaluationPersistenceTestSeed.PrepareBoundSubmissionEvidenceAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        var source = CreateSource(artifacts);
        var ownership = prepared.Request.FrozenInput.Ownership;

        var bundle = await source.LoadBoundItemsAsync(
            ownership.OrganizationId,
            ownership.ActivityId,
            ownership.ParticipantId,
            ownership.AttemptId,
            Guid.CreateVersion7(),
            CancellationToken);

        Assert.Null(bundle);
    }

    [Fact]
    public async Task Submission_evidence_source_returns_null_when_artifact_bytes_are_missing()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        var source = CreateSource(new InMemoryArtifactStore());
        var ownership = prepared.Request.FrozenInput.Ownership;

        var bundle = await source.LoadBoundItemsAsync(
            ownership.OrganizationId,
            ownership.ActivityId,
            ownership.ParticipantId,
            ownership.AttemptId,
            ownership.SessionId,
            CancellationToken);

        Assert.Null(bundle);
    }

    private PostgresEvaluationSubmissionEvidenceSource CreateSource(IArtifactStore artifacts) =>
        new(Fixture.Services.ConnectionAccessor, artifacts);
}
