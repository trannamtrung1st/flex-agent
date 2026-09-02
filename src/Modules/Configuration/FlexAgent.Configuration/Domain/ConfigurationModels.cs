namespace FlexAgent.Configuration.Domain;

public static class ConfigurationSourceKinds
{
    public const string SyntheticV1 = "synthetic.configuration_source.v1";
}

public static class ConfigurationProcedureIds
{
    public const string RscJcsSha256V1 = "rsc-jcs-sha256-v1";
}

public static class ConfigurationSchemaVersions
{
    public const string V1 = "v1";
}

public static class RegisterConfigurationSourceVersionFailureCodes
{
    public const string Denied = "configuration_source_version.denied";
    public const string InvalidDigest = "configuration_source_version.invalid_digest";
    public const string InvalidProcedure = "configuration_source_version.invalid_procedure";
    public const string ParentNotFound = "configuration_source_version.parent_not_found";
    public const string IdempotencyConflict = "configuration_source_version.idempotency_conflict";
    public const string Unavailable = "configuration_source_version.unavailable";
    public const string NoticeProjectionInvalid = "configuration_source_version.notice_projection_invalid";
}

public sealed record ConfigurationSourceVersionIdentity(
    Guid OrganizationId,
    Guid ConfigurationSourceId,
    Guid VersionId,
    string ContentDigest);

public sealed record RegisterConfigurationSourceVersionResult(
    bool Succeeded,
    string OutcomeCode,
    ConfigurationSourceVersionIdentity? Identity);
