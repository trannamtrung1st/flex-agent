using System.Text;
using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Sessions.Domain;

public static class ResolvedConfigurationProcedures
{
    public const string P0JcsSha256V1 = "resolved-session-configuration-jcs-sha256-v1";
    public const string SchemaVersion = "v1";
}

public static class ResolvedConfigurationOutcomeCodes
{
    public const string Succeeded = "resolved_configuration.succeeded";
    public const string MissingSource = "resolved_configuration.missing_source";
    public const string MutableAlias = "resolved_configuration.mutable_alias";
    public const string DigestDrift = "resolved_configuration.digest_drift";
    public const string WideningRejected = "resolved_configuration.widening_rejected";
    public const string UnqualifiedModel = "resolved_configuration.unqualified_model";
    public const string DisabledCapability = "resolved_configuration.disabled_capability";
    public const string InvalidField = "resolved_configuration.invalid_field";
}

public sealed record ResolvedSourceReference(
    string SourceKey,
    Guid SourceId,
    Guid SourceVersionId,
    string ContentDigest);

public sealed record P0ResolvedConfigurationRequest(
    Guid ConfigurationId,
    Guid ManifestId,
    SessionOwnership Ownership,
    IReadOnlyList<ResolvedSourceReference> BaselineSources,
    IReadOnlyList<ResolvedSourceReference> RevalidatedSources,
    FrozenTextSessionRuntimePolicy RuntimePolicy,
    FrozenModelDeploymentBinding ModelDeployment,
    IReadOnlyList<ProtectedContentRef> PermittedSubmissionRefs,
    bool VoiceEnabled,
    bool ToolsEnabled,
    bool DynamicMemoryWritesEnabled,
    bool SharedSessionEnabled,
    bool DirectDeploymentEnabled);

public sealed record P0ResolvedConfiguration(
    Guid ConfigurationId,
    string ConfigurationDigest,
    Guid ManifestId,
    string ManifestDigest,
    string CanonicalJson,
    string InitialManifestJson,
    FrozenTextSessionRuntimePolicy RuntimePolicy,
    FrozenModelDeploymentBinding ModelDeployment,
    IReadOnlyList<ProtectedContentRef> PermittedSubmissionRefs);

public sealed record P0ResolvedConfigurationResult(
    bool Succeeded,
    string OutcomeCode,
    P0ResolvedConfiguration? Value);

public static class P0ResolvedSessionConfigurationResolver
{
    private static readonly CanonicalJsonLimits Limits = new(65_536, 64, 4_096, 4_096);
    private static readonly HashSet<string> RequiredSourceKeys =
    [
        "organization_policy",
        "agent",
        "harness",
        "workflow",
        "model_deployment",
        "task_submission",
        "capability",
    ];

    public static P0ResolvedConfigurationResult Resolve(P0ResolvedConfigurationRequest request)
    {
        if (request.ConfigurationId == Guid.Empty
            || request.ManifestId == Guid.Empty
            || request.Ownership.OrganizationId == Guid.Empty
            || request.Ownership.SessionId == Guid.Empty)
        {
            return Fail(ResolvedConfigurationOutcomeCodes.InvalidField);
        }

        if (request.VoiceEnabled
            || request.ToolsEnabled
            || request.DynamicMemoryWritesEnabled
            || request.SharedSessionEnabled
            || request.DirectDeploymentEnabled)
        {
            return Fail(ResolvedConfigurationOutcomeCodes.DisabledCapability);
        }

        if (string.IsNullOrWhiteSpace(request.ModelDeployment.ProfileId)
            || string.IsNullOrWhiteSpace(request.ModelDeployment.ProfileVersion)
            || request.ModelDeployment.ProfileDigest.Length != 64
            || string.IsNullOrWhiteSpace(request.ModelDeployment.ProviderId)
            || string.IsNullOrWhiteSpace(request.ModelDeployment.CredentialBindingReference)
            || string.IsNullOrWhiteSpace(request.ModelDeployment.CredentialBindingVersion))
        {
            return Fail(ResolvedConfigurationOutcomeCodes.UnqualifiedModel);
        }

        foreach (var source in request.BaselineSources.Concat(request.RevalidatedSources))
        {
            if (source.SourceKey.Contains("current", StringComparison.OrdinalIgnoreCase)
                || source.SourceKey.Contains("latest", StringComparison.OrdinalIgnoreCase)
                || source.SourceVersionId == Guid.Empty)
            {
                return Fail(ResolvedConfigurationOutcomeCodes.MutableAlias);
            }
        }

        foreach (var required in RequiredSourceKeys)
        {
            if (request.BaselineSources.All(source =>
                    !string.Equals(source.SourceKey, required, StringComparison.Ordinal)))
            {
                return Fail(ResolvedConfigurationOutcomeCodes.MissingSource);
            }
        }

        foreach (var baseline in request.BaselineSources)
        {
            var live = request.RevalidatedSources.FirstOrDefault(source =>
                source.SourceId == baseline.SourceId && source.SourceVersionId == baseline.SourceVersionId);
            if (live is null
                || !string.Equals(live.ContentDigest, baseline.ContentDigest, StringComparison.Ordinal))
            {
                return Fail(ResolvedConfigurationOutcomeCodes.DigestDrift);
            }
        }

        if (request.PermittedSubmissionRefs.Count == 0)
        {
            return Fail(ResolvedConfigurationOutcomeCodes.InvalidField);
        }

        var disabled = request.RuntimePolicy.ExplicitlyDisabledCapabilities;
        if (!P0TextSessionRuntimeCapabilityPolicy.RequiredExplicitlyDisabledCapabilities
                .All(disabled.Contains))
        {
            return Fail(ResolvedConfigurationOutcomeCodes.WideningRejected);
        }

        var canonical = BuildCanonicalJson(request);
        var digest = CanonicalJsonProcessor.CanonicalizeSha256Hex(Encoding.UTF8.GetBytes(canonical), Limits);
        var manifest = BuildInitialManifestJson(request, digest);
        var manifestDigest = CanonicalJsonProcessor.CanonicalizeSha256Hex(Encoding.UTF8.GetBytes(manifest), Limits);
        return new P0ResolvedConfigurationResult(
            true,
            ResolvedConfigurationOutcomeCodes.Succeeded,
            new P0ResolvedConfiguration(
                request.ConfigurationId,
                digest,
                request.ManifestId,
                manifestDigest,
                canonical,
                manifest,
                request.RuntimePolicy,
                request.ModelDeployment,
                request.PermittedSubmissionRefs));
    }

    private static P0ResolvedConfigurationResult Fail(string code) =>
        new(false, code, null);

    private static string BuildCanonicalJson(P0ResolvedConfigurationRequest request)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("procedure_id", ResolvedConfigurationProcedures.P0JcsSha256V1);
            writer.WriteString("schema_version", ResolvedConfigurationProcedures.SchemaVersion);
            writer.WriteString("configuration_id", request.ConfigurationId.ToString("D"));
            writer.WriteString("organization_id", request.Ownership.OrganizationId.ToString("D"));
            writer.WriteString("activity_id", request.Ownership.ActivityId.ToString("D"));
            writer.WriteString("participant_id", request.Ownership.ParticipantId.ToString("D"));
            writer.WriteString("attempt_id", request.Ownership.AttemptId.ToString("D"));
            writer.WriteString("session_id", request.Ownership.SessionId.ToString("D"));
            writer.WriteString("runtime_policy_digest", request.RuntimePolicy.PolicyDigest);
            writer.WriteString("model_profile_id", request.ModelDeployment.ProfileId);
            writer.WriteString("model_profile_version", request.ModelDeployment.ProfileVersion);
            writer.WriteString("model_profile_digest", request.ModelDeployment.ProfileDigest);
            writer.WriteString("model_provider_id", request.ModelDeployment.ProviderId);
            writer.WriteString("credential_binding_reference", request.ModelDeployment.CredentialBindingReference);
            writer.WriteString("credential_binding_version", request.ModelDeployment.CredentialBindingVersion);
            writer.WritePropertyName("sources");
            writer.WriteStartArray();
            foreach (var source in request.BaselineSources
                         .OrderBy(item => item.SourceKey, StringComparer.Ordinal)
                         .ThenBy(item => item.SourceId))
            {
                writer.WriteStartObject();
                writer.WriteString("source_key", source.SourceKey);
                writer.WriteString("source_id", source.SourceId.ToString("D"));
                writer.WriteString("source_version_id", source.SourceVersionId.ToString("D"));
                writer.WriteString("content_digest", source.ContentDigest);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WritePropertyName("permitted_submissions");
            writer.WriteStartArray();
            foreach (var submission in request.PermittedSubmissionRefs.OrderBy(item => item.ProtectedRef, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("protected_ref", submission.ProtectedRef);
                writer.WriteString("content_digest", submission.ContentDigest);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string BuildInitialManifestJson(P0ResolvedConfigurationRequest request, string configurationDigest)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("manifest_id", request.ManifestId.ToString("D"));
            writer.WriteString("configuration_id", request.ConfigurationId.ToString("D"));
            writer.WriteString("configuration_digest", configurationDigest);
            writer.WriteString("session_id", request.Ownership.SessionId.ToString("D"));
            writer.WriteString("attempt_id", request.Ownership.AttemptId.ToString("D"));
            writer.WritePropertyName("provenance");
            writer.WriteStartArray();
            foreach (var source in request.BaselineSources
                         .OrderBy(item => item.SourceKey, StringComparer.Ordinal)
                         .ThenBy(item => item.SourceId))
            {
                writer.WriteStartObject();
                writer.WriteString("source_key", source.SourceKey);
                writer.WriteString("source_id", source.SourceId.ToString("D"));
                writer.WriteString("source_version_id", source.SourceVersionId.ToString("D"));
                writer.WriteString("content_digest", source.ContentDigest);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
