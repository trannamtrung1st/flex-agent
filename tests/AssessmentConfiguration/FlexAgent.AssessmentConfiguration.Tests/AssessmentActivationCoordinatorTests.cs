using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Canonicalization;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.AssessmentConfiguration.Tests;

public sealed class AssessmentActivationCoordinatorTests
{
    [Fact]
    public async Task Activate_commits_empty_cohort_baseline_when_ready()
    {
        var harness = await CreateReadyHarnessAsync();
        var outcome = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);

        Assert.True(outcome.Succeeded);
        Assert.Equal("assessment.activated", outcome.OutcomeCode);
        Assert.Equal(CohortStates.Activated, outcome.CohortState);
        Assert.False(string.IsNullOrWhiteSpace(outcome.BaselineDigest));
        Assert.Equal(64, outcome.BaselineDigest!.Length);
        Assert.True(harness.Store.LastWriteWasActivationMetadata);
        var stored = await harness.Store.GetDraftAsync(
            harness.Draft.OrganizationId,
            harness.Draft.ActivityId,
            TestContext.Current.CancellationToken);
        Assert.True(stored!.HasActivatedCohort);
    }

    [Fact]
    public async Task Equivalent_retry_returns_existing_activation()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        var second = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);

        Assert.True(second.Succeeded);
        Assert.Equal(first.BaselineId, second.BaselineId);
        Assert.Single(harness.Store.Cohorts);
    }

    [Fact]
    public async Task Competing_idempotency_key_after_activation_is_a_conflict()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        Assert.True(first.Succeeded);

        var competing = harness.Command("idem-2");
        var outcome = await harness.Coordinator.ActivateAsync(competing, TestContext.Current.CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Equal(AssessmentFailureCodes.ConcurrentActivation, outcome.OutcomeCode);
        Assert.Equal(CohortStates.Activated, outcome.CohortState);
        Assert.Equal(first.BaselineId, outcome.BaselineId);
    }

    [Fact]
    public async Task Same_key_with_different_content_is_an_idempotency_conflict()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        Assert.True(first.Succeeded);

        var retargeted = harness.Command() with { ExpectedRevisionNumber = harness.Draft.RevisionNumber + 1 };
        retargeted = retargeted with { TrustedCommandDigest = harness.CommandDigest.Compute(retargeted) };
        var outcome = await harness.Coordinator.ActivateAsync(retargeted, TestContext.Current.CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Equal(AssessmentFailureCodes.IdempotencyConflict, outcome.OutcomeCode);
        Assert.Contains(harness.Attempts.Items, item => item.OutcomeCode == AssessmentFailureCodes.IdempotencyConflict);
        Assert.Equal(CohortStates.Activated, outcome.CohortState);
        Assert.Equal(first.BaselineId, outcome.BaselineId);
        Assert.Equal(harness.Draft.RevisionId, harness.Attempts.Items.Single(item => item.OutcomeCode == AssessmentFailureCodes.IdempotencyConflict).AuthoritativeRevisionId);
    }

    [Fact]
    public async Task Equivalent_retry_after_success_records_a_deduplicated_attempt()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        var retry = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.True(retry.Succeeded);
        Assert.Equal(first.BaselineId, retry.BaselineId);
        Assert.Equal(AssessmentActivationOutcomes.Deduplicated, retry.OutcomeCode);
        Assert.Contains(harness.Attempts.Items, item => item.OutcomeCode == AssessmentActivationOutcomes.Deduplicated);
        Assert.Single(harness.Attempts.Items, item => item.OutcomeCode == AssessmentActivationOutcomes.Activated);
    }

    [Fact]
    public async Task Blank_or_oversized_idempotency_key_is_an_invalid_field()
    {
        var harness = await CreateReadyHarnessAsync();
        var blank = await harness.Coordinator.ActivateAsync(
            harness.Command("   "),
            TestContext.Current.CancellationToken);
        var oversized = await harness.Coordinator.ActivateAsync(
            harness.Command(new string('a', 129)),
            TestContext.Current.CancellationToken);
        var reconcile = await harness.Coordinator.ReconcileAsync(
            new ReconcileActivationQuery(harness.Actor, harness.Draft.ActivityId, harness.Cohort.CohortId, "bad key"),
            TestContext.Current.CancellationToken);

        Assert.Equal(AssessmentFailureCodes.InvalidField, blank.OutcomeCode);
        Assert.Equal(AssessmentFailureCodes.InvalidField, oversized.OutcomeCode);
        Assert.Equal(AssessmentFailureCodes.InvalidField, reconcile.OutcomeCode);
        Assert.Empty(harness.Attempts.Items);
        Assert.Equal(3, harness.Attempts.RequestAudits.Count);
        Assert.All(harness.Attempts.RequestAudits, item => Assert.Equal(AssessmentFailureCodes.InvalidField, item.ReasonCode));
        Assert.Equal(400, AssessmentIdempotencyKey.StatusForActivation(false, AssessmentFailureCodes.InvalidField));
    }

    [Fact]
    public async Task Reconcile_returns_the_stored_attempt()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);

        var reconciled = await harness.Coordinator.ReconcileAsync(
            new ReconcileActivationQuery(harness.Actor, harness.Draft.ActivityId, harness.Cohort.CohortId, "idem-1"),
            TestContext.Current.CancellationToken);

        Assert.True(reconciled.Succeeded);
        Assert.Equal(first.BaselineId, reconciled.BaselineId);
        Assert.Equal(first.BaselineDigest, reconciled.BaselineDigest);
    }

    [Fact]
    public async Task Participant_relationship_cannot_activate()
    {
        var harness = await CreateReadyHarnessAsync();
        harness = harness with { Actor = CreateActor(mfa: true, relationship: "participant") };
        var outcome = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Equal(AssessmentFailureCodes.Denied, outcome.OutcomeCode);
    }

    [Fact]
    public async Task Mismatched_command_digest_is_an_idempotency_conflict()
    {
        var harness = await CreateReadyHarnessAsync();
        var command = harness.Command() with { TrustedCommandDigest = new string('0', 64) };

        var outcome = await harness.Coordinator.ActivateAsync(command, TestContext.Current.CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Equal(AssessmentFailureCodes.IdempotencyConflict, outcome.OutcomeCode);
    }

    [Fact]
    public async Task Audit_failure_does_not_activate_and_persists_the_attempt()
    {
        var harness = await CreateReadyHarnessAsync();
        harness.UnitOfWork.Transaction.AuditAccepted = false;

        var outcome = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        var stored = await harness.Attempts.FindAsync(
            harness.Draft.OrganizationId,
            harness.Draft.ActivityId,
            harness.Cohort.CohortId,
            "idem-1",
            harness.UnitOfWork.Transaction,
            TestContext.Current.CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Equal(AssessmentFailureCodes.AuditUnavailable, outcome.OutcomeCode);
        Assert.Equal(CohortStates.Draft, harness.Store.Cohorts.Single().State);
        Assert.NotNull(stored);
        Assert.Equal(AssessmentFailureCodes.AuditUnavailable, stored!.OutcomeCode);
        Assert.Equal(harness.Actor.Actor.ActorId, stored.ActorId);
        Assert.Equal(harness.Actor.CorrelationId, stored.CorrelationId);
        Assert.Equal(harness.Draft.RevisionId, stored.AuthoritativeRevisionId);
        Assert.Equal(harness.Draft.RevisionNumber, stored.AuthoritativeRevisionNumber);
    }

    [Fact]
    public async Task Stale_failure_keeps_requested_revision_separate_from_the_authoritative_head()
    {
        var harness = await CreateReadyHarnessAsync();
        var stale = harness.Command() with { ExpectedRevisionNumber = harness.Draft.RevisionNumber + 1 };
        stale = stale with { TrustedCommandDigest = harness.CommandDigest.Compute(stale) };

        var outcome = await harness.Coordinator.ActivateAsync(stale, TestContext.Current.CancellationToken);
        var stored = await harness.Attempts.FindAsync(
            harness.Draft.OrganizationId,
            harness.Draft.ActivityId,
            harness.Cohort.CohortId,
            "idem-1",
            harness.UnitOfWork.Transaction,
            TestContext.Current.CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Equal(AssessmentFailureCodes.StaleRevision, outcome.OutcomeCode);
        Assert.Equal(harness.Draft.RevisionId, stored!.RequestedRevisionId);
        Assert.Equal(harness.Draft.RevisionNumber + 1, stored.RequestedRevisionNumber);
        Assert.Equal(harness.Draft.RevisionId, stored.AuthoritativeRevisionId);
        Assert.Equal(harness.Draft.RevisionNumber, stored.AuthoritativeRevisionNumber);
    }

    [Fact]
    public async Task Same_key_seen_only_after_locks_returns_the_stored_success()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        var delayed = new DelayedFindAttemptStore(harness.Attempts, firstSkipCount: 1);
        var coordinator = new AssessmentActivationCoordinator(
            new InMemoryAssessmentAuthorizationPort(),
            new InMemoryAssessmentSourceCatalog(AssessmentFixtures.PermittedSources()),
            harness.Store,
            harness.UnitOfWork,
            new ActivationBaselineDigester(),
            harness.CommandDigest,
            new InMemoryAssessmentBaselineStore(),
            delayed);

        var outcome = await coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);

        Assert.True(outcome.Succeeded);
        Assert.Equal(first.BaselineId, outcome.BaselineId);
    }

    [Fact]
    public async Task Missing_mfa_denies_administrator_activation()
    {
        var harness = await CreateReadyHarnessAsync();
        harness = harness with { Actor = CreateActor(mfa: false) };
        var outcome = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Equal(HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength, outcome.OutcomeCode);
    }

    [Fact]
    public async Task Early_mfa_failure_retry_revalidates_after_strength_is_restored()
    {
        var harness = await CreateReadyHarnessAsync();
        var denied = await harness.Coordinator.ActivateAsync(
            harness.Command() with { Actor = CreateActor(mfa: false) },
            TestContext.Current.CancellationToken);
        var retried = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);

        Assert.Equal(HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength, denied.OutcomeCode);
        Assert.True(retried.Succeeded, retried.OutcomeCode);
        Assert.Equal(2, harness.Attempts.Items.Count);
        Assert.Contains(harness.Attempts.Items, item => item.OutcomeCode == HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength);
        Assert.Contains(harness.Attempts.Items, item => item.OutcomeCode == "assessment.activated");
    }

    [Fact]
    public async Task Later_denied_request_cannot_rebind_an_idempotency_key()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = harness.Command();
        var deniedFirst = await harness.Coordinator.ActivateAsync(
            first with { Actor = CreateActor(mfa: false) },
            TestContext.Current.CancellationToken);

        var retargeted = harness.Command() with { ExpectedRevisionNumber = harness.Draft.RevisionNumber + 1 };
        retargeted = retargeted with { TrustedCommandDigest = harness.CommandDigest.Compute(retargeted) };
        var poisoned = await harness.Coordinator.ActivateAsync(
            retargeted with { Actor = CreateActor(mfa: false) },
            TestContext.Current.CancellationToken);
        var conflicting = await harness.Coordinator.ActivateAsync(retargeted, TestContext.Current.CancellationToken);
        var recovered = await harness.Coordinator.ActivateAsync(first, TestContext.Current.CancellationToken);

        Assert.Equal(HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength, deniedFirst.OutcomeCode);
        Assert.Equal(AssessmentFailureCodes.IdempotencyConflict, poisoned.OutcomeCode);
        Assert.Equal(AssessmentFailureCodes.IdempotencyConflict, conflicting.OutcomeCode);
        Assert.True(recovered.Succeeded, recovered.OutcomeCode);
        Assert.Equal(first.TrustedCommandDigest, harness.Attempts.Items.Single(item => item.OutcomeCode == "assessment.activated").CommandDigest);
    }

    [Fact]
    public async Task Attempt_timestamps_start_at_activate_entry()
    {
        var clock = new TickAssessmentClock(new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero));
        var harness = await CreateReadyHarnessAsync(clock: clock);

        var outcome = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        var stored = harness.Attempts.Items.Single();

        Assert.True(outcome.Succeeded, outcome.OutcomeCode);
        Assert.Equal(new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero), stored.StartedAtUtc);
        Assert.True(stored.FinishedAtUtc > stored.StartedAtUtc);
    }

    [Fact]
    public async Task Guessed_cohort_persists_an_unbound_failure_attempt()
    {
        var harness = await CreateReadyHarnessAsync();
        var guessed = harness.Command() with { CohortId = Guid.CreateVersion7() };
        guessed = guessed with { TrustedCommandDigest = harness.CommandDigest.Compute(guessed) };

        var outcome = await harness.Coordinator.ActivateAsync(guessed, TestContext.Current.CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Equal(AssessmentFailureCodes.Denied, outcome.OutcomeCode);
        Assert.Contains(harness.Attempts.Items, item => item.CohortId == guessed.CohortId && item.AuthoritativeCohortId is null);
        Assert.True(harness.Attempts.Items.Single(item => item.CohortId == guessed.CohortId).FinishedAtUtc
            >= harness.Attempts.Items.Single(item => item.CohortId == guessed.CohortId).StartedAtUtc);
    }

    [Fact]
    public async Task Success_then_lost_mfa_does_not_replay_the_activation()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        var denied = await harness.Coordinator.ActivateAsync(
            harness.Command() with { Actor = CreateActor(mfa: false) },
            TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.False(denied.Succeeded);
        Assert.Equal(HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength, denied.OutcomeCode);
        Assert.Null(denied.BaselineId);
        Assert.Null(denied.BaselineDigest);
        Assert.Equal(CohortStates.Draft, denied.CohortState);
    }

    [Fact]
    public async Task Success_then_participant_relationship_does_not_disclose_the_baseline()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        var denied = await harness.Coordinator.ActivateAsync(
            harness.Command() with { Actor = CreateActor(mfa: true, relationship: "participant") },
            TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.False(denied.Succeeded);
        Assert.Equal(AssessmentFailureCodes.Denied, denied.OutcomeCode);
        Assert.Null(denied.BaselineId);
        Assert.Null(denied.BaselineDigest);
        Assert.Equal(CohortStates.Draft, denied.CohortState);
    }

    [Fact]
    public async Task Success_then_revoked_activation_grant_does_not_disclose_the_baseline()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        harness.Authorization.DeniedActions.Add(AssessmentAuthorizationActions.ActivateCohort);
        var denied = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.False(denied.Succeeded);
        Assert.Equal(AssessmentFailureCodes.Denied, denied.OutcomeCode);
        Assert.Null(denied.BaselineId);
        Assert.Null(denied.BaselineDigest);
        Assert.Equal(CohortStates.Draft, denied.CohortState);
    }

    [Fact]
    public async Task Success_then_revoked_grant_after_admission_does_not_disclose_the_baseline()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        harness.Authorization.DeniedOnReauthorize.Add(AssessmentAuthorizationActions.ActivateCohort);
        var denied = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.False(denied.Succeeded);
        Assert.Equal(AssessmentFailureCodes.Denied, denied.OutcomeCode);
        Assert.Null(denied.BaselineId);
        Assert.Null(denied.BaselineDigest);
        Assert.Equal(CohortStates.Draft, denied.CohortState);
    }

    [Fact]
    public async Task Reconcile_after_admission_then_revoked_grant_does_not_disclose_the_baseline()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        harness.Authorization.DeniedOnReauthorize.Add(AssessmentAuthorizationActions.ReconcileActivation);
        var reconciled = await harness.Coordinator.ReconcileAsync(
            new ReconcileActivationQuery(harness.Actor, harness.Draft.ActivityId, harness.Cohort.CohortId, "idem-1"),
            TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.False(reconciled.Succeeded);
        Assert.Equal(AssessmentFailureCodes.Denied, reconciled.OutcomeCode);
        Assert.Null(reconciled.BaselineId);
        Assert.Null(reconciled.BaselineDigest);
        Assert.Equal(CohortStates.Draft, reconciled.CohortState);
    }

    [Fact]
    public async Task Reconcile_returns_the_stored_success_when_the_attempt_appears_after_the_first_read()
    {
        var harness = await CreateReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);
        var delayed = new DelayedFindAttemptStore(harness.Attempts, firstSkipCount: 1);
        var coordinator = new AssessmentActivationCoordinator(
            new InMemoryAssessmentAuthorizationPort(),
            new InMemoryAssessmentSourceCatalog(AssessmentFixtures.PermittedSources()),
            harness.Store,
            harness.UnitOfWork,
            new ActivationBaselineDigester(),
            harness.CommandDigest,
            new InMemoryAssessmentBaselineStore(),
            delayed);

        var reconciled = await coordinator.ReconcileAsync(
            new ReconcileActivationQuery(harness.Actor, harness.Draft.ActivityId, harness.Cohort.CohortId, "idem-1"),
            TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.True(reconciled.Succeeded, reconciled.OutcomeCode);
        Assert.Equal(first.BaselineId, reconciled.BaselineId);
        Assert.Equal("assessment.activated", reconciled.OutcomeCode);
    }

    [Fact]
    public async Task Production_ineligible_model_profile_blocks_activation()
    {
        var harness = await CreateReadyHarnessAsync(DeploymentEnvironments.Production);
        var outcome = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Equal(AssessmentFailureCodes.UnavailableSource, outcome.OutcomeCode);
    }

    [Fact]
    public async Task One_field_change_changes_content_digest()
    {
        var first = ActivationBaselineDocument.FromReadyDraft(
            AssessmentFixtures.CreateDraft().Value!,
            AssessmentFixtures.PermittedSources()).Value!;
        var retitled = AssessmentFixtures.CreateDraft().Value!
            .Save(1, AssessmentFixtures.CreateDraft().Value!.Content with { Title = "Other" }).Value!;
        var second = ActivationBaselineDocument.FromReadyDraft(retitled, AssessmentFixtures.PermittedSources()).Value!;
        var digester = new ActivationBaselineDigester();

        var left = digester.Digest(first);
        var right = digester.Digest(second);

        Assert.True(left.Succeeded);
        Assert.True(right.Succeeded);
        Assert.NotEqual(left.Value, right.Value);
    }

    private static async Task<Harness> CreateReadyHarnessAsync(
        string environment = DeploymentEnvironments.Development,
        IAssessmentClock? clock = null)
    {
        var store = new InMemoryAssessmentDraftStore();
        var authorization = new InMemoryAssessmentAuthorizationPort();
        var catalog = new InMemoryAssessmentSourceCatalog(AssessmentFixtures.PermittedSources());
        var drafts = new AssessmentDraftHandler(authorization, catalog, store, new InMemoryAssessmentUnitOfWork());
        var actor = CreateActor(mfa: true);
        var created = await drafts.CreateAsync(new CreateAssessmentDraftCommand(
            actor,
            "P0 Assessment",
            AssessmentFixtures.ValidTask(),
            AssessmentFixtures.ValidTiming(),
            AssessmentFixtures.Ref(1),
            AssessmentFixtures.Ref(2),
            AssessmentFixtures.Ref(3),
            AssessmentFixtures.Ref(4),
            AssessmentFixtures.Ref(5),
            AssessmentFixtures.Ref(6),
            AssessmentFixtures.Ref(7),
            [AssessmentFixtures.Ref(8)],
            AssessmentFixtures.Ref(10),
            AssessmentFixtures.Ref(11),
            DeploymentEnvironments.Development),
            TestContext.Current.CancellationToken);
        var cohort = store.Cohorts.Single();
        var commandDigest = new AssessmentCommandDigest();
        var unitOfWork = new InMemoryAssessmentUnitOfWork();
        var attempts = new InMemoryAssessmentAttemptStore();
        var coordinator = new AssessmentActivationCoordinator(
            authorization,
            catalog,
            store,
            unitOfWork,
            new ActivationBaselineDigester(),
            commandDigest,
            new InMemoryAssessmentBaselineStore(),
            attempts,
            clock);

        return new Harness(
            coordinator,
            authorization,
            store,
            unitOfWork,
            created.Value!,
            cohort,
            actor,
            commandDigest,
            environment,
            attempts);
    }

    private static AssessmentActorContext CreateActor(bool mfa, string? relationship = null)
    {
        var strength = mfa
            ? new AuthenticationStrength("mfa", ["mfa"])
            : new AuthenticationStrength(null, []);
        return new AssessmentActorContext(
            new TrustedActor(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "human.interactive"),
            new OrganizationScope(AssessmentFixtures.OrganizationId),
            relationship ?? AuthenticationStrengthEvaluator.AdministratorRelationship,
            strength,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            "https");
    }

    private sealed record Harness(
        AssessmentActivationCoordinator Coordinator,
        InMemoryAssessmentAuthorizationPort Authorization,
        InMemoryAssessmentDraftStore Store,
        InMemoryAssessmentUnitOfWork UnitOfWork,
        ActivityDraft Draft,
        AssessmentCohort Cohort,
        AssessmentActorContext Actor,
        AssessmentCommandDigest CommandDigest,
        string Environment,
        InMemoryAssessmentAttemptStore Attempts)
    {
        public ActivateCohortCommand Command(string idempotencyKey = "idem-1")
        {
            var command = new ActivateCohortCommand(
                Actor,
                Draft.ActivityId,
                Cohort.CohortId,
                Draft.RevisionId,
                Draft.RevisionNumber,
                idempotencyKey,
                "pending",
                Environment);
            return command with { TrustedCommandDigest = CommandDigest.Compute(command) };
        }
    }
}

internal sealed class DelayedFindAttemptStore(
    IAssessmentActivationAttemptStore inner,
    int firstSkipCount) : IAssessmentActivationAttemptStore
{
    private int _findCount;

    public Task AcquireIdempotencyLockAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken) =>
        inner.AcquireIdempotencyLockAsync(organizationId, activityId, cohortId, idempotencyKey, transaction, cancellationToken);

    public Task<AssessmentActivationAttempt?> FindSuccessfulAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken) =>
        FindCoreAsync(
            () => inner.FindSuccessfulAsync(organizationId, activityId, cohortId, idempotencyKey, transaction, cancellationToken));

    public Task InsertAsync(
        AssessmentActivationAttempt attempt,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken) =>
        inner.InsertAsync(attempt, transaction, cancellationToken);

    public Task<AssessmentActivationAttempt?> FindAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken) =>
        FindCoreAsync(
            () => inner.FindAsync(organizationId, activityId, cohortId, idempotencyKey, transaction, cancellationToken));

    public Task InsertRequestAuditAsync(
        AssessmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        string outcome,
        string? reasonCode,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken,
        AuthorizationDecision? authorization = null) =>
        inner.InsertRequestAuditAsync(actor, action, resourceId, resourceType, outcome, reasonCode, transaction, cancellationToken, authorization);

    public Task<string> BindCommandDigestAsync(
        Guid organizationId,
        Guid activityId,
        Guid requestedCohortId,
        string idempotencyKey,
        string commandDigest,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken) =>
        inner.BindCommandDigestAsync(
            organizationId,
            activityId,
            requestedCohortId,
            idempotencyKey,
            commandDigest,
            transaction,
            cancellationToken);

    private async Task<AssessmentActivationAttempt?> FindCoreAsync(Func<Task<AssessmentActivationAttempt?>> find)
    {
        var count = Interlocked.Increment(ref _findCount);
        if (count <= firstSkipCount)
        {
            return null;
        }

        return await find();
    }
}

internal sealed class TickAssessmentClock(DateTimeOffset start) : IAssessmentClock
{
    private DateTimeOffset _now = start;

    public DateTimeOffset UtcNow
    {
        get
        {
            var current = _now;
            _now = _now.AddSeconds(1);
            return current;
        }
    }
}
