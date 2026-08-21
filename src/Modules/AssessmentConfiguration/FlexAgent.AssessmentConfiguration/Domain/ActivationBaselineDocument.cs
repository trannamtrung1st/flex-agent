namespace FlexAgent.AssessmentConfiguration.Domain;

public sealed record FairnessDomainValue(
    string DomainKey,
    IReadOnlyDictionary<string, string> EffectiveValue,
    string Classification);

public sealed record BaselineSourceReference(
    string SourceKey,
    Guid SourceId,
    Guid SourceVersion,
    string ContentDigest);

public sealed record ResolutionDecision(string DecisionKey, string Outcome);

public sealed record ActivationProvenance(
    Guid ActorId,
    string ActorType,
    Guid CorrelationId,
    DateTimeOffset OccurredAtUtc);

public sealed record ActivationBaselineDocument(
    string ProcedureId,
    string SchemaVersion,
    string CanonicalizationVersion,
    IReadOnlyList<FairnessDomainValue> FairnessDomains,
    IReadOnlyList<BaselineSourceReference> SourceReferences,
    IReadOnlyList<ResolutionDecision> ResolutionDecisions,
    IReadOnlyList<Guid> ApprovedExceptionRefs)
{
    public const int MaxUtf8Bytes = 262_144;
    public const int MaxNestingDepth = 8;
    public const int MaxObjectProperties = 64;
    public const int MaxArrayElements = 64;

    internal static readonly HashSet<string> KnownDomainKeys =
    [
        AssessmentSourceCategories.OrganizationPolicy,
        AssessmentSourceCategories.Agent,
        AssessmentSourceCategories.Harness,
        AssessmentSourceCategories.ActivityRevision,
        AssessmentSourceCategories.TaskSubmission,
        AssessmentSourceCategories.Workflow,
        AssessmentSourceCategories.AdaptiveFollowUp,
        AssessmentSourceCategories.RubricEvaluation,
        AssessmentSourceCategories.ModelDeployment,
        AssessmentSourceCategories.Knowledge,
        AssessmentSourceCategories.Capability,
        AssessmentSourceCategories.Memory,
        AssessmentSourceCategories.ReviewRelease,
        AssessmentSourceCategories.Timing,
    ];

    public static AssessmentDecision<ActivationBaselineDocument> FromReadyDraft(
        ActivityDraft draft,
        IReadOnlyList<TrustedSourceDescriptor> sources,
        ActivationProvenance? provenance = null)
    {
        var readiness = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, sources, AuditAvailable: true, DeploymentEnvironments.Development));
        if (readiness.HasBlocker)
        {
            return AssessmentDecision<ActivationBaselineDocument>.Fail(AssessmentFailureCodes.NotReady);
        }

        var domains = new List<FairnessDomainValue>();
        var references = new List<BaselineSourceReference>();
        var content = draft.Content;

        AddSourceDomain(domains, references, sources, content.OrganizationPolicy, AssessmentSourceCategories.OrganizationPolicy, FairnessClassifications.Inherited);
        AddSourceDomain(domains, references, sources, content.Agent, AssessmentSourceCategories.Agent, FairnessClassifications.Inherited);
        AddSourceDomain(domains, references, sources, content.Harness, AssessmentSourceCategories.Harness, FairnessClassifications.Inherited);
        AddSourceDomain(domains, references, sources, content.Workflow, AssessmentSourceCategories.Workflow, FairnessClassifications.Inherited);
        AddSourceDomain(domains, references, sources, content.AdaptiveFollowUp, AssessmentSourceCategories.AdaptiveFollowUp, FairnessClassifications.Inherited);
        AddSourceDomain(domains, references, sources, content.Rubric, AssessmentSourceCategories.RubricEvaluation, FairnessClassifications.Inherited);
        AddSourceDomain(domains, references, sources, content.ModelDeployment, AssessmentSourceCategories.ModelDeployment, FairnessClassifications.Inherited);
        AddSourceDomain(domains, references, sources, content.CapabilityProfile, AssessmentSourceCategories.Capability, FairnessClassifications.MostRestrictive);
        AddSourceDomain(domains, references, sources, content.ReviewRelease, AssessmentSourceCategories.ReviewRelease, FairnessClassifications.Inherited);

        foreach (var knowledge in content.Knowledge.OrderBy(item => item.SourceId).ThenBy(item => item.VersionId))
        {
            AddSourceDomain(domains, references, sources, knowledge, AssessmentSourceCategories.Knowledge, FairnessClassifications.Inherited);
        }

        domains.Add(new FairnessDomainValue(
            AssessmentSourceCategories.ActivityRevision,
            new Dictionary<string, string>
            {
                ["activity_id"] = draft.ActivityId.ToString("D"),
                ["revision_id"] = draft.RevisionId.ToString("D"),
                ["revision_number"] = draft.RevisionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["title"] = content.Title,
            },
            FairnessClassifications.ActivitySupplied));

        var taskSource = sources.First(item => item.Matches(content.Task.RequirementSource));
        domains.Add(new FairnessDomainValue(
            AssessmentSourceCategories.TaskSubmission,
            new Dictionary<string, string>
            {
                ["task_id"] = content.Task.TaskId.ToString("D"),
                ["title"] = content.Task.Title,
                ["requirement_digest"] = taskSource.ContentDigest,
            },
            FairnessClassifications.ActivitySupplied));
        references.Add(new BaselineSourceReference(
            AssessmentSourceCategories.TaskSubmission,
            taskSource.SourceId,
            taskSource.VersionId,
            taskSource.ContentDigest));

        var memoryValues = new Dictionary<string, string>
        {
            ["mode"] = content.Memory.Mode,
            ["stable"] = "true",
            ["learning_disabled"] = "true",
        };
        if (content.Memory.Snapshot is { } snapshot)
        {
            memoryValues["snapshot_digest"] = snapshot.ContentDigest;
            references.Add(new BaselineSourceReference(
                AssessmentSourceCategories.Memory,
                snapshot.SourceId,
                snapshot.VersionId,
                snapshot.ContentDigest));
        }

        domains.Add(new FairnessDomainValue(
            AssessmentSourceCategories.Memory,
            memoryValues,
            content.Memory.Mode == MemoryReadModes.Disabled
                ? FairnessClassifications.Derived
                : FairnessClassifications.ActivitySupplied));

        domains.Add(new FairnessDomainValue(
            AssessmentSourceCategories.Timing,
            new Dictionary<string, string>
            {
                ["starts_at"] = content.Timing.StartsAtUtc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                ["ends_at"] = content.Timing.EndsAtUtc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                ["deadline_at"] = content.Timing.DeadlineUtc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                ["time_zone_id"] = content.Timing.TimeZoneId,
                ["attempt_limit"] = content.Timing.AttemptLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            },
            FairnessClassifications.CohortSupplied));

        var decisions = new List<ResolutionDecision>
        {
            new("memory_mode", content.Memory.Mode),
            new("capability_profile", "p0_assessment_text"),
            new("empty_cohort_permitted", "true"),
            new("exception_path", "none"),
        };
        if (provenance is not null)
        {
            decisions.Add(new ResolutionDecision("activation_actor_id", provenance.ActorId.ToString("D")));
            decisions.Add(new ResolutionDecision("activation_actor_type", provenance.ActorType));
            decisions.Add(new ResolutionDecision("activation_correlation_id", provenance.CorrelationId.ToString("D")));
            decisions.Add(new ResolutionDecision(
                "activation_occurred_at",
                provenance.OccurredAtUtc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")));
        }

        var document = new ActivationBaselineDocument(
            ActivationBaselineProcedure.Id,
            ActivationBaselineProcedure.SchemaVersion,
            ActivationBaselineProcedure.CanonicalizationVersion,
            domains
                .OrderBy(domain => domain.DomainKey, StringComparer.Ordinal)
                .ToArray(),
            references
                .OrderBy(reference => reference.SourceKey, StringComparer.Ordinal)
                .ThenBy(reference => reference.SourceId)
                .ToArray(),
            decisions
                .OrderBy(decision => decision.DecisionKey, StringComparer.Ordinal)
                .ToArray(),
            ApprovedExceptionRefs: []);

        var validation = Validate(document);
        return validation.Succeeded
            ? AssessmentDecision<ActivationBaselineDocument>.Ok(document)
            : AssessmentDecision<ActivationBaselineDocument>.Fail(validation.OutcomeCode);
    }

    public static AssessmentDecision<ActivationBaselineDocument> Validate(ActivationBaselineDocument document)
    {
        if (document.ProcedureId != ActivationBaselineProcedure.Id
            || document.SchemaVersion != ActivationBaselineProcedure.SchemaVersion
            || document.CanonicalizationVersion != ActivationBaselineProcedure.CanonicalizationVersion)
        {
            return AssessmentDecision<ActivationBaselineDocument>.Fail(AssessmentFailureCodes.Incompatible);
        }

        if (document.FairnessDomains.Count is 0 or > 32
            || document.SourceReferences.Count > 64
            || document.ResolutionDecisions.Count > 64
            || document.ApprovedExceptionRefs.Count > 16)
        {
            return AssessmentDecision<ActivationBaselineDocument>.Fail(AssessmentFailureCodes.InvalidField);
        }

        foreach (var domain in document.FairnessDomains)
        {
            if (!KnownDomainKeys.Contains(domain.DomainKey)
                || domain.EffectiveValue.Count is 0 or > MaxObjectProperties
                || domain.EffectiveValue.Keys.Any(key => key.Length is 0 or > 64))
            {
                return AssessmentDecision<ActivationBaselineDocument>.Fail(AssessmentFailureCodes.InvalidField, domain.DomainKey);
            }
        }

        foreach (var reference in document.SourceReferences)
        {
            if (reference.SourceId == Guid.Empty
                || reference.SourceVersion == Guid.Empty
                || reference.ContentDigest.Length != 64
                || reference.ContentDigest != reference.ContentDigest.ToLowerInvariant())
            {
                return AssessmentDecision<ActivationBaselineDocument>.Fail(AssessmentFailureCodes.DigestMismatch);
            }
        }

        return AssessmentDecision<ActivationBaselineDocument>.Ok(document);
    }

    private static void AddSourceDomain(
        List<FairnessDomainValue> domains,
        List<BaselineSourceReference> references,
        IReadOnlyList<TrustedSourceDescriptor> sources,
        ExactSourceRef sourceRef,
        string category,
        string classification)
    {
        var source = sources.First(item => item.Matches(sourceRef));
        domains.Add(new FairnessDomainValue(category, source.EffectiveValues, classification));
        references.Add(new BaselineSourceReference(category, source.SourceId, source.VersionId, source.ContentDigest));
    }
}
