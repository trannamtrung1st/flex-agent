namespace FlexAgent.Submissions.Domain;

public static class MaterialCategories
{
    public const string DirectText = "direct_text";
    public const string PlainTextAttachment = "text_plain_attachment";
    public const string MarkdownAttachment = "text_markdown_attachment";
}

public sealed record PolicySourceRef(Guid SourceId, Guid VersionId, string ContentDigest);

public sealed record MaterialCategoryLimit(
    string Category,
    bool Available,
    long MaxBytes,
    int? MaxCount,
    string[] AllowedExtensions,
    string[] DetectedContentTypes);

public sealed record NormalizedMaterialPolicy(
    string ContractVersion,
    PolicySourceRef FrozenRequirement,
    PolicySourceRef OrganizationPolicy,
    string EffectiveDigest,
    DateTimeOffset EffectiveAtUtc,
    IReadOnlyList<MaterialCategoryLimit> Categories,
    int MaxAttachmentCount,
    long MaxAttachmentAggregateBytes,
    MaterialScannerMode ScannerMode,
    TimeSpan ValidationTimeout,
    TimeSpan ArtifactCapabilityLifetime,
    bool EnvironmentEligible);

public static class MaterialPolicyContract
{
    public const string Version = "submissions.material_policy.v1";

    public static NormalizedMaterialPolicy MvpDefaults(
        PolicySourceRef frozenRequirement,
        PolicySourceRef organizationPolicy,
        bool environmentEligible = true) =>
        new(
            Version,
            frozenRequirement,
            organizationPolicy,
            ComputeEffectiveDigest(frozenRequirement, organizationPolicy),
            DateTimeOffset.UtcNow,
            [
                new(MaterialCategories.DirectText, true, 1_048_576, 1, [], ["text/plain"]),
                new(MaterialCategories.PlainTextAttachment, true, 10_485_760, 10, [".txt"], ["text/plain"]),
                new(MaterialCategories.MarkdownAttachment, true, 10_485_760, 10, [".md"], ["text/markdown", "text/x-markdown"]),
            ],
            10,
            26_214_400,
            MaterialScannerMode.DisabledByApprovedPolicy,
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(5),
            environmentEligible);

    public static string ComputeEffectiveDigest(PolicySourceRef frozen, PolicySourceRef organization) =>
        CanonicalDigest($"{frozen.ContentDigest}:{organization.ContentDigest}");

    private static string CanonicalDigest(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public static class MaterialPolicyResolver
{
    public static NormalizedMaterialPolicy? Intersect(
        NormalizedMaterialPolicy? frozen,
        NormalizedMaterialPolicy? organization)
    {
        if (frozen is null || organization is null || !frozen.EnvironmentEligible || !organization.EnvironmentEligible)
        {
            return null;
        }

        var categories = frozen.Categories
            .Select(frozenCat =>
            {
                var orgCat = organization.Categories.FirstOrDefault(c => c.Category == frozenCat.Category);
                if (orgCat is null || !frozenCat.Available || !orgCat.Available)
                {
                    return frozenCat with { Available = false };
                }

                return frozenCat with
                {
                    MaxBytes = Math.Min(frozenCat.MaxBytes, orgCat.MaxBytes),
                    MaxCount = frozenCat.MaxCount is int fc && orgCat.MaxCount is int oc
                        ? Math.Min(fc, oc)
                        : frozenCat.MaxCount ?? orgCat.MaxCount,
                };
            })
            .ToArray();

        return frozen with
        {
            OrganizationPolicy = organization.OrganizationPolicy,
            Categories = categories,
            MaxAttachmentCount = Math.Min(frozen.MaxAttachmentCount, organization.MaxAttachmentCount),
            MaxAttachmentAggregateBytes = Math.Min(
                frozen.MaxAttachmentAggregateBytes,
                organization.MaxAttachmentAggregateBytes),
            ScannerMode = frozen.ScannerMode == MaterialScannerMode.Required
                || organization.ScannerMode == MaterialScannerMode.Required
                    ? MaterialScannerMode.Required
                    : MaterialScannerMode.DisabledByApprovedPolicy,
            EnvironmentEligible = frozen.EnvironmentEligible && organization.EnvironmentEligible,
            EffectiveDigest = MaterialPolicyContract.ComputeEffectiveDigest(
                frozen.FrozenRequirement,
                organization.OrganizationPolicy),
        };
    }

    private static string CanonicalDigest(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
