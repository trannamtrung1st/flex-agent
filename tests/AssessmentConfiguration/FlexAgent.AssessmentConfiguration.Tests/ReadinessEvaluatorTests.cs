using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Tests;

public sealed class ReadinessEvaluatorTests
{
    [Fact]
    public void Permitted_synthetic_sources_are_ready_in_development()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var result = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, AssessmentFixtures.PermittedSources(), true, DeploymentEnvironments.Development));

        Assert.Equal(ReadinessSeverities.Ready, result.OverallSeverity);
        Assert.False(result.HasBlocker);
    }

    [Fact]
    public void Empty_knowledge_is_a_warning_and_does_not_block_activation()
    {
        var draft = ActivityDraft.Create(
            AssessmentFixtures.OrganizationId,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
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
            [],
            AssessmentFixtures.Ref(10),
            AssessmentFixtures.Ref(11)).Value!;

        var result = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, AssessmentFixtures.PermittedSources(), true, DeploymentEnvironments.Development));

        Assert.Equal(ReadinessSeverities.Warning, result.OverallSeverity);
        Assert.False(result.HasBlocker);
        Assert.Contains(result.Issues, issue =>
            issue.Category == AssessmentSourceCategories.Knowledge
            && issue.Severity == ReadinessSeverities.Warning
            && issue.ReasonCode == AssessmentFailureCodes.KnowledgeUnselected);
    }

    [Fact]
    public void Missing_source_is_blocked()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var sources = AssessmentFixtures.PermittedSources();
        sources.RemoveAll(source => source.Category == AssessmentSourceCategories.Agent);

        var result = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, sources, true, DeploymentEnvironments.Development));

        Assert.True(result.HasBlocker);
        Assert.Contains(result.Issues, issue =>
            issue.Category == AssessmentSourceCategories.Agent
            && issue.ReasonCode == AssessmentFailureCodes.MissingSource);
    }

    [Fact]
    public void Cross_organization_source_is_blocked()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var sources = AssessmentFixtures.PermittedSources();
        sources[1] = AssessmentFixtures.Source(
            AssessmentFixtures.Ref(2),
            AssessmentSourceKinds.AgentRevision,
            AssessmentSourceCategories.Agent,
            organizationId: Guid.Parse("99999999-9999-9999-9999-999999999999"));

        var result = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, sources, true, DeploymentEnvironments.Development));

        Assert.Contains(result.Issues, issue => issue.ReasonCode == AssessmentFailureCodes.WrongScope);
    }

    [Fact]
    public void Mutable_revoked_and_digest_mismatch_are_blocked()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var sources = AssessmentFixtures.PermittedSources();
        sources[1] = AssessmentFixtures.Source(
            AssessmentFixtures.Ref(2),
            AssessmentSourceKinds.AgentRevision,
            AssessmentSourceCategories.Agent,
            lifecycle: SourceLifecycleStates.MutableAlias);
        sources[2] = AssessmentFixtures.Source(
            AssessmentFixtures.Ref(3),
            AssessmentSourceKinds.HarnessRevision,
            AssessmentSourceCategories.Harness,
            lifecycle: SourceLifecycleStates.Revoked);
        sources[6] = sources[6] with { ContentDigest = AssessmentFixtures.Digest('z') };

        var result = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, sources, true, DeploymentEnvironments.Development));

        Assert.Contains(result.Issues, issue => issue.ReasonCode == AssessmentFailureCodes.MutableSource);
        Assert.Contains(result.Issues, issue => issue.ReasonCode == AssessmentFailureCodes.RevokedSource);
        Assert.Contains(result.Issues, issue =>
            issue.Category == AssessmentSourceCategories.ModelDeployment
            && issue.ReasonCode == AssessmentFailureCodes.MissingSource);
    }

    [Fact]
    public void Incompatible_agent_and_harness_are_blocked()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var sources = AssessmentFixtures.PermittedSources();
        sources[2] = AssessmentFixtures.Source(
            AssessmentFixtures.Ref(3),
            AssessmentSourceKinds.HarnessRevision,
            AssessmentSourceCategories.Harness,
            compatibility: "other");

        var result = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, sources, true, DeploymentEnvironments.Development));

        Assert.Contains(result.Issues, issue => issue.ReasonCode == AssessmentFailureCodes.Incompatible);
    }

    [Fact]
    public void Capability_widening_is_blocked()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var widened = draft.Save(
            draft.RevisionNumber,
            draft.Content with
            {
                RequestedCapabilities = CapabilityBounds.P0Assessment with { ToolsEnabled = true, PermittedTools = ["search"] },
            });
        Assert.False(widened.Succeeded);

        var sources = AssessmentFixtures.PermittedSources();
        sources[0] = AssessmentFixtures.Source(
            AssessmentFixtures.Ref(1),
            AssessmentSourceKinds.OrganizationPolicy,
            AssessmentSourceCategories.OrganizationPolicy,
            capabilities: CapabilityBounds.P0Assessment with { VoiceEnabled = false, TextEnabled = false });

        var result = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, sources, true, DeploymentEnvironments.Development));

        Assert.Contains(result.Issues, issue => issue.ReasonCode == AssessmentFailureCodes.Widening);
    }

    [Fact]
    public void Non_transactional_or_production_ineligible_model_profile_is_blocked()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var sources = AssessmentFixtures.PermittedSources();
        sources[6] = AssessmentFixtures.Source(
            AssessmentFixtures.Ref(7),
            AssessmentSourceKinds.ModelDeployment,
            AssessmentSourceCategories.ModelDeployment,
            transactional: false);

        var development = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, sources, true, DeploymentEnvironments.Development));
        Assert.Contains(development.Issues, issue => issue.ReasonCode == AssessmentFailureCodes.TransactionOwnerMissing);

        sources[6] = AssessmentFixtures.Source(
            AssessmentFixtures.Ref(7),
            AssessmentSourceKinds.ModelDeployment,
            AssessmentSourceCategories.ModelDeployment,
            productionEligible: false);
        var production = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, sources, true, DeploymentEnvironments.Production));
        Assert.Contains(production.Issues, issue =>
            issue.Category == AssessmentSourceCategories.ModelDeployment
            && issue.ReasonCode == AssessmentFailureCodes.UnavailableSource);
    }

    [Fact]
    public void Missing_audit_and_unknown_exception_are_blocked()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var withException = draft.Save(
            draft.RevisionNumber,
            draft.Content with { ApprovedException = AssessmentFixtures.Ref(12) }).Value!;

        var result = ReadinessEvaluator.Evaluate(
            new ReadinessContext(withException, AssessmentFixtures.PermittedSources(), false, DeploymentEnvironments.Development));

        Assert.Contains(result.Issues, issue => issue.ReasonCode == AssessmentFailureCodes.MissingException);
        Assert.Contains(result.Issues, issue => issue.ReasonCode == AssessmentFailureCodes.AuditUnavailable);
    }

    [Fact]
    public void Missing_revoked_wrong_scope_and_digest_mismatched_task_requirement_are_blocked()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;

        var missing = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, AssessmentFixtures.PermittedSources().Where(source =>
                source.Category != AssessmentSourceCategories.TaskSubmission).ToList(), true, DeploymentEnvironments.Development));
        Assert.Contains(missing.Issues, issue =>
            issue.Category == AssessmentSourceCategories.TaskSubmission
            && issue.ReasonCode == AssessmentFailureCodes.MissingSource);

        var revokedSources = AssessmentFixtures.PermittedSources();
        revokedSources[8] = AssessmentFixtures.Source(
            AssessmentFixtures.Ref(9),
            AssessmentSourceKinds.TaskRequirement,
            AssessmentSourceCategories.TaskSubmission,
            lifecycle: SourceLifecycleStates.Revoked);
        var revoked = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, revokedSources, true, DeploymentEnvironments.Development));
        Assert.Contains(revoked.Issues, issue =>
            issue.Category == AssessmentSourceCategories.TaskSubmission
            && issue.ReasonCode == AssessmentFailureCodes.RevokedSource);

        var scopedSources = AssessmentFixtures.PermittedSources();
        scopedSources[8] = AssessmentFixtures.Source(
            AssessmentFixtures.Ref(9),
            AssessmentSourceKinds.TaskRequirement,
            AssessmentSourceCategories.TaskSubmission,
            organizationId: Guid.Parse("99999999-9999-9999-9999-999999999999"));
        var scoped = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, scopedSources, true, DeploymentEnvironments.Development));
        Assert.Contains(scoped.Issues, issue =>
            issue.Category == AssessmentSourceCategories.TaskSubmission
            && issue.ReasonCode == AssessmentFailureCodes.WrongScope);

        var digestSources = AssessmentFixtures.PermittedSources();
        digestSources[8] = digestSources[8] with { ContentDigest = AssessmentFixtures.Digest('z') };
        var digest = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, digestSources, true, DeploymentEnvironments.Development));
        Assert.Contains(digest.Issues, issue =>
            issue.Category == AssessmentSourceCategories.TaskSubmission
            && issue.ReasonCode == AssessmentFailureCodes.MissingSource);
    }

    [Fact]
    public void Cross_scope_memory_snapshot_is_blocked()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var snapshot = AssessmentFixtures.Ref(12);
        var saved = draft.Save(
            draft.RevisionNumber,
            draft.Content with { Memory = new MemoryChoice(MemoryReadModes.ImmutableSnapshot, snapshot) }).Value!;
        var sources = AssessmentFixtures.PermittedSources();
        sources.Add(AssessmentFixtures.Source(
            snapshot,
            AssessmentSourceKinds.StableMemorySnapshot,
            AssessmentSourceCategories.Memory,
            organizationId: Guid.NewGuid()));

        var result = ReadinessEvaluator.Evaluate(
            new ReadinessContext(saved, sources, true, DeploymentEnvironments.Development));

        Assert.Contains(result.Issues, issue =>
            issue.Category == AssessmentSourceCategories.Memory
            && issue.ReasonCode == AssessmentFailureCodes.WrongScope);
    }
}
