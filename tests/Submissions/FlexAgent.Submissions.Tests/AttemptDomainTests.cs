using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Tests;

public sealed class AttemptDomainTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-02T06:00:00Z");
    private static readonly Guid OrganizationId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid ActivityId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid CohortId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2");
    private static readonly Guid BaselineId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa4");
    private static readonly Guid EnrollmentId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa8");
    private static readonly Guid ParticipantId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
    private static readonly Guid TaskSourceId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa5");
    private static readonly Guid TaskVersionId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa6");
    private static readonly string Digest = new string('a', 64);

    [Fact]
    public void Next_ordinal_is_derived_from_trusted_history()
    {
        Assert.Equal(1, AttemptEntitlementCalculator.NextOrdinal([]));
        var first = Activate(ordinal: 1).Value!;
        Assert.Equal(2, AttemptEntitlementCalculator.NextOrdinal([first]));
    }

    [Fact]
    public void Remaining_entitlement_counts_only_consumed_attempts()
    {
        var consumed = Activate(ordinal: 1).Value!;
        Assert.Equal(1, AttemptEntitlementCalculator.Remaining(2, [consumed], [], Now));
        Assert.Equal(0, AttemptEntitlementCalculator.Remaining(1, [consumed], [], Now));
    }

    [Fact]
    public void Unused_retry_entitlement_adds_remaining_without_renumbering()
    {
        var consumed = Activate(ordinal: 1).Value!;
        var retry = new RetryEntitlementFact(
            Guid.CreateVersion7(),
            consumed.AttemptId,
            Now,
            Now.AddDays(1),
            null);
        Assert.Equal(1, AttemptEntitlementCalculator.Remaining(1, [consumed], [retry], Now));
        Assert.Equal(AttemptEntitlementSources.Retry, AttemptEntitlementCalculator.NextEntitlementSource(1, [consumed]));
        Assert.Equal(2, AttemptEntitlementCalculator.NextOrdinal([consumed]));
    }

    [Fact]
    public void Activate_consumes_entitlement_and_freezes_exact_bindings()
    {
        var activated = Activate(ordinal: 1);
        Assert.True(activated.Succeeded);
        Assert.Equal(AttemptStates.Active, activated.Value!.Status);
        Assert.True(activated.Value.Consumed);
        Assert.Equal(AttemptEntitlementSources.Baseline, activated.Value.EntitlementSource);
        Assert.Single(activated.Value.SubmissionBindings);
        Assert.Equal(AttemptOutcomes.Activated, activated.OutcomeCode);
    }

    [Fact]
    public void Client_cannot_select_ordinal_or_retry_source_inconsistently()
    {
        var invalid = Attempt.Activate(
            Guid.CreateVersion7(),
            Scope(),
            ordinal: 0,
            AttemptEntitlementSources.Baseline,
            null,
            Now,
            Now,
            Binding(),
            [new AttemptSubmissionBinding(Guid.CreateVersion7(), 1, 1, Digest)]);
        Assert.False(invalid.Succeeded);
        Assert.Equal(AttemptFailureCodes.InvalidField, invalid.OutcomeCode);
    }

    [Fact]
    public void Abort_keeps_consumed_history_and_rejects_reset()
    {
        var active = Activate(ordinal: 1).Value!;
        var aborted = active.Abort(Now.AddMinutes(5), "integrity_abort");
        Assert.True(aborted.Succeeded);
        Assert.Equal(AttemptStates.Aborted, aborted.Value!.Status);
        Assert.True(aborted.Value.Consumed);
        Assert.Equal(active.Ordinal, aborted.Value.Ordinal);
        Assert.Equal(active.Binding.SessionId, aborted.Value.Binding.SessionId);
        Assert.False(aborted.Value.Complete(Now.AddMinutes(6), "completed").Succeeded);
        Assert.Equal(AttemptFailureCodes.Terminal, aborted.Value.Complete(Now.AddMinutes(6), "completed").OutcomeCode);
    }

    [Theory]
    [InlineData(EnrollmentStates.Suspended, AttemptReadinessStates.EnrollmentUnavailable)]
    [InlineData(EnrollmentStates.Revoked, AttemptReadinessStates.EnrollmentUnavailable)]
    [InlineData(EnrollmentStates.Closed, AttemptReadinessStates.EnrollmentUnavailable)]
    public void Inactive_enrollment_blocks_start_without_consuming(string enrollmentStatus, string expected)
    {
        var readiness = AttemptEligibility.Evaluate(Facts(enrollmentStatus: enrollmentStatus));
        Assert.Equal(expected, readiness.State);
        Assert.DoesNotContain(AttemptClientActions.StartAttempt, readiness.PermittedActions);
        Assert.Equal(1, readiness.RemainingEntitlement);
    }

    [Fact]
    public void Too_early_and_expired_windows_are_distinct_blocks()
    {
        Assert.Equal(
            AttemptReadinessStates.TooEarly,
            AttemptEligibility.Evaluate(Facts(timing: TimingEligibilityStates.TooEarly)).State);
        Assert.Equal(
            AttemptReadinessStates.Expired,
            AttemptEligibility.Evaluate(Facts(timing: TimingEligibilityStates.AttemptStartClosed)).State);
    }

    [Fact]
    public void Exhausted_limit_blocks_without_retry_entitlement()
    {
        var consumed = Activate(ordinal: 1).Value!.Abort(Now.AddMinutes(1), "integrity_abort").Value!;
        var readiness = AttemptEligibility.Evaluate(Facts(history: [consumed], baselineLimit: 1));
        Assert.Equal(AttemptReadinessStates.Exhausted, readiness.State);
        Assert.Equal(0, readiness.RemainingEntitlement);
        Assert.DoesNotContain(AttemptClientActions.StartAttempt, readiness.PermittedActions);
    }

    [Fact]
    public void Missing_or_unreadable_required_material_blocks_start()
    {
        Assert.Equal(
            AttemptReadinessStates.MissingAcceptedMaterial,
            AttemptEligibility.Evaluate(Facts(requiredMaterial: false)).State);
        Assert.Equal(
            AttemptReadinessStates.MaterialNotAgentReadable,
            AttemptEligibility.Evaluate(Facts(agentReadable: false)).State);
    }

    [Fact]
    public void Active_attempt_offers_continue_and_not_a_competing_start()
    {
        var active = Activate(ordinal: 1).Value!;
        var readiness = AttemptEligibility.Evaluate(Facts(history: [active], baselineLimit: 2));
        Assert.Equal(AttemptReadinessStates.ActiveConflict, readiness.State);
        Assert.Equal(active.AttemptId, readiness.ActiveAttemptId);
        Assert.Equal(active.Binding.SessionId, readiness.ActiveSessionId);
        Assert.Contains(AttemptClientActions.ContinueAttempt, readiness.PermittedActions);
        Assert.DoesNotContain(AttemptClientActions.StartAttempt, readiness.PermittedActions);
    }

    [Fact]
    public void Eligible_readiness_exposes_start_without_local_entitlement_decrement()
    {
        var readiness = AttemptEligibility.Evaluate(Facts());
        Assert.Equal(AttemptReadinessStates.Eligible, readiness.State);
        Assert.Equal(1, readiness.NextOrdinal);
        Assert.Equal(1, readiness.RemainingEntitlement);
        Assert.Contains(AttemptClientActions.StartAttempt, readiness.PermittedActions);
    }

    [Fact]
    public void Equivalent_start_key_reconciles_to_the_committed_identifiers()
    {
        var claimed = StartOperationPolicy.Claim(
            OrganizationId,
            ParticipantId,
            EnrollmentId,
            "start-key-0001",
            Digest,
            Guid.CreateVersion7(),
            Now,
            null).Value!;
        var committed = StartOperationPolicy.Commit(
            claimed,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Now.AddSeconds(1)).Value!;
        var replay = StartOperationPolicy.Claim(
            OrganizationId,
            ParticipantId,
            EnrollmentId,
            "start-key-0001",
            Digest,
            Guid.CreateVersion7(),
            Now.AddSeconds(2),
            committed);
        Assert.True(replay.Succeeded);
        Assert.Equal(AttemptOutcomes.Reconciled, replay.OutcomeCode);
        Assert.Equal(committed.AttemptId, replay.Value!.AttemptId);
        Assert.Equal(committed.SessionId, replay.Value.SessionId);
    }

    [Fact]
    public void Mismatched_digest_reuse_fails_closed()
    {
        var claimed = StartOperationPolicy.Claim(
            OrganizationId,
            ParticipantId,
            EnrollmentId,
            "start-key-0001",
            Digest,
            Guid.CreateVersion7(),
            Now,
            null).Value!;
        var conflict = StartOperationPolicy.Claim(
            OrganizationId,
            ParticipantId,
            EnrollmentId,
            "start-key-0001",
            new string('b', 64),
            Guid.CreateVersion7(),
            Now,
            claimed);
        Assert.False(conflict.Succeeded);
        Assert.Equal(AttemptFailureCodes.IdempotencyConflict, conflict.OutcomeCode);
    }

    [Fact]
    public void Stale_claim_can_be_recovered_by_an_authorized_retry()
    {
        var owner = Guid.CreateVersion7();
        var claimed = StartOperationPolicy.Claim(
            OrganizationId,
            ParticipantId,
            EnrollmentId,
            "start-key-0001",
            Digest,
            owner,
            Now,
            null).Value!;
        var recovered = StartOperationPolicy.Claim(
            OrganizationId,
            ParticipantId,
            EnrollmentId,
            "start-key-0001",
            Digest,
            Guid.CreateVersion7(),
            claimed.LeaseUntilUtc,
            claimed);
        Assert.True(recovered.Succeeded);
        Assert.Equal(AttemptOutcomes.ClaimRecovered, recovered.OutcomeCode);
        Assert.NotEqual(owner, recovered.Value!.ClaimOwner);
        Assert.Equal(StartOperationStates.Claimed, recovered.Value.Status);
        Assert.Null(recovered.Value.AttemptId);
    }

    [Fact]
    public void Different_key_cannot_compete_with_a_live_claim()
    {
        var claimed = StartOperationPolicy.Claim(
            OrganizationId,
            ParticipantId,
            EnrollmentId,
            "start-key-0001",
            Digest,
            Guid.CreateVersion7(),
            Now,
            null).Value!;
        Assert.True(StartOperationPolicy.HasActiveConflict([claimed], "start-key-0002", Now));
        Assert.False(StartOperationPolicy.HasActiveConflict([claimed], "start-key-0001", Now));
        Assert.False(StartOperationPolicy.HasActiveConflict([claimed], "start-key-0002", claimed.LeaseUntilUtc));
    }

    [Fact]
    public void Failed_claim_does_not_consume_and_permits_a_later_key()
    {
        var claimed = StartOperationPolicy.Claim(
            OrganizationId,
            ParticipantId,
            EnrollmentId,
            "start-key-0001",
            Digest,
            Guid.CreateVersion7(),
            Now,
            null).Value!;
        var failed = StartOperationPolicy.Fail(claimed, AttemptFailureCodes.Ineligible, Now.AddSeconds(1)).Value!;
        Assert.Equal(StartOperationStates.Failed, failed.Status);
        Assert.Null(failed.AttemptId);
        Assert.False(StartOperationPolicy.HasActiveConflict([failed], "start-key-0002", Now.AddSeconds(2)));
    }

    [Fact]
    public void Version_content_digest_covers_every_ordered_item()
    {
        var first = new AcceptedVersionItem(Guid.Parse("11111111-1111-4111-8111-111111111111"), MaterialCategories.DirectText, null, 4, new string('b', 64), "obj-1", "v1");
        var second = new AcceptedVersionItem(Guid.Parse("22222222-2222-4222-8222-222222222222"), MaterialCategories.DirectText, null, 4, new string('c', 64), "obj-2", "v1");
        var version = new AcceptedSubmissionVersion(
            Guid.CreateVersion7(),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa7"),
            2,
            Scope(),
            Digest,
            null,
            Now,
            [second, first]);
        var digest = AttemptSubmissionProvenance.ForAcceptedVersion(version);
        Assert.Equal(64, digest.Length);
        Assert.NotEqual(first.ContentDigest, digest);
        Assert.Equal(digest, AttemptSubmissionProvenance.ForAcceptedVersion(version with { Items = [first, second] }));
    }

    private static AttemptDecision<Attempt> Activate(int ordinal) =>
        Attempt.Activate(
            Guid.CreateVersion7(),
            Scope(),
            ordinal,
            AttemptEntitlementSources.Baseline,
            null,
            Now,
            Now,
            Binding(),
            [new AttemptSubmissionBinding(Guid.CreateVersion7(), 1, 1, Digest)]);

    private static SubmissionParentScope Scope() =>
        new(
            OrganizationId,
            ActivityId,
            CohortId,
            BaselineId,
            EnrollmentId,
            ParticipantId,
            TaskSourceId,
            TaskVersionId,
            Digest);

    private static AttemptBinding Binding() =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Digest, Digest);

    private static AttemptReadinessFacts Facts(
        string enrollmentStatus = EnrollmentStates.Active,
        string timing = TimingEligibilityStates.Open,
        int baselineLimit = 1,
        IReadOnlyList<Attempt>? history = null,
        bool requiredMaterial = true,
        bool agentReadable = true) =>
        new(
            enrollmentStatus,
            timing,
            baselineLimit,
            history ?? [],
            [],
            requiredMaterial,
            AgentInspectionRequired: true,
            agentReadable,
            ConfigurationReady: true,
            RequiredNoticeProjectionReady: true,
            Now);
}
