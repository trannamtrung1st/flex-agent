using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public static class AssessmentDevelopmentSources
{
    public static ExactSourceRef OrganizationPolicy { get; } = Ref(1);
    public static ExactSourceRef Agent { get; } = Ref(2);
    public static ExactSourceRef Harness { get; } = Ref(3);
    public static ExactSourceRef Workflow { get; } = Ref(4);
    public static ExactSourceRef AdaptiveFollowUp { get; } = Ref(5);
    public static ExactSourceRef Rubric { get; } = Ref(6);
    public static ExactSourceRef ModelDeployment { get; } = Ref(7);
    public static ExactSourceRef Knowledge { get; } = Ref(8);
    public static ExactSourceRef TaskRequirement { get; } = Ref(9);
    public static ExactSourceRef Capability { get; } = Ref(10);
    public static ExactSourceRef ReviewRelease { get; } = Ref(11);

    public static ExactSourceRef Ref(byte n) =>
        new(
            Guid.Parse($"22222222-2222-2222-2222-2222222222{n:D2}"),
            Guid.Parse($"33333333-3333-3333-3333-3333333333{n:D2}"),
            new string((char)('a' + n), 64));

    public static IReadOnlyList<TrustedSourceDescriptor> ForOrganization(Guid organizationId) =>
    [
        Descriptor(organizationId, OrganizationPolicy, AssessmentSourceKinds.OrganizationPolicy, AssessmentSourceCategories.OrganizationPolicy),
        Descriptor(organizationId, Agent, AssessmentSourceKinds.AgentRevision, AssessmentSourceCategories.Agent),
        Descriptor(organizationId, Harness, AssessmentSourceKinds.HarnessRevision, AssessmentSourceCategories.Harness),
        Descriptor(organizationId, Workflow, AssessmentSourceKinds.WorkflowPolicy, AssessmentSourceCategories.Workflow),
        Descriptor(organizationId, AdaptiveFollowUp, AssessmentSourceKinds.AdaptiveFollowUp, AssessmentSourceCategories.AdaptiveFollowUp),
        Descriptor(organizationId, Rubric, AssessmentSourceKinds.RubricEvaluation, AssessmentSourceCategories.RubricEvaluation),
        Descriptor(organizationId, ModelDeployment, AssessmentSourceKinds.ModelDeployment, AssessmentSourceCategories.ModelDeployment),
        Descriptor(organizationId, Knowledge, AssessmentSourceKinds.KnowledgeReference, AssessmentSourceCategories.Knowledge),
        Descriptor(organizationId, TaskRequirement, AssessmentSourceKinds.TaskRequirement, AssessmentSourceCategories.TaskSubmission),
        Descriptor(organizationId, Capability, AssessmentSourceKinds.CapabilityProfile, AssessmentSourceCategories.Capability),
        Descriptor(organizationId, ReviewRelease, AssessmentSourceKinds.ReviewRelease, AssessmentSourceCategories.ReviewRelease),
    ];

    private static TrustedSourceDescriptor Descriptor(
        Guid organizationId,
        ExactSourceRef reference,
        string kind,
        string category) =>
        new(
            organizationId,
            reference.SourceId,
            reference.VersionId,
            kind,
            category,
            reference.ContentDigest,
            SourceLifecycleStates.Available,
            "p0-text",
            CapabilityBounds.P0Assessment,
            new Dictionary<string, string> { ["ref"] = reference.VersionId.ToString("D") },
            TransactionallyRevalidatable: true,
            ProductionEligible: false);
}
