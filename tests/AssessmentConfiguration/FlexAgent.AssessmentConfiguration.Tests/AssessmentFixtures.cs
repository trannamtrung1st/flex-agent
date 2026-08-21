using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Tests;

internal static class AssessmentFixtures
{
    internal static readonly Guid OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    internal static string Digest(char fill) => new(fill, 64);

    internal static ExactSourceRef Ref(byte n) => AssessmentDevelopmentSources.Ref(n);

    internal static TimingRules ValidTiming() => new(
        StartsAtUtc: new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
        EndsAtUtc: new DateTimeOffset(2026, 9, 30, 23, 59, 0, TimeSpan.Zero),
        DeadlineUtc: new DateTimeOffset(2026, 9, 30, 17, 0, 0, TimeSpan.Zero),
        TimeZoneId: "UTC",
        AttemptLimit: 2,
        PerAttemptDurationSeconds: 3600);

    internal static TaskBinding ValidTask() => new(
        Guid.Parse("44444444-4444-4444-4444-444444444444"),
        "Task 1",
        "Submit one written response",
        Ref(9));

    internal static AssessmentDecision<ActivityDraft> CreateDraft() =>
        ActivityDraft.Create(
            OrganizationId,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "P0 Assessment",
            ValidTask(),
            ValidTiming(),
            Ref(1),
            Ref(2),
            Ref(3),
            Ref(4),
            Ref(5),
            Ref(6),
            Ref(7),
            [Ref(8)],
            Ref(10),
            Ref(11));

    internal static TrustedSourceDescriptor Source(
        ExactSourceRef reference,
        string kind,
        string category,
        string lifecycle = SourceLifecycleStates.Available,
        string compatibility = "p0-text",
        bool transactional = true,
        bool productionEligible = false,
        Guid? organizationId = null,
        CapabilityBounds? capabilities = null) =>
        new(
            organizationId ?? OrganizationId,
            reference.SourceId,
            reference.VersionId,
            kind,
            category,
            reference.ContentDigest,
            lifecycle,
            compatibility,
            capabilities ?? CapabilityBounds.P0Assessment,
            new Dictionary<string, string> { ["ref"] = reference.VersionId.ToString("D") },
            transactional,
            productionEligible);

    internal static List<TrustedSourceDescriptor> PermittedSources() =>
    [
        Source(Ref(1), AssessmentSourceKinds.OrganizationPolicy, AssessmentSourceCategories.OrganizationPolicy),
        Source(Ref(2), AssessmentSourceKinds.AgentRevision, AssessmentSourceCategories.Agent),
        Source(Ref(3), AssessmentSourceKinds.HarnessRevision, AssessmentSourceCategories.Harness),
        Source(Ref(4), AssessmentSourceKinds.WorkflowPolicy, AssessmentSourceCategories.Workflow),
        Source(Ref(5), AssessmentSourceKinds.AdaptiveFollowUp, AssessmentSourceCategories.AdaptiveFollowUp),
        Source(Ref(6), AssessmentSourceKinds.RubricEvaluation, AssessmentSourceCategories.RubricEvaluation),
        Source(Ref(7), AssessmentSourceKinds.ModelDeployment, AssessmentSourceCategories.ModelDeployment),
        Source(Ref(8), AssessmentSourceKinds.KnowledgeReference, AssessmentSourceCategories.Knowledge),
        Source(Ref(9), AssessmentSourceKinds.WorkflowPolicy, AssessmentSourceCategories.TaskSubmission),
        Source(Ref(10), AssessmentSourceKinds.CapabilityProfile, AssessmentSourceCategories.Capability),
        Source(Ref(11), AssessmentSourceKinds.ReviewRelease, AssessmentSourceCategories.ReviewRelease),
    ];
}
