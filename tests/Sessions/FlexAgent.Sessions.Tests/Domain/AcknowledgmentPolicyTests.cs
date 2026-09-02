using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class AcknowledgmentPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Record_requires_deliberate_outcome_and_exact_notice_version()
    {
        var notice = Notice();
        var recorded = AcknowledgmentPolicy.Record(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            notice,
            AcknowledgmentOutcomes.Affirmed,
            Now);
        Assert.True(recorded.Succeeded);
        Assert.Equal(AcknowledgmentOutcomes.Affirmed, recorded.Value!.Outcome);
        Assert.Null(recorded.Value.BoundAttemptId);
    }

    [Fact]
    public void Declined_or_withdrawn_current_records_block_start_revalidation()
    {
        var notice = Notice();
        var enrollmentId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();
        var declined = AcknowledgmentPolicy.Record(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            enrollmentId,
            participantId,
            notice,
            AcknowledgmentOutcomes.Declined,
            Now).Value!;
        Assert.Equal(
            AcknowledgmentFailureCodes.Stale,
            AcknowledgmentPolicy.ValidateCurrentAffirmations(
                [notice],
                [declined],
                enrollmentId,
                participantId));
    }

    [Fact]
    public void Cross_scope_or_stale_digest_cannot_bind_to_an_attempt()
    {
        var notice = Notice();
        var record = AcknowledgmentPolicy.Record(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            notice,
            AcknowledgmentOutcomes.Affirmed,
            Now).Value!;
        var cross = AcknowledgmentPolicy.BindToAttempt(
            record,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            record.ParticipantActorId);
        Assert.False(cross.Succeeded);
        Assert.Equal(AcknowledgmentFailureCodes.CrossScope, cross.OutcomeCode);
    }

    [Fact]
    public void Affirmed_current_records_bind_once_to_the_started_attempt()
    {
        var notice = Notice();
        var record = AcknowledgmentPolicy.Record(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            notice,
            AcknowledgmentOutcomes.Affirmed,
            Now).Value!;
        var attemptId = Guid.CreateVersion7();
        var bound = AcknowledgmentPolicy.BindToAttempt(
            record,
            attemptId,
            record.EnrollmentId,
            record.ParticipantActorId);
        Assert.True(bound.Succeeded);
        Assert.Equal(attemptId, bound.Value!.BoundAttemptId);
        Assert.Null(AcknowledgmentPolicy.ValidateCurrentAffirmations(
            [notice],
            [bound.Value],
            record.EnrollmentId,
            record.ParticipantActorId));
    }

    private static ParticipantNoticeDescriptor Notice() =>
        new(
            Guid.CreateVersion7(),
            "instructions",
            AcknowledgmentOutcomes.Affirmed,
            "notice:synthetic.instructions",
            new string('a', 64),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new string('b', 64));
}
