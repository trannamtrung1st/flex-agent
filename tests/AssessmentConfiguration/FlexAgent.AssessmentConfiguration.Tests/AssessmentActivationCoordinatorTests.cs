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
    public async Task Mismatched_command_digest_is_an_idempotency_conflict()
    {
        var harness = await CreateReadyHarnessAsync();
        var command = harness.Command() with { TrustedCommandDigest = new string('0', 64) };

        var outcome = await harness.Coordinator.ActivateAsync(command, TestContext.Current.CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Equal(AssessmentFailureCodes.IdempotencyConflict, outcome.OutcomeCode);
    }

    [Fact]
    public async Task Audit_failure_does_not_activate()
    {
        var harness = await CreateReadyHarnessAsync();
        harness.UnitOfWork.Transaction.AuditAccepted = false;

        var outcome = await harness.Coordinator.ActivateAsync(harness.Command(), TestContext.Current.CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Equal(AssessmentFailureCodes.AuditUnavailable, outcome.OutcomeCode);
        Assert.Equal(CohortStates.Draft, harness.Store.Cohorts.Single().State);
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
        string environment = DeploymentEnvironments.Development)
    {
        var store = new InMemoryAssessmentDraftStore();
        var authorization = new InMemoryAssessmentAuthorizationPort();
        var catalog = new InMemoryAssessmentSourceCatalog(AssessmentFixtures.PermittedSources());
        var drafts = new AssessmentDraftHandler(authorization, catalog, store);
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
            AssessmentFixtures.Ref(11)),
            TestContext.Current.CancellationToken);
        var cohort = store.Cohorts.Single();
        var commandDigest = new AssessmentCommandDigest();
        var unitOfWork = new InMemoryAssessmentUnitOfWork();
        var coordinator = new AssessmentActivationCoordinator(
            authorization,
            catalog,
            store,
            unitOfWork,
            new ActivationBaselineDigester(),
            commandDigest,
            new InMemoryAssessmentBaselineStore());

        return new Harness(coordinator, store, unitOfWork, created.Value!, cohort, actor, commandDigest, environment);
    }

    private static AssessmentActorContext CreateActor(bool mfa)
    {
        var strength = mfa
            ? new AuthenticationStrength("mfa", ["mfa"])
            : new AuthenticationStrength(null, []);
        return new AssessmentActorContext(
            new TrustedActor(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "human.interactive"),
            new OrganizationScope(AssessmentFixtures.OrganizationId),
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            strength,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            "https");
    }

    private sealed record Harness(
        AssessmentActivationCoordinator Coordinator,
        InMemoryAssessmentDraftStore Store,
        InMemoryAssessmentUnitOfWork UnitOfWork,
        ActivityDraft Draft,
        AssessmentCohort Cohort,
        AssessmentActorContext Actor,
        AssessmentCommandDigest CommandDigest,
        string Environment)
    {
        public ActivateCohortCommand Command()
        {
            var command = new ActivateCohortCommand(
                Actor,
                Draft.ActivityId,
                Cohort.CohortId,
                Draft.RevisionId,
                Draft.RevisionNumber,
                "idem-1",
                "pending",
                Environment);
            return command with { TrustedCommandDigest = CommandDigest.Compute(command) };
        }
    }
}
