using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public static class DevelopmentMaterialPolicy
{
    public static NormalizedMaterialPolicy Create(
        Guid organizationId,
        PolicySourceRef frozenRequirement,
        PolicySourceRef organizationPolicy,
        string environment) =>
        MaterialPolicyContract.MvpDefaults(
            frozenRequirement,
            organizationPolicy,
            environmentEligible: !string.Equals(environment, "production", StringComparison.Ordinal)
                && !string.Equals(environment, "staging", StringComparison.Ordinal));

    public static NormalizedMaterialPolicy FrozenRequirement(PolicySourceRef requirementRef) =>
        MaterialPolicyContract.MvpDefaults(
            requirementRef,
            requirementRef with { VersionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1") },
            environmentEligible: true);

    public static NormalizedMaterialPolicy OrganizationPolicy(PolicySourceRef organizationRef) =>
        MaterialPolicyContract.MvpDefaults(
            organizationRef with { VersionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1") },
            organizationRef,
            environmentEligible: true);
}

public sealed class FixedFrozenSubmissionRequirementPort : IFrozenSubmissionRequirementPort
{
    public NormalizedMaterialPolicy? Policy { get; set; }

    public Task<NormalizedMaterialPolicy?> ResolveFrozenAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid taskSourceId,
        Guid taskVersionId,
        string taskContentDigest,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<NormalizedMaterialPolicy?>(
            Policy ?? DevelopmentMaterialPolicy.FrozenRequirement(
                new PolicySourceRef(taskSourceId, taskVersionId, taskContentDigest)));
}

public sealed class FixedMaterialPolicyPort : IMaterialPolicyPort
{
    public NormalizedMaterialPolicy? Policy { get; set; }

    public Task<NormalizedMaterialPolicy?> ResolveCurrentAsync(
        Guid organizationId,
        PolicySourceRef frozenOrganizationPolicyRef,
        DateTimeOffset nowUtc,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<NormalizedMaterialPolicy?>(
            Policy ?? DevelopmentMaterialPolicy.OrganizationPolicy(frozenOrganizationPolicyRef));
}

public sealed class DisabledArtifactSafetyScanner : IArtifactSafetyScanner
{
    public Task<ArtifactScanResult> ScanAsync(ArtifactScanRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ArtifactScanResult(
            true,
            ArtifactScanOutcome.Clean,
            ArtifactOutcomeCodes.ScanDisabled));
}

public sealed class UnavailableFrozenSubmissionRequirementPort : IFrozenSubmissionRequirementPort
{
    public Task<NormalizedMaterialPolicy?> ResolveFrozenAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid taskSourceId,
        Guid taskVersionId,
        string taskContentDigest,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<NormalizedMaterialPolicy?>(null);
}

public sealed class UnavailableMaterialPolicyPort : IMaterialPolicyPort
{
    public Task<NormalizedMaterialPolicy?> ResolveCurrentAsync(
        Guid organizationId,
        PolicySourceRef frozenOrganizationPolicyRef,
        DateTimeOffset nowUtc,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<NormalizedMaterialPolicy?>(null);
}

public sealed class UnavailableArtifactSafetyScanner : IArtifactSafetyScanner
{
    public Task<ArtifactScanResult> ScanAsync(ArtifactScanRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ArtifactScanResult(
            false,
            ArtifactScanOutcome.Unavailable,
            ArtifactOutcomeCodes.ScanUnavailable));
}

public sealed class UnavailableActivityClosurePort : IActivityClosurePort
{
    public Task<DateTimeOffset?> FindClosedAtUtcAsync(
        Guid organizationId,
        Guid activityId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<DateTimeOffset?>(null);
}

public sealed class ApprovedDefaultAcceptedPayloadLifecyclePolicyPort : IAcceptedPayloadLifecyclePolicyPort
{
    public Task<AcceptedPayloadLifecyclePolicy> ResolveAcceptedPayloadPolicyAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AcceptedPayloadLifecyclePolicy.ApprovedOperationalDefault);
}
