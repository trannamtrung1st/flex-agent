using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Tests;

public sealed class InMemorySubmissionIdentityTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-02T12:00:00Z");
    private static readonly Guid OrganizationId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid EnrollmentId = Guid.Parse("dddddddd-dddd-4ddd-8ddd-ddddddddddda");
    private static readonly Guid ParticipantId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
    private static readonly string Digest = new('a', 64);

    [Fact]
    public async Task Separate_version_stores_keep_enrollment_submission_maps_isolated()
    {
        var first = new InMemorySubmissionVersionStore();
        var second = new InMemorySubmissionVersionStore();
        var transaction = new InMemoryEnrollmentTransaction();
        var firstSubmissionId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var secondSubmissionId = Guid.Parse("22222222-2222-4222-8222-222222222222");

        await first.InsertAcceptedVersionAsync(
            Version(firstSubmissionId),
            ParticipantId,
            transaction,
            CancellationToken.None);
        await second.InsertAcceptedVersionAsync(
            Version(secondSubmissionId),
            ParticipantId,
            transaction,
            CancellationToken.None);

        var found = await first.FindSubmissionIdByEnrollmentAsync(
            OrganizationId,
            EnrollmentId,
            transaction,
            CancellationToken.None);
        Assert.Equal(firstSubmissionId, found);

        var next = await first.AllocateNextVersionAsync(
            OrganizationId,
            found!.Value,
            transaction,
            CancellationToken.None);
        Assert.Equal(2, next.VersionNumber);
        Assert.NotNull(next.PredecessorVersionId);
    }

    private static AcceptedSubmissionVersion Version(Guid submissionId) =>
        new(
            submissionId,
            Guid.CreateVersion7(),
            1,
            new SubmissionParentScope(
                OrganizationId,
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2"),
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3"),
                EnrollmentId,
                ParticipantId,
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa4"),
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa5"),
                Digest),
            Digest,
            null,
            Now,
            [new AcceptedVersionItem(Guid.CreateVersion7(), MaterialCategories.DirectText, null, 12, Digest, "obj", "v1")]);
}
