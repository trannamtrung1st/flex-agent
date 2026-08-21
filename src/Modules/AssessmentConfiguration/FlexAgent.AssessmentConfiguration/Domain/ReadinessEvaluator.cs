namespace FlexAgent.AssessmentConfiguration.Domain;

public sealed record ReadinessIssue(
    string Category,
    string Severity,
    string ReasonCode,
    string RecoveryHint);

public sealed record ReadinessResult(
    string OverallSeverity,
    IReadOnlyList<ReadinessIssue> Issues)
{
    public bool HasBlocker => Issues.Any(issue => issue.Severity == ReadinessSeverities.Blocked);

    public static ReadinessResult From(IReadOnlyList<ReadinessIssue> issues)
    {
        var overall = issues.Any(issue => issue.Severity == ReadinessSeverities.Blocked)
            ? ReadinessSeverities.Blocked
            : issues.Any(issue => issue.Severity == ReadinessSeverities.Warning)
                ? ReadinessSeverities.Warning
                : ReadinessSeverities.Ready;
        return new ReadinessResult(overall, issues);
    }
}

public sealed record ReadinessContext(
    ActivityDraft Draft,
    IReadOnlyList<TrustedSourceDescriptor> Sources,
    bool AuditAvailable,
    string Environment);

public static class ReadinessEvaluator
{
    public static ReadinessResult Evaluate(ReadinessContext context)
    {
        var issues = new List<ReadinessIssue>();
        var draft = context.Draft;
        var content = draft.Content;

        EvaluateSource(
            issues,
            context,
            AssessmentSourceCategories.OrganizationPolicy,
            content.OrganizationPolicy,
            AssessmentSourceKinds.OrganizationPolicy);
        EvaluateSource(
            issues,
            context,
            AssessmentSourceCategories.Agent,
            content.Agent,
            AssessmentSourceKinds.AgentRevision);
        EvaluateSource(
            issues,
            context,
            AssessmentSourceCategories.Harness,
            content.Harness,
            AssessmentSourceKinds.HarnessRevision);
        EvaluateSource(
            issues,
            context,
            AssessmentSourceCategories.Workflow,
            content.Workflow,
            AssessmentSourceKinds.WorkflowPolicy);
        EvaluateSource(
            issues,
            context,
            AssessmentSourceCategories.AdaptiveFollowUp,
            content.AdaptiveFollowUp,
            AssessmentSourceKinds.AdaptiveFollowUp);
        EvaluateSource(
            issues,
            context,
            AssessmentSourceCategories.RubricEvaluation,
            content.Rubric,
            AssessmentSourceKinds.RubricEvaluation);
        EvaluateSource(
            issues,
            context,
            AssessmentSourceCategories.ModelDeployment,
            content.ModelDeployment,
            AssessmentSourceKinds.ModelDeployment);
        EvaluateSource(
            issues,
            context,
            AssessmentSourceCategories.Capability,
            content.CapabilityProfile,
            AssessmentSourceKinds.CapabilityProfile);
        EvaluateSource(
            issues,
            context,
            AssessmentSourceCategories.ReviewRelease,
            content.ReviewRelease,
            AssessmentSourceKinds.ReviewRelease);
        EvaluateSource(
            issues,
            context,
            AssessmentSourceCategories.TaskSubmission,
            content.Task.RequirementSource,
            AssessmentSourceKinds.TaskRequirement);

        foreach (var knowledge in content.Knowledge)
        {
            EvaluateSource(
                issues,
                context,
                AssessmentSourceCategories.Knowledge,
                knowledge,
                AssessmentSourceKinds.KnowledgeReference);
        }

        EvaluateMemory(issues, context);
        EvaluateCompatibility(issues, context);
        EvaluateCapabilities(issues, context);
        EvaluateTiming(issues, draft.Content.Timing);
        EvaluateException(issues, context);
        EvaluateAudit(issues, context.AuditAvailable);

        issues.Add(new ReadinessIssue(
            AssessmentSourceCategories.ActivityRevision,
            ReadinessSeverities.Ready,
            "assessment.ok",
            "Saved revision is the candidate for activation."));

        return ReadinessResult.From(issues);
    }

    private static void EvaluateSource(
        List<ReadinessIssue> issues,
        ReadinessContext context,
        string category,
        ExactSourceRef reference,
        string expectedKind)
    {
        var match = context.Sources.FirstOrDefault(source => source.Matches(reference));
        if (match is null)
        {
            issues.Add(Block(category, AssessmentFailureCodes.MissingSource, "Select an exact permitted source revision."));
            return;
        }

        if (match.OrganizationId != context.Draft.OrganizationId)
        {
            issues.Add(Block(category, AssessmentFailureCodes.WrongScope, "Select a source owned by the current Organization."));
            return;
        }

        if (!string.Equals(match.SourceKind, expectedKind, StringComparison.Ordinal)
            || !string.Equals(match.Category, category, StringComparison.Ordinal))
        {
            issues.Add(Block(category, AssessmentFailureCodes.Incompatible, "Select a compatible source kind for this category."));
            return;
        }

        if (match.LifecycleState == SourceLifecycleStates.MutableAlias)
        {
            issues.Add(Block(category, AssessmentFailureCodes.MutableSource, "Resolve the alias to an exact immutable version."));
            return;
        }

        if (match.LifecycleState == SourceLifecycleStates.Revoked)
        {
            issues.Add(Block(category, AssessmentFailureCodes.RevokedSource, "Select a current permitted revision."));
            return;
        }

        if (match.LifecycleState == SourceLifecycleStates.Unavailable)
        {
            issues.Add(Block(category, AssessmentFailureCodes.UnavailableSource, "Wait for availability or select another permitted revision."));
            return;
        }

        if (!match.TransactionallyRevalidatable)
        {
            issues.Add(Block(
                category,
                AssessmentFailureCodes.TransactionOwnerMissing,
                "A transactional source owner is required before activation."));
            return;
        }

        if (context.Environment == DeploymentEnvironments.Production && !match.ProductionEligible)
        {
            issues.Add(Block(
                category,
                AssessmentFailureCodes.UnavailableSource,
                "Production cannot activate a Development or Testing-only source."));
            return;
        }

        issues.Add(new ReadinessIssue(category, ReadinessSeverities.Ready, "assessment.ok", "Exact source revision is permitted."));
    }

    private static void EvaluateMemory(List<ReadinessIssue> issues, ReadinessContext context)
    {
        var memory = context.Draft.Content.Memory;
        if (memory.Mode == MemoryReadModes.Disabled)
        {
            issues.Add(new ReadinessIssue(
                AssessmentSourceCategories.Memory,
                ReadinessSeverities.Ready,
                "assessment.ok",
                "Stable memory with approved reads disabled is the default."));
            return;
        }

        if (memory.Snapshot is null)
        {
            issues.Add(Block(
                AssessmentSourceCategories.Memory,
                AssessmentFailureCodes.InvalidMemory,
                "Select one immutable Organization-owned memory snapshot."));
            return;
        }

        EvaluateSource(
            issues,
            context,
            AssessmentSourceCategories.Memory,
            memory.Snapshot,
            AssessmentSourceKinds.StableMemorySnapshot);
    }

    private static void EvaluateCompatibility(List<ReadinessIssue> issues, ReadinessContext context)
    {
        var agent = context.Sources.FirstOrDefault(source => source.Matches(context.Draft.Content.Agent));
        var harness = context.Sources.FirstOrDefault(source => source.Matches(context.Draft.Content.Harness));
        if (agent is null || harness is null)
        {
            return;
        }

        if (!string.Equals(agent.CompatibilityKey, harness.CompatibilityKey, StringComparison.Ordinal))
        {
            issues.Add(Block(
                AssessmentSourceCategories.Harness,
                AssessmentFailureCodes.Incompatible,
                "Select an Agent and Harness that share an approved compatibility key."));
        }
    }

    private static void EvaluateCapabilities(List<ReadinessIssue> issues, ReadinessContext context)
    {
        var organization = context.Sources.FirstOrDefault(source => source.Matches(context.Draft.Content.OrganizationPolicy));
        var agent = context.Sources.FirstOrDefault(source => source.Matches(context.Draft.Content.Agent));
        var harness = context.Sources.FirstOrDefault(source => source.Matches(context.Draft.Content.Harness));
        var capability = context.Sources.FirstOrDefault(source => source.Matches(context.Draft.Content.CapabilityProfile));
        if (organization is null || agent is null || harness is null || capability is null)
        {
            return;
        }

        var upper = CapabilityBounds.MostRestrictive(
            CapabilityBounds.MostRestrictive(organization.Capabilities, agent.Capabilities),
            CapabilityBounds.MostRestrictive(harness.Capabilities, capability.Capabilities));
        var requested = context.Draft.Content.RequestedCapabilities;

        if (requested.Widens(upper) || requested.ViolatesP0Profile())
        {
            issues.Add(Block(
                AssessmentSourceCategories.Capability,
                AssessmentFailureCodes.Widening,
                "Narrow requested capabilities within Organization, Agent, and Harness bounds."));
        }
    }

    private static void EvaluateTiming(List<ReadinessIssue> issues, TimingRules timing)
    {
        if (!timing.IsValid(out var code))
        {
            issues.Add(Block(AssessmentSourceCategories.Timing, code, "Correct start, end, deadline, timezone, and attempt bounds."));
        }
    }

    private static void EvaluateException(List<ReadinessIssue> issues, ReadinessContext context)
    {
        if (context.Draft.Content.ApprovedException is null)
        {
            issues.Add(new ReadinessIssue(
                AssessmentSourceCategories.ExceptionReference,
                ReadinessSeverities.Ready,
                "assessment.ok",
                "No exception is required for this candidate."));
            return;
        }

        issues.Add(Block(
            AssessmentSourceCategories.ExceptionReference,
            AssessmentFailureCodes.MissingException,
            "A separately approved exception record is required; this slice has no exception workflow."));
    }

    private static void EvaluateAudit(List<ReadinessIssue> issues, bool auditAvailable)
    {
        if (!auditAvailable)
        {
            issues.Add(Block(
                AssessmentSourceCategories.AuditAvailability,
                AssessmentFailureCodes.AuditUnavailable,
                "Activation cannot proceed while required durable audit is unavailable."));
            return;
        }

        issues.Add(new ReadinessIssue(
            AssessmentSourceCategories.AuditAvailability,
            ReadinessSeverities.Ready,
            "assessment.ok",
            "Required durable audit is available."));
    }

    private static ReadinessIssue Block(string category, string reasonCode, string recoveryHint) =>
        new(category, ReadinessSeverities.Blocked, reasonCode, recoveryHint);
}
